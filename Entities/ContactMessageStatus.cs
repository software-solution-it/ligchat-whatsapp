namespace WhatsAppProject.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    namespace WhatsAppProject.Entities
    {
        [Table("contact_message_status")]
        public class ContactMessageStatus
        {
            [Key]
            [Column("id")]
            public int Id { get; set; }

            [Column("contact_id")]
            public int ContactId { get; set; }

            [Column("message_scheduling_id")]
            public int MessageSchedulingId { get; set; }

            [Column("is_sent")]
            public bool IsSent { get; set; } = false; // Indica se a mensagem foi enviada para este contato

            [Column("sent_at")]
            public DateTime? SentAt { get; set; } // Data e hora em que a mensagem foi enviada
        }
    }

}
