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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configuração do DbContext para MySQL (WhatsAppContext)
builder.Services.AddDbContext<WhatsAppContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 26)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure()
    )
    .EnableSensitiveDataLogging() // Para depurar consultas
    .EnableDetailedErrors() // Para ajudar na depuração de erros
);

// Configuração do DbContext para SaaS (SaasDbContext)
builder.Services.AddDbContext<SaasDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("SaasDatabase"),
        new MySqlServerVersion(new Version(8, 0, 26)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure()
    )
    .EnableSensitiveDataLogging()
    .EnableDetailedErrors()
);

// Configuração do MongoDB
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDb");
var mongoDatabaseName = builder.Configuration["DatabaseName"];

// Registrar o IMongoClient como singleton
builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConnectionString));

// Registrar MongoDbContext como scoped para lidar melhor com ciclo de vida
builder.Services.AddScoped<MongoDbContext>(sp => new MongoDbContext(sp.GetRequiredService<IMongoClient>(), mongoDatabaseName));

// Adicionar serviços de WhatsApp e HttpClient
builder.Services.AddScoped<WhatsAppService>();
builder.Services.AddScoped<ContactService>();
builder.Services.AddScoped<WebhookService>();
builder.Services.AddScoped<MessageSchedulingService>();
builder.Services.AddSingleton<WebSocketManager>();
builder.Services.AddHttpClient(); // Para enviar requisições HTTP

// Configuração do Hangfire com MySQL
builder.Services.AddHangfire(config =>
{
    var storageOptions = new MySqlStorageOptions
    {
        PrepareSchemaIfNecessary = true,  // Cria as tabelas no banco se elas não existirem
        QueuePollInterval = TimeSpan.FromSeconds(15), // Reduzir a frequência de polling para aliviar carga
        JobExpirationCheckInterval = TimeSpan.FromHours(1)
    };

    config.UseStorage(new MySqlStorage(builder.Configuration.GetConnectionString("DefaultConnection"), storageOptions));
});

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 1; // Reduzir o número de workers para evitar sobrecarregar o banco
});

// Registre `IRecurringJobManager` e `IBackgroundJobClient` para injeção de dependência
builder.Services.AddSingleton<IRecurringJobManager, RecurringJobManager>();
builder.Services.AddSingleton<IBackgroundJobClient, BackgroundJobClient>();

// Adicionar configuração do Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "WhatsApp API", Version = "v1" });
});

var app = builder.Build();

// Configuração do pipeline
app.UseWebSockets(); // Habilitar WebSockets

app.Use(async (context, next) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocketManager = context.RequestServices.GetRequiredService<WebSocketManager>();
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();

        // Obtenha o setor a partir da query
        var sectorId = context.Request.Query["sectorId"];

        if (!string.IsNullOrEmpty(sectorId)) // Verifique se o setor foi fornecido
        {
            webSocketManager.AddClient(sectorId, webSocket); // Adicione o cliente ao setor
            await Echo(context, webSocket, webSocketManager, sectorId); // Passando o sectorId para a função Echo
        }
        else
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Sector ID is required.", CancellationToken.None);
        }
    }
    else
    {
        await next();
    }
});

// Método para ecoar mensagens do WebSocket
async Task Echo(HttpContext context, WebSocket webSocket, WebSocketManager webSocketManager, string sectorId)
{
    var buffer = new byte[1024 * 4];
    WebSocketReceiveResult result;

    do
    {
        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        if (result.MessageType == WebSocketMessageType.Text)
        {
            var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
            Console.WriteLine($"Received message: {message} from sector: {sectorId}");

            // Aqui você pode enviar a mensagem para todos os clientes do setor
            await webSocketManager.SendMessageToSectorAsync(sectorId, message);
        }
    } while (!result.CloseStatus.HasValue);

    webSocketManager.RemoveClient(webSocket); // Remover o cliente ao fechar a conexão
    await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
}

// Configuração do painel de controle do Hangfire
app.UseHangfireDashboard();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    // Habilitar o Swagger
    app.UseSwagger();

    // Configurar a UI do Swagger para servir no caminho raiz
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "WhatsApp API v1");
    });
}

// Use `IRecurringJobManager` para configurar o job recorrente para rodar a cada minuto
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobManager.AddOrUpdate(
        "CheckNewSchedules", // Identificador do job
        () => scope.ServiceProvider.GetRequiredService<MessageSchedulingService>().ScheduleAllMessagesAsync(),
        Cron.Minutely // Usando o método Cron para melhorar a legibilidade
    );
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
