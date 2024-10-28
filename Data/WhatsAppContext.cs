using Microsoft.EntityFrameworkCore;
using WhatsAppProject.Entities;
using WhatsAppProject.Entities.WhatsAppProject.Entities;

namespace WhatsAppProject.Data
{
    public class WhatsAppContext : DbContext
    {
        public WhatsAppContext(DbContextOptions<WhatsAppContext> options) : base(options) { }

        public DbSet<Messages> Messages { get; set; }
        public DbSet<MediaFile> MediaFiles { get; set; }
        public DbSet<Contacts> Contacts { get; set; }
    }

    public class SaasDbContext : DbContext
    {
        public SaasDbContext(DbContextOptions<SaasDbContext> options) : base(options) { }

        public DbSet<Sector> Sector { get; set; }
        public DbSet<ContactMessageStatus> ContactMessageStatus { get; set; }
        public DbSet<ContactFlowStatus> ContactFlowStatus { get; set; } // Adiciona o controle de status de fluxo
        public DbSet<MessageScheduling> MessageScheduling { get; set; }
        public DbSet<Tag> Tags { get; set; }

        public DbSet<Contacts> Contacts { get; set; }

        public DbSet<FlowDTO> Flows { get; set; }
    }
}
