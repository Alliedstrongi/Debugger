using Debugger.Data;
using Debugger.Models;
using Debugger.Models.Enums;
using Debugger.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<BTUser> _userManager;

        public BTTicketService(ApplicationDbContext context, IBTRolesService rolesService, UserManager<BTUser> userManager)
        {
            _context = context;
            _rolesService = rolesService;
            _userManager = userManager;
        }

        public async Task AddTicketAsync(Ticket ticket)
        {
			try
			{
				_context.Tickets.Add(ticket);
				await _context.SaveChangesAsync();
			}
			catch (Exception)
			{

				throw;
			}
		}
        public async Task<bool> AddTicketDeveloperAsync(string userId, int ticketId, int companyId)
        {
            try
            {
                Ticket? ticket = await _context.Tickets.Include(t => t.Project!.Members).FirstOrDefaultAsync(t => t.Id == ticketId && t.Project!.CompanyId == companyId);

                BTUser? ticketDeveloper = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.CompanyId == companyId);

                if (ticket is not null && ticketDeveloper is not null)
                {
                    if (!await _rolesService.IsUserInRole(ticketDeveloper, nameof(BTRoles.Developer))) return false;

                    await RemoveTicketDeveloperAsync(ticketId, companyId);

                    ticket.Project!.Members.Add(ticketDeveloper);
                    await _context.SaveChangesAsync();

                    return true;
                }

            }
            catch (Exception)
            {

                throw;
            }
            return false;
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
            return await _context.Tickets.Where(t => t.Project!.CompanyId == companyId).ToListAsync();
        }

        public async Task<Ticket?> GetTicketByIdAsync(int ticketId, int companyId)
        {
            return await _context.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId && t.Project!.CompanyId == companyId);
        }

        public async Task<BTUser?> GetTicketDeveloperAsync(int ticketId, int companyId)
        {
            try
            {
                Ticket? ticket = await _context.Tickets
                                                .AsNoTracking()
                                                .Include(t => t.Project)
                                                .FirstOrDefaultAsync(t => t.Id == ticketId && t.Project!.CompanyId == companyId);

                if (ticket is not null)
                {
                    foreach (BTUser member in ticket.Project!.Members)
                    {
                        if (await _rolesService.IsUserInRole(member, nameof(BTRoles.Developer)))
                        {
                            return member;
                        }
                    }
                }

                return null;
            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task<List<TicketType>> GetTicketTypes()
        {
            return await _context.TicketTypes.ToListAsync();
        }

        public async Task<List<TicketPriority>> GetTicketPriorities()
        {
            return await _context.TicketPriorities.ToListAsync();
        }

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
            return await _context.TicketStatuses.ToListAsync();
        }

        public async Task<List<Ticket>> GetUnassignedTicketsByCompanyIdAsync(int companyId)
        {
            try
            {
                List<Ticket> allTickets = await GetTicketsByCompanyIdAsync(companyId);
                List<Ticket> unassignedProjects = new List<Ticket>();

                foreach (Ticket ticket in allTickets)
                {
                    BTUser? ticketDeveloper = await GetTicketDeveloperAsync(ticket.Id, companyId);

                    if (ticketDeveloper is null) unassignedProjects.Add(ticket);
                }
                return unassignedProjects;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<Ticket?> GetTicketAsNoTrackingAsync(int ticketId, int companyId)
        {
            return await _context.Tickets
                .AsNoTracking()
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == ticketId && t.Project!.CompanyId == companyId);
        }

        public async Task RemoveTicketDeveloperAsync(int ticketId, int companyId)
        {
            try
            {
                Ticket? ticket = await _context.Tickets
                                               .Include(t => t.Project)
                                               .ThenInclude(p => p.Members)
                                               .FirstOrDefaultAsync(t => t.Id == ticketId && t.Project!.CompanyId == companyId);

                if (ticket is not null)
                {
                    foreach (BTUser member in ticket.Project!.Members)
                    {
                        if (await _rolesService.IsUserInRole(member, nameof(BTRoles.Developer)))
                        {
                            ticket.Project.Members.Remove(member);
                        }
                    }

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task AddTicketAttachmentAsync(TicketAttachment ticketAttachment)
        {
            try
            {
                await _context.AddAsync(ticketAttachment);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }

        public Task AddTicketCommentAsync(TicketComment comment)
        {
            throw new NotImplementedException();
        }
    }
}