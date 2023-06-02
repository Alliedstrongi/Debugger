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
			_context.Add(ticket);
			await _context.SaveChangesAsync();
		}

		public async Task ArchiveTicketAsync(Ticket ticket, int companyId)
		{
			ticket.Archived = true;
			ticket.ArchivedByProject = true;
			await _context.SaveChangesAsync();
		}

		public async Task<List<Ticket>> GetArchivedTicketsAsync(int companyId)
		{
			return await _context.Tickets
				.Where(t => t.Project.CompanyId == companyId && t.Archived)
				.Include(t => t.DeveloperUser)
				.Include(t => t.Project)
				.Include(t => t.SubmitterUser)
				.Include(t => t.TicketPriority)
				.Include(t => t.TicketStatus)
				.Include(t => t.TicketType)
				.ToListAsync();
		}

		public async Task<Ticket?> GetTicketByIdAsync(int ticketId, int companyId)
		{
			return await _context.Tickets
				.Where(t => t.Project.CompanyId == companyId)
				.Include(t => t.DeveloperUser)
				.Include(t => t.Project)
				.Include(t => t.SubmitterUser)
				.Include(t => t.TicketPriority)
				.Include(t => t.TicketStatus)
				.Include(t => t.TicketType)
				.FirstOrDefaultAsync(t => t.Id == ticketId);
		}

		public async Task<List<TicketPriority>> GetTicketPriorities()
		{
			return await _context.TicketPriorities.ToListAsync();
		}

		public async Task<List<Ticket>> GetTicketsByCompanyIdAsync(int companyId)
		{
			return await _context.Tickets
				.Where(t => t.Project.CompanyId == companyId && !t.Archived)
				.Include(t => t.DeveloperUser)
				.Include(t => t.Project)
				.Include(t => t.SubmitterUser)
				.Include(t => t.TicketPriority)
				.Include(t => t.TicketStatus)
				.Include(t => t.TicketType)
				.ToListAsync();
		}

		public async Task<List<Ticket>> GetTicketsByUserIdAsync(string userId)
		{
			try
			{
				BTUser? user = await _context.Users.FindAsync(userId);
				if (user is null) return new List<Ticket>();

				if (await _rolesService.IsUserInRole(user, nameof(BTRoles.Admin)))
				{
					return await GetTicketsByCompanyIdAsync(user.CompanyId);
				}
				else if(await _rolesService.IsUserInRole(user, nameof(BTRoles.ProjectManager)))
				{
					return await _context.Tickets
										 .Include(t => t.TicketType)
										 .Include(t => t.TicketStatus)
										 .Include(t => t.TicketPriority)
										 .Include(t => t.SubmitterUser)
										 .Include(t => t.DeveloperUser)
										 .Include(t => t.Project)
											.ThenInclude(p => p!.Members)
										 .Where(t => !t.Archived && t.Project!.Members.Contains(user))
										 .ToListAsync();
				}
				else
				{
					return await _context.Tickets
										 .Include(t => t.TicketType)
										 .Include(t => t.TicketStatus)
										 .Include(t => t.TicketPriority)
										 .Include(t => t.SubmitterUser)
										 .Include(t => t.DeveloperUser)
										 .Include(t => t.Project)
											.ThenInclude(p => p!.Members)
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

		public async Task<List<TicketType>> GetTicketTypes()
		{
			return await _context.TicketTypes.ToListAsync();
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
			ticket.Updated = DateTime.UtcNow;
			_context.Update(ticket);
			await _context.SaveChangesAsync();
		}

/*        public async Task<bool> AddProjectManagerAsync(string userId, int projectId, int companyId)
        {
            try
            {
                Project? project = await _context.Projects.Include(p => p.Members).FirstOrDefaultAsync(p => p.Id == projectId && p.CompanyId == companyId);

                BTUser? projectManager = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.CompanyId == companyId);

                if (project is not null && projectManager is not null)
                {
                    if (!await _rolesService.IsUserInRole(projectManager, nameof(BTRoles.ProjectManager))) return false;

                    await RemoveProjectManagerAsync(projectId, companyId);

                    project.Members.Add(projectManager);
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

        public async Task<BTUser?> GetProjectManagerAsync(int projectId, int companyId)
        {
            try
            {
                Project? project = await _context.Projects
                                                .AsNoTracking()
                                                .Include(p => p.Members)
                                                .FirstOrDefaultAsync(p => p.Id == projectId && p.CompanyId == companyId);

                if (project is not null)
                {
                    foreach (BTUser member in project.Members)
                    {
                        if (await _rolesService.IsUserInRole(member, nameof(BTRoles.ProjectManager)))
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

        public async Task RemoveProjectManagerAsync(int projectId, int companyId)
        {
            try
            {
                Project? project = await _context.Projects
                                                 .Include(p => p.Members)
                                                 .FirstOrDefaultAsync(p => p.Id == projectId && p.CompanyId == companyId);

                if (project is not null)
                {
                    foreach (BTUser member in project.Members)
                    {
                        if (await _rolesService.IsUserInRole(member, nameof(BTRoles.ProjectManager)))
                        {
                            project.Members.Remove(member);
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

        public async Task<List<Ticket>> GetUnassignedTicketsByCompanyIdAsync(int companyId)
        {
            try
            {
                List<Ticket> allTickets = await GetTicketsByCompanyIdAsync(companyId);
                List<Ticket> unassignedTickets = new();

                foreach (Ticket ticket in allTickets)
                {
                    BTUser? ticketManager = await GetTicketManagerAsync(ticket.Id, companyId);

                    if (ticketManager is null) unassignedTickets.Add(ticket);
                }
                return unassignedTickets;
            }
            catch (Exception)
            {

                throw;
            }
        } */
    }
}