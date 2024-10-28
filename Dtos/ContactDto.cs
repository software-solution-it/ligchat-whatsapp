using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace WhatsAppProject.Dtos
{
    public class ContactDto
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(15)]
        public string PhoneNumber { get; set; }

        public string? ProfilePictureUrl { get; set; }
        public int? SectorId { get; set; }

        // Mudança de TagIds para uma lista de strings
        public string? TagIds { get; set; }

        public int? Status { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? Annotations { get; set; }

        public DateTime CreatedAt { get; set; } // Adicionado para data de criação
        public DateTime UpdatedAt { get; set; } // Adicionado para data da última atualização
    }
}
