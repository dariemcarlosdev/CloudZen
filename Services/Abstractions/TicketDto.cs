using Microsoft.VisualBasic;

namespace CloudZen.Services.Abstractions
{
    public class TicketDto
    {
        public string Id { get; set; } 
        public string Title { get; set; } 
        public bool IsOpen { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Summary { get; set; }
    }
}