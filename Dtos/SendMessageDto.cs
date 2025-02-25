using System.ComponentModel.DataAnnotations;

namespace WhatsAppProject.Dtos
{
    public class SendMessageDto
    {
        [Required]
        public string To { get; set; }

        [Required]
        public string RecipientPhone { get; set; }

        [Required]
        public string Text { get; set; }

        public int ContactId { get; set; }
        public int SectorId { get; set; }
    }
} 