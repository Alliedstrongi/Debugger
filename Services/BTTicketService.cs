using Debugger.Data;
using Debugger.Models;
using Debugger.Models.Enums;
using Debugger.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Debugger.Services
{
	public class BTTicketService : IBTTicketService
	{
		private readonly ApplicationDbContext _context;
		private readonly IBTRolesService _rolesService;

        public BTTicketService(ApplicationDbContext context, IBTRolesService rolesService)
        {
            _context = context;
            _rolesService = rolesService;
        }

        public async Task AddTicketAsync(Ticket ticket)
        {
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();
        }

        public async Task ArchiveTicketAsync(Ticket ticket, int companyId)
        {
            try
            {
                var existingTicket = await GetTicketByIdAsync(ticket.Id, companyId);
                if (existingTicket != null)
                {
                    existingTicket.Archived = true;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<List<Ticket>> GetArchivedTicketsAsync(int companyId)
        {
            try
            {
                return await _context.Tickets.Where(t => t.Project!.CompanyId == companyId && t.Archived == true).ToListAsync();

            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task<List<Ticket>> GetTicketsByCompanyIdAsync(int companyId)
        {
            try
            {
                return await _context.Tickets.Where(t => t.Project!.CompanyId == companyId).ToListAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<Ticket?> GetTicketByIdAsync(int ticketId, int companyId)
        {
            try
            {
                return await _context.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId && t.Project!.CompanyId == companyId);

            }
            catch (Exception)
            {

                throw;
            }        }
        public async Task<List<TicketType>> GetTicketTypes()
        {
            try
            {
                return await _context.TicketTypes.ToListAsync();

            }
            catch (Exception)
            {

                throw;
            }        }

        public async Task<List<TicketPriority>> GetTicketPriorities()
        {
            try
            {
                return await _context.TicketPriorities.ToListAsync();

            }
            catch (Exception)
            {

                throw;
            }        }

        public async Task RestoreTicketAsync(Ticket ticket, int companyId)
        {
            try
            {
                var existingTicket = await GetTicketByIdAsync(ticket.Id, companyId);
                if (existingTicket != null)
                {
                    existingTicket.Archived = false;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception)
            {
                throw;
            }

        }

        public async Task UpdateTicketAsync(Ticket ticket, int companyId)
        {
            if (await _context.Projects.AnyAsync(p => p.CompanyId == companyId && p.Id == ticket.ProjectId))
            {
                _context.Update(ticket);
                await _context.SaveChangesAsync();
            }
            else
            {
                throw new InvalidOperationException("Project not found");
            }

        }

        public async Task<List<Ticket>> GetTicketsByUserIdAsync(string userId)
        {
            try
            {
                BTUser? user = await _context.Users.FindAsync(userId);
                if (user is null) return new List<Ticket>();

                //admin
                if (await _rolesService.IsUserInRole(user, nameof(BTRoles.Admin)))
                {
                    return await GetTicketsByCompanyIdAsync(user.CompanyId);
                }
                //PM
                else if (await _rolesService.IsUserInRole(user, nameof(BTRoles.ProjectManager)))
                {
                    return await _context.Tickets
                                         .Include(t => t.TicketStatus)
                                         .Include(t => t.TicketType)
                                         .Include(t => t.TicketPriority)
                                         .Include(t => t.SubmitterUser)
                                         .Include(t => t.DeveloperUser)
                                         .Include(t => t.Project)
                                            .ThenInclude(p => p.Members)
                                         .Where(t => !t.Archived && t.Project!.Members.Contains(user))
                                         .ToListAsync();
                }
                else
                {
                    return await _context.Tickets
                                         .Include(t => t.TicketStatus)
                                         .Include(t => t.TicketType)
                                         .Include(t => t.TicketPriority)
                                         .Include(t => t.SubmitterUser)
                                         .Include(t => t.DeveloperUser)
                                         .Include(t => t.Project)
                                            .ThenInclude(p => p.Members)
                                         .Where(t => !t.Archived && (t.DeveloperUserId == userId || t.SubmitterUserId == userId))
                                         .ToListAsync();
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<List<TicketStatus>> GetTicketStatuses()
        {
            try
            {
                return await _context.TicketStatuses.ToListAsync();

            }
            catch (Exception)
            {

                throw;
            }        }

        public async Task<Ticket?> GetTicketAsNoTrackingAsync(int ticketId, int companyId)
        {
            return await _context.Tickets
                .AsNoTracking()
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == ticketId && t.Project.CompanyId == companyId);
        }

        public Task AddTicketAttachmentAsync(TicketAttachment ticketAttachment)
        {
            throw new NotImplementedException();
        }

        public Task<List<Ticket>> GetUnassignedTicketsAsync(int companyId)
        {
            throw new NotImplementedException();
        }

        public Task AddTicketCommentAsync(TicketComment comment)
        {
            throw new NotImplementedException();
        }

        public Task<TicketAttachment?> GetTicketAttachmentByIdAsync(int ticketAttachmentId)
        {
            throw new NotImplementedException();
        }
    }
}