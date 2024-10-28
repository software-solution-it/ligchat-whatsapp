using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WhatsAppProject.Entities
{
    [Table("messages")] // Define o nome da tabela em snake_case
    public class Messages
    {
        [Key]
        [Column("id")] // Define o nome da coluna em snake_case
        public int Id { get; set; }

        [Column("conteudo")] // Define o nome da coluna em snake_case
        public string Content { get; set; } // Conteúdo da mensagem

        [Column("tipo", TypeName = "varchar(50)")] // Tipo da mídia
        public string? MediaType { get; set; } // Tipo da mídia (imagem, vídeo, etc.)

        [Column("url", TypeName = "varchar(255)")] // URL da mídia 
        public string? MediaUrl { get; set; } // URL da mídia, se houver

        [Column("id_setor")] // ID do setor que enviou a mensagem
        [Required]
        public int SectorId { get; set; } // ID do setor

        [Column("contato_id")] // ID do contato que recebeu a mensagem
        [Required]
        public int? ContactID { get; set; } // ID do contato

        [Column("data_envio")] // Data/hora em que a mensagem foi enviada
        public DateTime? SentAt { get; set; } // Data e hora do envio

        [Column("enviado")] // Indica se a mensagem foi enviada
        public bool IsSent { get; set; } = false; // Padrão é false


        [JsonIgnore]
        [ForeignKey("ContactID")]
        public virtual Contacts Contact { get; set; }
    }
}
