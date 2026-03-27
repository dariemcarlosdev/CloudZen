using System.Collections.Generic;
using System.Threading.Tasks;

using CloudZen.Features.Tickets.Models;

namespace CloudZen.Features.Tickets.Services
{
    public interface ITicketService
    {
        Task<List<TicketDto>> GetAllTicketsAsync();
    }
}
