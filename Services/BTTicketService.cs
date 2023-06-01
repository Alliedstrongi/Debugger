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
			ticket.Archived = false;
			ticket.ArchivedByProject = false;
			await _context.SaveChangesAsync();
		}

		public async Task UpdateTicketAsync(Ticket ticket, int companyId)
		{
			ticket.Updated = DateTime.UtcNow;
			_context.Update(ticket);
			await _context.SaveChangesAsync();
		}
	}
}