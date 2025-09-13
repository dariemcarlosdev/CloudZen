using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudZen.Services.Abstractions
{
    public interface ITicketService
    {
        Task<List<TicketDto>> GetAllTicketsAsync();
    }
}
