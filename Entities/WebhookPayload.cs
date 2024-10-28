using System.Collections.Generic;

public class WebhookPayload
{
    public string Object { get; set; }
    public List<Entry> Entry { get; set; }
}

public class Entry
{
    public string Id { get; set; }
    public List<Change> Changes { get; set; }
}

public class Change
{
    public string Field { get; set; }
    public ChangeValue Value { get; set; }
}

public class ChangeValue
{
    public string MessagingProduct { get; set; }
    public Metadata Metadata { get; set; }
    public List<Contact> Contacts { get; set; }
    public List<Message> Messages { get; set; }
    public List<Status> Statuses { get; set; }  // Adicione esta linha se os status estiverem aqui
    public List<Error> Errors { get; set; }
}

public class Status
{
    public string Id { get; set; }
    public string Timestamp { get; set; }
    public string RecipientId { get; set; }
}

public class Metadata
{
    public string DisplayPhoneNumber { get; set; }
    public string PhoneNumberId { get; set; }
}

public class Contact
{
    public Profile Profile { get; set; }
    public string WaId { get; set; }  // ID do WhatsApp
}

public class Profile
{
    public string Name { get; set; } // Nome do contato
}

public class Message
{
    public string From { get; set; } // Remetente da mensagem
    public string Id { get; set; } // ID da mensagem
    public string Timestamp { get; set; } // Timestamp da mensagem
    public string Type { get; set; } // Tipo da mensagem (texto, imagem, etc.)
    public Text Text { get; set; } // Detalhes do texto
    public Document Document { get; set; } // Detalhes do documento
    public Image Image { get; set; } // Detalhes da imagem
    public Video Video { get; set; } // Detalhes do vídeo
    public Audio Audio { get; set; } // Detalhes do áudio
}

public class Text
{
    public string Body { get; set; } // Conteúdo da mensagem de texto
}

public class Document
{
    public string Caption { get; set; } // Legenda do documento
    public string Filename { get; set; } // Nome do arquivo
    public string MimeType { get; set; } // Tipo MIME do documento
    public string Id { get; set; } // ID do documento
}

public class Image
{
    public string Caption { get; set; } // Legenda da imagem
    public string MimeType { get; set; } // Tipo MIME da imagem
    public string Id { get; set; } // ID da imagem
}

public class Video
{
    public string Caption { get; set; } // Legenda do vídeo
    public string MimeType { get; set; } // Tipo MIME do vídeo
    public string Id { get; set; } // ID do vídeo
}

public class Audio
{
    public string MimeType { get; set; } // Tipo MIME do áudio
    public string Id { get; set; } // ID do áudio
}

public class Error
{
    public int Code { get; set; } // Código de erro
    public string Title { get; set; } // Título do erro
    public string Message { get; set; } // Mensagem detalhada do erro
    public ErrorData ErrorData { get; set; } // Dados adicionais sobre o erro
}

public class ErrorData
{
    public string Details { get; set; } // Detalhes adicionais sobre o erro
}
