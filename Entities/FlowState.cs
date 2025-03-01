using System;

namespace WhatsAppProject.Entities
{
    public class FlowState
    {
        public int Id { get; set; }
        public int ContactId { get; set; }
        public string FlowId { get; set; }
        public string CurrentNodeId { get; set; }
        public DateTime LastUpdated { get; set; }
    }
} 