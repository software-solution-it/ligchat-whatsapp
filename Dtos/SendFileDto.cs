namespace WhatsAppProject.Dtos
{
    public class SendFileDto
    {
        public string Base64File { get; set; }
        public string MediaType { get; set; }
        public string FileName { get; set; }
        public string Caption { get; set; }
        public string Recipient { get; set; }
        public int ContactId { get; set; }
        public int SectorId { get; set; }
    }
}
