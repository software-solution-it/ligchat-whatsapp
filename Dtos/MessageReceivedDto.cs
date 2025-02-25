public class MessageReceivedDto
{
    public int Id { get; set; }
    public string Content { get; set; }
    public string MediaType { get; set; }
    public string MediaUrl { get; set; }
    public int ContactID { get; set; }
    public DateTime SentAt { get; set; }
    public bool IsSent { get; set; }
    public bool IsRead { get; set; }
    public string FileName { get; set; }
} 