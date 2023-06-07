using Debugger.Models;
using Debugger.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Debugger.Services.Interfaces
{
    public interface IBTTicketService
    {
        public Task AddTicketAsync(Ticket ticket);
        public Task ArchiveTicketAsync(Ticket ticket, int companyId);
        public Task<List<Ticket>> GetArchivedTicketsAsync(int companyId);
        public Task<List<Ticket>> GetTicketsByCompanyIdAsync(int companyId);
        public Task<Ticket?> GetTicketByIdAsync(int ticketId, int companyId);
        public Task<BTUser?> GetTicketDeveloperAsync(int ticketId, int companyId);

		public Task<List<TicketStatus>> GetTicketStatuses();
        public Task<List<TicketType>> GetTicketTypes();
        public Task<List<TicketPriority>> GetTicketPriorities();
        public Task RestoreTicketAsync(Ticket ticket, int companyId);
        public Task UpdateTicketAsync(Ticket ticket, int companyId);
        public Task<List<Ticket>> GetTicketsByUserIdAsync(string userId);
        public Task<List<Ticket>> GetUnassignedTicketsByCompanyIdAsync(int companyId);
        Task<Ticket?> GetTicketAsNoTrackingAsync(int ticketId, int companyId);
        Task<bool> AddTicketDeveloperAsync(string userId, int ticketId, int companyId);
        Task RemoveTicketDeveloperAsync(int ticketId, int companyId);
        Task AddTicketAttachmentAsync(TicketAttachment ticketAttachment);
        Task AddTicketCommentAsync(TicketComment comment);
    }
}
