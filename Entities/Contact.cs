using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;

namespace WhatsAppProject.Entities
{
    [Table("contacts")]
    public class Contacts
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("nome", TypeName = "varchar(100)")]
        public string Name { get; set; }

        [Column("numero", TypeName = "varchar(15)")]
        [Required]
        public string PhoneNumber { get; set; }

        [Column("foto_perfil", TypeName = "varchar(255)")]
        public string? ProfilePictureUrl { get; set; }

        [Column("setor_id")]
        [Required]
        public int? SectorId { get; set; }

        // Mudança de TagIds para uma lista de strings
        [Column("tag_id")]
        public string? TagIds { get; set; }

        [Column("status")]
        public int? Status { get; set; }

        [Column("endereco", TypeName = "varchar(255)")]
        public string? Address { get; set; }

        [Column("email", TypeName = "varchar(255)")]
        public string? Email { get; set; }

        [Column("anotacoes", TypeName = "text")]
        public string? Annotations { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public virtual ICollection<Messages> Messages { get; set; } = new List<Messages>();
    }
}
