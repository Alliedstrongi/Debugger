using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Debugger.Data;
using Debugger.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Debugger.Extensions;
using Debugger.Models.Enums;
using Debugger.Services.Interfaces;

namespace Debugger.Controllers
{
	[Authorize]
	public class TicketsController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly UserManager<BTUser> _userManager;
		private readonly IBTTicketService _ticketService;

		public TicketsController(ApplicationDbContext context, UserManager<BTUser> userManager, IBTTicketService ticketService)
		{
			_context = context;
			_userManager = userManager;
			_ticketService = ticketService;
		}

		// GET: Tickets
		public async Task<IActionResult> Index()
		{
			BTUser user = await _userManager.GetUserAsync(User);
			int companyId = User.Identity!.GetCompanyId();

			List<Ticket> tickets = await _ticketService.GetTicketsByCompanyIdAsync(companyId);

			return View(tickets);
		}

		// GET: Tickets/Details/5
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null || _context.Tickets == null)
			{
				return NotFound();
			}

			BTUser user = await _userManager.GetUserAsync(User);

			Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value, user.CompanyId);

			if (ticket == null)
			{
				return NotFound();
			}

			return View(ticket);
		}

		// GET: Tickets/Create
		public async Task<IActionResult> Create()
		{
			BTUser user = await _userManager.GetUserAsync(User);

			List<Project> projects = await _context.Projects.Where(c => c.CompanyId == user!.CompanyId).ToListAsync();

			ViewData["ProjectId"] = new SelectList(projects, "Id", "Name");
			ViewData["TicketPriorityId"] = new SelectList(await _ticketService.GetTicketPriorities(), "Id", "Name");
			ViewData["TicketTypeId"] = new SelectList(await _ticketService.GetTicketTypes(), "Id", "Name");
			return View();
		}

		// POST: Tickets/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create([Bind("Title,Description,TicketTypeId,TicketPriorityId,ProjectId")] Ticket ticket)
		{
			if (ModelState.IsValid)
			{
				ticket.Created = DateTime.UtcNow;

				await _ticketService.AddTicketAsync(ticket);

				return RedirectToAction(nameof(Index));
			}

			BTUser user = await _userManager.GetUserAsync(User);

			List<Project> projects = await _context.Projects.Where(c => c.CompanyId == user!.CompanyId).ToListAsync();

			ViewData["ProjectId"] = new SelectList(projects, "Id", "Name");
			ViewData["TicketPriorityId"] = new SelectList(await _ticketService.GetTicketPriorities(), "Id", "Name");
			ViewData["TicketTypeId"] = new SelectList(await _ticketService.GetTicketTypes(), "Id", "Name");
			return View(ticket);
		}

		// GET: Tickets/Edit/5
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null || _context.Tickets == null)
			{
				return NotFound();
			}

			BTUser user = await _userManager.GetUserAsync(User);

			Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value, user.CompanyId);

			if (ticket == null)
			{
				return NotFound();
			}

			List<Project> projects = await _context.Projects.Where(c => c.CompanyId == user!.CompanyId).ToListAsync();

			ViewData["ProjectId"] = new SelectList(projects, "Id", "Name");
			ViewData["TicketPriorityId"] = new SelectList(await _ticketService.GetTicketPriorities(), "Id", "Name", ticket.TicketPriorityId);
			ViewData["TicketStatusId"] = new SelectList(await _ticketService.GetTicketStatuses(), "Id", "Name", ticket.TicketStatusId);
			ViewData["TicketTypeId"] = new SelectList(await _ticketService.GetTicketTypes(), "Id", "Name", ticket.TicketTypeId);
			return View(ticket);
		}

		// POST: Tickets/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Created,Updated,Archived,ArchivedByProject,ProjectId,TicketTypeId,TicketStatusId,TicketPriorityId,DeveloperUserId,SubmitterUserId")] Ticket ticket)
		{
			if (id != ticket.Id)
			{
				return NotFound();
			}

			BTUser? user = await _userManager.GetUserAsync(User);
			int companyId = User.Identity!.GetCompanyId();

			if (ModelState.IsValid)
			{
				try
				{
					ticket.Created = DateTime.SpecifyKind(ticket.Created, DateTimeKind.Utc);
					ticket.Updated = DateTime.UtcNow;

					await _ticketService.UpdateTicketAsync(ticket, companyId);

					return RedirectToAction(nameof(Index));
				}
				catch (DbUpdateConcurrencyException)
				{
					return NotFound();
				}
			}0

			List<Project> projects = await _context.Projects.Where(c => c.CompanyId == user!.CompanyId).ToListAsync();

			ViewData["ProjectId"] = new SelectList(projects, "Id", "Name");
			ViewData["TicketPriorityId"] = new SelectList(_context.TicketPriorities, "Id", "Id", ticket.TicketPriorityId);
			ViewData["TicketStatusId"] = new SelectList(_context.TicketStatuses, "Id", "Id", ticket.TicketStatusId);
			ViewData["TicketTypeId"] = new SelectList(_context.TicketTypes, "Id", "Id", ticket.TicketTypeId);

			return View(ticket);
		}



		// GET: Tickets/Delete/5
		public async Task<IActionResult> Archive(int? id)
		{
			if (id == null || _context.Tickets == null)
			{
				return NotFound();
			}
			BTUser user = await _userManager.GetUserAsync(User);

			Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value, user.CompanyId);

			if (ticket == null)
			{
				return NotFound();
			}

			return View(ticket);
		}

		// POST: Tickets/Delete/5
		[HttpPost, ActionName("Archive")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ArchiveConfirmed(int id)
		{
			if (_context.Tickets == null)
			{
				return Problem("Entity set 'ApplicationDbContext.Tickets' is null.");
			}

			var ticket = await _context.Tickets.FindAsync(id);
			if (ticket != null)
			{
				BTUser? user = await _userManager.GetUserAsync(User);
				int companyId = User.Identity!.GetCompanyId();

				await _ticketService.ArchiveTicketAsync(ticket, companyId);
			}

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}
	}
}
