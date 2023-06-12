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
using Debugger.Services;
using Debugger.Models.ViewModels;

namespace Debugger.Controllers
{
    [Authorize]
    public class TicketsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BTUser> _userManager;
        private readonly IBTTicketService _ticketService;
        private readonly IBTProjectService _projectService;
        private readonly IBTRolesService _rolesService;
        private readonly IBTTicketHistoryService _ticketHistoryService;
        private readonly IBTFileService _fileService;

        public TicketsController(ApplicationDbContext context, UserManager<BTUser> userManager, IBTTicketService ticketService, IBTProjectService projectService, IBTRolesService rolesService, IBTTicketHistoryService ticketHistoryService, IBTFileService fileService)
        {
            _context = context;
            _userManager = userManager;
            _ticketService = ticketService;
            _projectService = projectService;
            _rolesService = rolesService;
            _ticketHistoryService = ticketHistoryService;
            _fileService = fileService;
        }

        // GET: Tickets
        public async Task<IActionResult> Index()
        {
            BTUser? user = await _userManager.GetUserAsync(User);
            int companyId = user!.CompanyId;

            var tickets = await _ticketService.GetTicketsByCompanyIdAsync(companyId);
            return View(tickets);
        }

        // GET: Tickets/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            BTUser? user = await _userManager.GetUserAsync(User);
            int companyId = user!.CompanyId;

            var ticket = await _ticketService.GetTicketByIdAsync(id.Value, companyId);

            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }


        // GET: Tickets/Create
        public async Task<IActionResult> Create()
        {
            BTUser? user = await _userManager.GetUserAsync(User);
            int companyId = user!.CompanyId;

            List<Project> projects = await _projectService.GetAllProjectsByCompanyIdAsync(companyId);

            ViewData["ProjectId"] = new SelectList(projects, "Id", "Name");
            ViewData["TicketPriorityId"] = new SelectList(await _ticketService.GetTicketPriorities(), "Id", "Name");
            ViewData["TicketTypeId"] = new SelectList(await _ticketService.GetTicketTypes(), "Id", "Name");
            return View();
        }

        // POST: Tickets/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,TicketTypeId,TicketPriorityId,ProjectId")] Ticket ticket)
        {
            ModelState.Remove(nameof(Ticket.SubmitterUserId));

            if (ModelState.IsValid)
            {
                int companyId = User.Identity!.GetCompanyId();

                ticket.Created = DateTime.UtcNow;
                ticket.SubmitterUserId = _userManager.GetUserId(User);

                List<TicketStatus> ticketStatuses = await _ticketService.GetTicketStatuses();

                TicketStatus? ticketStatus = ticketStatuses.FirstOrDefault(ts => ts.Name == BTTicketStatuses.New.ToString());

                ticket.TicketStatusId = ticketStatus!.Id;

                await _ticketService.AddTicketAsync(ticket);

                await _ticketHistoryService.AddHistoryAsync(null, ticket, _userManager.GetUserId(User)!);

                return RedirectToAction(nameof(Index));
            }

            BTUser? user = await _userManager.GetUserAsync(User);


            List<Project> projects = await _projectService.GetAllProjectsByCompanyIdAsync(User.Identity!.GetCompanyId());

            ViewData["ProjectId"] = new SelectList(projects, "Id", "Name");
            ViewData["TicketPriorityId"] = new SelectList(await _ticketService.GetTicketPriorities(), "Id", "Name");
            ViewData["TicketTypeId"] = new SelectList(await _ticketService.GetTicketTypes(), "Id", "Name");
            return View(ticket);
        }

        // GET: Tickets/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            BTUser? user = await _userManager.GetUserAsync(User);
            int companyId = user!.CompanyId;

            var ticket = await _ticketService.GetTicketByIdAsync(id.Value, companyId);

            if (ticket == null)
            {
                return NotFound();
            }

            List<Project> projects = await _projectService.GetAllProjectsByCompanyIdAsync(companyId);

            ViewData["ProjectId"] = new SelectList(projects, "Id", "Name");
            ViewData["TicketPriorityId"] = new SelectList(await _ticketService.GetTicketPriorities(), "Id", "Id", ticket.TicketPriorityId);
            ViewData["TicketStatusId"] = new SelectList(await _ticketService.GetTicketStatuses(), "Id", "Id", ticket.TicketStatusId);
            ViewData["TicketTypeId"] = new SelectList(await _ticketService.GetTicketTypes(), "Id", "Id", ticket.TicketTypeId);
            return View(ticket);
        }

        // POST: Tickets/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Created,Updated,Archived,ArchivedByProject,ProjectId,TicketTypeId,TicketStatusId,TicketPriorityId,DeveloperUserId,SubmitterUserId")] Ticket ticket)
        {
            if (id != ticket.Id)
            {
                return NotFound();
            }

            BTUser? user = await _userManager.GetUserAsync(User);
            int companyId = user!.CompanyId;

            if (ModelState.IsValid)
            {
                try
                {
                    ticket.Created = DateTime.SpecifyKind(ticket.Created, DateTimeKind.Utc);
                    ticket.Updated = DateTime.UtcNow;

                    Ticket? oldTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id, User.Identity!.GetCompanyId());



                    await _ticketService.UpdateTicketAsync(ticket, companyId);
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw;

                }
                return RedirectToAction(nameof(Index));
            }


            List<Project> projects = await _projectService.GetAllProjectsByCompanyIdAsync(companyId);

            ViewData["ProjectId"] = new SelectList(projects, "Id", "Name");
            ViewData["TicketPriorityId"] = new SelectList(await _ticketService.GetTicketPriorities(), "Id", "Id", ticket.TicketPriorityId);
            ViewData["TicketStatusId"] = new SelectList(await _ticketService.GetTicketStatuses(), "Id", "Id", ticket.TicketStatusId);
            ViewData["TicketTypeId"] = new SelectList(await _ticketService.GetTicketTypes(), "Id", "Id", ticket.TicketTypeId);
            return View(ticket);
        }

        public async Task<IActionResult> UnassignedTickets()
        {
            int companyId = User.Identity!.GetCompanyId();
            List<Ticket> tickets = await _ticketService.GetUnassignedTicketsByCompanyIdAsync(companyId);

            ViewData["Title"] = "Unassigned Tickets";
            return View(nameof(Index), tickets);
        }

        [HttpGet]
        [Authorize(Roles = $"{nameof(BTRoles.Admin)}, {nameof(BTRoles.ProjectManager)}")]
        public async Task<IActionResult> AssignDev(int? id)
        {
            if (id is null or 0)
            {
                return NotFound();
            }

            int companyId = User.Identity!.GetCompanyId();

            Ticket? ticket = await _ticketService.GetTicketByIdAsync(id.Value, companyId);

            if (ticket is null)
            {
                return NotFound();
            }

            List<BTUser> ticketDevelopers = await _rolesService.GetUsersInRoleAsync(nameof(BTRoles.Developer), companyId);
            BTUser? currentDev = await _ticketService.GetTicketDeveloperAsync(id.Value, companyId);

            AssignDevViewModel viewModel = new AssignDevViewModel()
            {
                Ticket = ticket,
                DevId = currentDev?.Id,
                DevList = new SelectList(ticketDevelopers, "Id", "FullName", currentDev?.Id)
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = $"{nameof(BTRoles.Admin)}, {nameof(BTRoles.ProjectManager)}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDev(AssignDevViewModel viewModel)
        {
            if (viewModel.Ticket?.Id != null)
            {
                if (string.IsNullOrEmpty(viewModel.DevId))
                {
                    await _ticketService.RemoveTicketDeveloperAsync(viewModel.Ticket.Id, User.Identity!.GetCompanyId());
                }
                else
                {
                    await _ticketService.AddTicketDeveloperAsync(viewModel.DevId, viewModel.Ticket.Id, User.Identity!.GetCompanyId());
                }

                return RedirectToAction(nameof(Details), new { id = viewModel.Ticket!.Id });
            }

            return BadRequest();
        }

        // GET: Tickets/Delete/5
        public async Task<IActionResult> Archive(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            BTUser? user = await _userManager.GetUserAsync(User);
            int companyId = user!.CompanyId;

            var ticket = await _ticketService.GetTicketByIdAsync(id.Value, companyId);
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
            BTUser user = await _userManager.GetUserAsync(User);
            int companyId = user.CompanyId;

            var ticket = await _ticketService.GetTicketByIdAsync(id, companyId);
            if (ticket != null)
            {
                ticket.Archived = true;
            }

            await _ticketService.UpdateTicketAsync(ticket, companyId);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> MyTickets()
        {
            List<Ticket> tickets = await _ticketService.GetTicketsByUserIdAsync(_userManager.GetUserId(User)!);

            ViewData["Title"] = "My Tickets";
            return View(nameof(Index), tickets);
        }

        public async Task<IActionResult> AllTickets()
        {
            int companyId = User.Identity!.GetCompanyId();
            List<Ticket> tickets = await _ticketService.GetTicketsByCompanyIdAsync(companyId);

            ViewData["Title"] = "All Tickets";
            return View(nameof(Index), tickets);
        }

        public async Task<IActionResult> ArchivedTickets()
        {
            int companyId = User.Identity!.GetCompanyId();
            List<Ticket> tickets = await _ticketService.GetArchivedTicketsAsync(companyId);

            ViewData["Title"] = "Archived Tickets";
            return View(nameof(Index), tickets);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTicketAttachment([Bind("Id,FormFile,Description,TicketId")] TicketAttachment ticketAttachment)
        {
            string statusMessage;

            ModelState.Remove("UserId");

            if (!ModelState.IsValid && ticketAttachment.FormFile != null)
            {
                ticketAttachment.FileData = await _fileService.ConvertFileToByteArrayAsync(ticketAttachment.FormFile);
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

    }
}