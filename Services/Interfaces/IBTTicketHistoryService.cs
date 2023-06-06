using Debugger.Models;

namespace Debugger.Services.Interfaces
{
    public interface IBTTicketHistoryService
    {
        Task AddHistoryAsync(Ticket? oldTicket, Ticket newTicket, string userId);
        Task AddHistoryAsync(int ticketId, string model, string userId);
        Task<List<TicketHistory>> GetProjectTicketHistoriesAsync(int projectId, int companyId);
        Task<List<TicketHistory>> GetCompanyTicketHistoriesAsnyc(int companyId);
    }
}
