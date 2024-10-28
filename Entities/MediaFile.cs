using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WhatsAppProject.Entities
{
    [Table("media_files")] // Define o nome da tabela em snake_case
    public class MediaFile
    {
        [Key]
        [Column("id")] // Define o nome da coluna em snake_case
        public int Id { get; set; }

        [Column("url")] // Define o nome da coluna em snake_case
        public string Url { get; set; }

        [Column("media_type")] // Define o nome da coluna em snake_case
        public string MediaType { get; set; } // "audio", "image", "document"

        [Column("uploaded_at")] // Define o nome da coluna em snake_case
        public DateTime UploadedAt { get; set; }
    }
}
