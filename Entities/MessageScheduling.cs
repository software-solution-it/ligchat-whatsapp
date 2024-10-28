using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WhatsAppProject.Entities
{
    [Table("mensagens_agendadas")]
    public class MessageScheduling
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("nome")]
        [Required]
        public string Name { get; set; }

        [Column("mensagem_de_texto")]
        public string? MessageText { get; set; }

        [Column("data_envio")]
        public string? SendDate { get; set; }

        [Column("id_fluxo")]
        public string FlowId { get; set; }

        [Column("setor_id")]
        public int? SectorId { get; set; }

        [Column("data_criacao")]
        public DateTime CreatedAt { get; set; }

        [Column("data_atualizacao")]
        public DateTime UpdatedAt { get; set; }

        [Column("nome_imagem")]
        public string? ImageName { get; set; }

        [Column("nome_arquivo")]
        public string? FileName { get; set; }

        [Column("imagem_anexo")]
        public string? ImageAttachment { get; set; }

        [Column("arquivo_anexo")]
        public string? FileAttachment { get; set; }

        [Column("imagem_mimetype")]
        public string? ImageMimeType { get; set; }

        [Column("arquivo_mimetype")]
        public string? FileMimeType { get; set; }

        [Column("tag_id")]
        public string? Tags { get; set; }

        public MessageScheduling()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            Name = string.Empty;
        }

        public MessageScheduling(string name, string messageText, int userId, string sendDate, string flowId, string status, int? sectorId = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name), "O nome da mensagem não pode ser nulo.");
            MessageText = messageText;
            SendDate = sendDate;
            FlowId = flowId;
            SectorId = sectorId;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
