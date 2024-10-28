namespace WhatsAppProject.Controllers
{
    internal class WebhookMessage
    {
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Content { get; set; }

        public string MediaType { get; set; }
        public string MediaUrl { get; set; }

        public string ProfilePictureUrl { get; set; }
    }
}
