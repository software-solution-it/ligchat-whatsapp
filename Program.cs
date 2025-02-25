using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Hangfire;
using System.Net.WebSockets;
using WhatsAppProject.Data;
using WhatsAppProject.Services;
using Hangfire.MySql;
using MongoDB.Driver;
using Microsoft.Extensions.FileProviders;
using Serilog;
using System.IO;
using WhatsAppProject;
using Xabe.FFmpeg;
using Microsoft.AspNetCore.SignalR;
using WhatsAppProject.Hubs;
using WhatsAppProject.Middleware;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Configurar a porta
builder.WebHost.UseUrls("http://localhost:5001");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

builder.Services.AddControllers();

builder.Services.AddDbContext<WhatsAppContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 26)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure()
    )
    .EnableSensitiveDataLogging() 
    .EnableDetailedErrors()
);

builder.Services.AddDbContext<SaasDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("SaasDatabase"),
        new MySqlServerVersion(new Version(8, 0, 26)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure()
    )
    .EnableSensitiveDataLogging()
    .EnableDetailedErrors()
);

var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDb");
var mongoDatabaseName = builder.Configuration["DatabaseName"];


builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConnectionString));

builder.Services.AddScoped<MongoDbContext>(sp => new MongoDbContext(sp.GetRequiredService<IMongoClient>(), mongoDatabaseName));

builder.Services.AddScoped<WhatsAppService>();
builder.Services.AddScoped<ContactService>();
builder.Services.AddScoped<WebhookService>();
builder.Services.AddScoped<MessageSchedulingService>();
builder.Services.AddSingleton<WebSocketManager>();
builder.Services.AddHttpClient(); 

builder.Services.AddHangfire(config =>
{
    var storageOptions = new MySqlStorageOptions
    {
        PrepareSchemaIfNecessary = true, 
        QueuePollInterval = TimeSpan.FromSeconds(15),
        JobExpirationCheckInterval = TimeSpan.FromHours(1)
    };

    config.UseStorage(new MySqlStorage(builder.Configuration.GetConnectionString("DefaultConnection"), storageOptions));
});

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 1; 
});

builder.Services.AddSingleton<IRecurringJobManager, RecurringJobManager>();
builder.Services.AddSingleton<IBackgroundJobClient, BackgroundJobClient>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "WhatsApp API", Version = "v1" });
});

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine("logs", "log-.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 10 * 1024 * 1024,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();

builder.Host.UseSerilog();

// Configurar logging adicional
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configurar nível de log detalhado para seu namespace
builder.Logging.AddFilter("WhatsAppProject", LogLevel.Debug);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);

// Registrar serviços
builder.Services.AddLogging();

// Registrar o handler do WebSocket
builder.Services.AddScoped<WebSocketConnectionHandler>(); 

// Registrar o DbContext
builder.Services.AddDbContext<WhatsAppContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registrar os serviços
builder.Services.AddScoped<ContactService>();
builder.Services.AddScoped<WebhookService>();

builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();

// Configurar FFmpeg
string ffmpegPath = @"C:\Program Files\ffmpeg\bin";
FFmpeg.SetExecutablesPath(ffmpegPath);

// Registrar o caminho do FFmpeg na configuração para uso posterior
builder.Configuration["FFmpeg:ExecutablesPath"] = ffmpegPath;

// Adicionar SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Configurar CORS antes dos arquivos estáticos
app.UseCors("AllowAll");

// Garantir que o diretório de uploads existe
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
Directory.CreateDirectory(uploadsPath); // Cria se não existir

// Criar subdiretórios para cada tipo de mídia
var mediaTypes = new[] { "image", "audio", "video", "application" };
foreach (var mediaType in mediaTypes)
{
    Directory.CreateDirectory(Path.Combine(uploadsPath, mediaType));
}

// Configurar arquivos estáticos
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads")),
    RequestPath = "/uploads",
    OnPrepareResponse = ctx =>
    {
        // Headers CORS
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Methods", "GET");
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Headers", "*");
        
        // Cache
        ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=3600");
    }
});

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
});

app.Use(async (context, next) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        try
        {
            var webSocketManager = context.RequestServices.GetRequiredService<WebSocketManager>();
            var webSocketHandler = context.RequestServices.GetRequiredService<WebSocketConnectionHandler>();
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            
            var sectorId = context.Request.Query["sectorId"].ToString();
            
            if (string.IsNullOrEmpty(sectorId))
            {
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.InvalidPayloadData,
                    "Sector ID is required.",
                    CancellationToken.None);
                return;
            }

            logger.LogInformation($"WebSocket connection established for sector: {sectorId}");
            
            webSocketManager.AddClient(sectorId, webSocket);
            
            try
            {
                await webSocketHandler.HandleConnection(context, webSocket, webSocketManager, sectorId);
            }
            catch (Exception ex)
            {
                logger.LogError($"WebSocket error: {ex.Message}");
            }
            finally
            {
                webSocketManager.RemoveClient(webSocket);
                
                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closing connection.",
                        CancellationToken.None);
                }
            }
        }
        catch (Exception ex)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError($"Error handling WebSocket request: {ex.Message}");
        }
    }
    else
    {
        await next();
    }
});

app.UseHangfireDashboard();


    app.UseSwagger();


    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "WhatsApp API v1");
   });

using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobManager.AddOrUpdate(
        "CheckNewSchedules", 
        () => scope.ServiceProvider.GetRequiredService<MessageSchedulingService>().ScheduleAllMessagesAsync(),
        Cron.Minutely 
    );
}



app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Mapear o hub
app.MapHub<ChatHub>("/chatHub");

// Adiciona o middleware de logging de exceções
app.UseMiddleware<ExceptionLoggingMiddleware>();

try
{
    Log.Information("Iniciando aplicação");
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicação terminou inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}
