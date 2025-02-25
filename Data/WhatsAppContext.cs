using Eon.Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using WhatsAppProject.Entities;
using WhatsAppProject.Entities.WhatsAppProject.Entities;
using Microsoft.Extensions.Logging;

namespace WhatsAppProject.Data
{
    public class WhatsAppContext : DbContext
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<WhatsAppContext> _logger;

        public WhatsAppContext(
            DbContextOptions<WhatsAppContext> options,
            ILoggerFactory loggerFactory) : base(options)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<WhatsAppContext>();
        }

        public DbSet<Messages> Messages { get; set; }
        public DbSet<MediaFile> MediaFiles { get; set; }
        public DbSet<Contacts> Contacts { get; set; }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Iniciando SaveChangesAsync no WhatsAppContext");
                
                // Log das entidades que serão alteradas
                foreach (var entry in ChangeTracker.Entries())
                {
                    _logger.LogDebug($"Entity: {entry.Entity.GetType().Name}, State: {entry.State}");
                    if (entry.State == EntityState.Modified)
                    {
                        foreach (var prop in entry.Properties)
                        {
                            if (prop.IsModified)
                            {
                                _logger.LogDebug($"Propriedade modificada: {prop.Metadata.Name}, " +
                                               $"Valor Original: {prop.OriginalValue}, " +
                                               $"Novo Valor: {prop.CurrentValue}");
                            }
                        }
                    }
                }

                var result = await base.SaveChangesAsync(cancellationToken);
                _logger.LogInformation($"SaveChangesAsync concluído. Registros afetados: {result}");
                return result;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError($"Erro ao salvar alterações no WhatsAppContext");
                _logger.LogError($"Mensagem: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                    _logger.LogError($"Inner stack trace: {ex.InnerException.StackTrace}");
                }
                throw;
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Messages>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).HasMaxLength(4000);
                entity.Property(e => e.MediaType).HasMaxLength(50);
                entity.Property(e => e.MediaUrl).HasMaxLength(255);
                
                _logger.LogInformation("Configurando entidade Messages");
            });

            modelBuilder.Entity<Contacts>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(15);
                entity.Property(e => e.ProfilePictureUrl).HasMaxLength(255);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.Address).HasMaxLength(255);
                entity.Property(e => e.Annotations).HasMaxLength(4000);

                _logger.LogInformation("Configurando entidade Contacts");
            });

            modelBuilder.Entity<MediaFile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Url).IsRequired().HasMaxLength(255);
                entity.Property(e => e.MediaType).IsRequired().HasMaxLength(50);

                _logger.LogInformation("Configurando entidade MediaFile");
            });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder
                    .UseLoggerFactory(_loggerFactory)
                    .EnableSensitiveDataLogging()
                    .EnableDetailedErrors();
            }
        }
    }

    public class SaasDbContext : DbContext
    {
        private readonly ILogger<SaasDbContext> _logger;

        public SaasDbContext(
            DbContextOptions<SaasDbContext> options,
            ILogger<SaasDbContext> logger) : base(options)
        {
            _logger = logger;
        }

        public DbSet<Sector> Sector { get; set; }
        public DbSet<ContactMessageStatus> ContactMessageStatus { get; set; }
        public DbSet<ContactFlowStatus> ContactFlowStatus { get; set; }
        public DbSet<MessageScheduling> MessageScheduling { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Webhook> Webhooks { get; set; }
        public DbSet<Contacts> Contacts { get; set; }
        public DbSet<FlowDTO> Flows { get; set; }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Iniciando SaveChangesAsync no SaasDbContext");
                
                foreach (var entry in ChangeTracker.Entries())
                {
                    _logger.LogDebug($"Entity: {entry.Entity.GetType().Name}, State: {entry.State}");
                    if (entry.State == EntityState.Modified)
                    {
                        foreach (var prop in entry.Properties)
                        {
                            if (prop.IsModified)
                            {
                                _logger.LogDebug($"Propriedade modificada: {prop.Metadata.Name}, " +
                                               $"Valor Original: {prop.OriginalValue}, " +
                                               $"Novo Valor: {prop.CurrentValue}");
                            }
                        }
                    }
                }

                var result = await base.SaveChangesAsync(cancellationToken);
                _logger.LogInformation($"SaveChangesAsync concluído. Registros afetados: {result}");
                return result;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError($"Erro ao salvar alterações no SaasDbContext");
                _logger.LogError($"Mensagem: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                    _logger.LogError($"Inner stack trace: {ex.InnerException.StackTrace}");
                }
                throw;
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
                .LogTo(message => _logger.LogDebug(message));

            base.OnConfiguring(optionsBuilder);
        }
    }
}
