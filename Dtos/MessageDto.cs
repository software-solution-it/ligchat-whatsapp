namespace WhatsAppProject.Dtos
{
    public class MessageDto
    {
        public string Content { get; set; } // Conteúdo da mensagem

        public string? MediaType { get; set; } // Tipo da mídia (imagem, vídeo, etc.)

        public string? MediaUrl { get; set; } // URL da mídia, se houver

        public int SectorId { get; set; } // ID do setor que enviou a mensagem

        public string Recipient { get; set; } // Número de telefone do destinatário

        public string RecipientPhone { get; set; }  // Nova propriedade

        public int ContactId { get; set; } // ID do contato associado à mensagem
    }

    public class MessageReceivedDto
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public string? MediaType { get; set; }
        public string? MediaUrl { get; set; }
        public int? ContactID { get; set; } // Referência ao ID do contato
        public DateTime? SentAt { get; set; }
        public bool IsSent { get; set; }
    }


    public class UpdateCredentialsDto
    {
        public string PhoneNumber { get; set; } // Número de telefone do WhatsApp
        public string AccessToken { get; set; } // Token de acesso para autenticação
        public int SectorId { get; set; } // ID do setor associado às credenciais
    }
}
