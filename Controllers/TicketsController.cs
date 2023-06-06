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
using System.ComponentModel.Design;

namespace Debugger.Controllers
{
	[Authorize]
	public class TicketsController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly UserManager<BTUser> _userManager;
		private readonly IBTTicketService _ticketService;
		private readonly IBTTicketHistoryService _ticketHistoryService;
		private readonly IBTProjectService _projectService;
		private readonly IBTFileService _fileService;

		public TicketsController(ApplicationDbContext context, UserManager<BTUser> userManager, IBTTicketService ticketService, IBTTicketHistoryService ticketHistoryService, IBTProjectService projectService, IBTFileService fileService)
		{
			_context = context;
			_userManager = userManager;
			_ticketService = ticketService;
			_ticketHistoryService = ticketHistoryService;
			_projectService = projectService;
			_fileService = fileService;
		}

		// GET: Tickets
		public async Task<IActionResult> Index()
		{
			BTUser user = await _userManager.GetUserAsync(User);
			int companyId = User.Identity!.GetCompanyId();

			List<Ticket> tickets = await _ticketService.GetTicketsByCompanyIdAsync(companyId);

			return View(tickets);
		}

		public async Task<IActionResult> MyTickets()
		{
			List<Ticket> tickets = await _ticketService.GetTicketsByUserIdAsync(_userManager.GetUserId(User)!);

			ViewData["Title"] = "My Tickets";
			return View(nameof(Index), tickets);
		}

		public async Task<IActionResult> ArchivedTickets()
		{
			List<Ticket> tickets = await _ticketService.GetArchivedTicketsAsync(User.Identity!.GetCompanyId());

			ViewData["Title"] = "Archived Tickets";
			return View(nameof(Index), tickets);
		}

		public async Task<IActionResult> AllTickets()
		{
			List<Ticket> tickets = await _ticketService.GetTicketsByCompanyIdAsync(User.Identity!.GetCompanyId());

			ViewData["Title"] = "All Tickets";
			return View(nameof(Index), tickets);
		}

		public async Task<IActionResult> UnassignedTickets()
		{
			List<Ticket> tickets = await _ticketService.GetTicketsByCompanyIdAsync(User.Identity!.GetCompanyId());

			ViewData["Title"] = "Unassigned Tickets";
			return View(nameof(Index), tickets);
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
		public async Task<IActionResult> Create([Bind("Id,Title,Description,TicketTypeId,TicketPriorityId,ProjectId,TicketStatusId")] Ticket ticket)
		{
			ModelState.Remove(nameof(Ticket.SubmitterUserId));

			if (ModelState.IsValid)
			{
				int companyId = User.Identity!.GetCompanyId();

				ticket.Created = DateTime.UtcNow;
				ticket.SubmitterUserId = _userManager.GetUserId(User);

				await _ticketService.AddTicketAsync(ticket);

				await _ticketHistoryService.AddHistoryAsync(null, ticket, _userManager.GetUserId(User)!);

				return RedirectToAction(nameof(Index));
			}

			List<Project> projects = await _projectService.GetAllProjectsByCompanyIdAsync(User.Identity!.GetCompanyId());

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
					Ticket? oldTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id, User.Identity!.GetCompanyId());
					await _ticketService.UpdateTicketAsync(ticket, User.Identity!.GetCompanyId());

					ticket.Created = DateTime.SpecifyKind(ticket.Created, DateTimeKind.Utc);
					ticket.Updated = DateTime.UtcNow;

					await _ticketHistoryService.AddHistoryAsync(oldTicket, ticket, _userManager.GetUserId(User)!);
				}
				catch (DbUpdateConcurrencyException)
				{
					throw;
				}
                return RedirectToAction(nameof(Index));
            }

			List<Project> projects = await _context.Projects.Where(c => c.CompanyId == user!.CompanyId).ToListAsync();

			ViewData["ProjectId"] = new SelectList(projects, "Id", "Name");
			ViewData["TicketPriorityId"] = new SelectList(_context.TicketPriorities, "Id", "Id", ticket.TicketPriorityId);
			ViewData["TicketStatusId"] = new SelectList(_context.TicketStatuses, "Id", "Id", ticket.TicketStatusId);
			ViewData["TicketTypeId"] = new SelectList(_context.TicketTypes, "Id", "Id", ticket.TicketTypeId);

			return View(ticket);
		}

		public async Task<IActionResult> AddTicketComment(TicketComment? ticketComment)
		{
			if (ticketComment is not null && ticketComment.TicketId is not 0 && !string.IsNullOrEmpty(ticketComment.Comment))
			{
				Ticket? ticket = await _ticketService.GetTicketByIdAsync(ticketComment.TicketId, User.Identity!.GetCompanyId());
				if (ticket is null) return NotFound();

				string userId = _userManager.GetUserId(User)!;

				if (User.IsInRole(nameof(BTRoles.Admin))
					|| (User.IsInRole(nameof(BTRoles.ProjectManager))  && ticket.Project?.Members.Any(m => m.Id == userId) == true)
					|| ticket.DeveloperUserId == userId
					|| ticket.DeveloperUserId == userId)
				{
					ticketComment.Created = DateTime.UtcNow;
					ticketComment.UserId = userId;

					await _ticketService.AddTicketCommentAsync(ticketComment);

					await _ticketHistoryService.AddHistoryAsync(ticketComment.TicketId, nameof(TicketComment), ticketComment.UserId);
				}

				return RedirectToAction(nameof(Details), new { id = ticket.Id });
			}

			return BadRequest();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddTicketAttachment(int id, [Bind("Id,FormFile,Description,TicketId")] TicketAttachment ticketAttachment, Ticket ticket)
        {
			string statusMessage;

			ModelState.Remove("UserId");

			if (!ModelState.IsValid && ticketAttachment.FormFile != null) 
			{
				ticketAttachment.FileData = await _fileService.ConvertFileToByteArrayAsync(ticketAttachment.FormFile);
				//ticketAttachment.FileName = ticketAttachment.FormFile.FileName;
				ticketAttachment.FileType = ticketAttachment.FormFile.ContentType;

				ticketAttachment.Created = DateTime.UtcNow;
				ticketAttachment.BTUserId = _userManager.GetUserId(User);

				await _ticketService.AddTicketAttachmentAsync(ticketAttachment);
				statusMessage = "Success! A new Attachment added to ticket";
			}
			else
			{
				statusMessage = "Error: Invalid data.";
			}

			return RedirectToAction("Details", new { id = ticketAttachment.TicketId, message = statusMessage });
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
