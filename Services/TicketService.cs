using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudZen.Services.Abstractions;

namespace CloudZen.Services
{
    public class TicketService : ITicketService
    {
        // Sample in-memory tickets. Replace with API calls / persistence as needed.
        private readonly List<TicketDto> _tickets = new()
        {
            new TicketDto { Id = "TCK-001", Title = "Login failure on iOS", IsOpen = true, CreatedAt = DateTime.UtcNow.AddHours(-6), Summary = "User unable to login from iOS app." },
            new TicketDto { Id = "TCK-002", Title = "Report export error", IsOpen = false, CreatedAt = DateTime.UtcNow.AddDays(-2), Summary = "Export to PDF fails for large reports." },
            new TicketDto { Id = "TCK-003", Title = "SSO configuration question", IsOpen = true, CreatedAt = DateTime.UtcNow.AddHours(-48), Summary = "Customer needs help with SSO setup." },
            new TicketDto { Id = "TCK-004", Title = "Dashboard slow", IsOpen = false, CreatedAt = DateTime.UtcNow.AddDays(-7), Summary = "Dashboard loads slowly for some users." }
        };

        public Task<List<TicketDto>> GetAllTicketsAsync() => Task.FromResult(_tickets.OrderByDescending( t => t.CreatedAt).ToList());

        public Task<int> GetClosedCountAsync() => Task.FromResult(_tickets.Count(t => !t.IsOpen));

        public Task<int> GetOpenCountAsync() => Task.FromResult(_tickets.Count(t => t.IsOpen));
    }
}
