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
using Debugger.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Debugger.Services.Interfaces;
using Debugger.Extensions;
using Debugger.Models.ViewModels;
using Debugger.Services;

namespace Debugger.Controllers
{
	[Authorize]
	public class ProjectsController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly UserManager<BTUser> _userManager;
		private readonly IBTFileService _fileService;
		private readonly IBTProjectService _projectService;
		private readonly IBTRolesService _rolesService;


		public ProjectsController(ApplicationDbContext context, UserManager<BTUser> userManager, IBTFileService fileService, IBTProjectService projectService, IBTRolesService rolesService)
		{
			_context = context;
			_userManager = userManager;
			_fileService = fileService;
			_projectService = projectService;
			_rolesService = rolesService;
		}

		// GET: Projects
		public async Task<IActionResult> Index()
		{
			List<Project> projects = await _projectService.GetAllProjectsByCompanyIdAsync(User.Identity!.GetCompanyId());
			return View(projects);
		}

		[HttpGet]
		[Authorize(Roles = nameof(BTRoles.Admin))]
		public async Task<IActionResult> AssignPM(int? id)
		{
			if (id is null or 0)
			{
				return NotFound();
			}

			int companyId = User.Identity!.GetCompanyId();
			Project? project = await _projectService.GetProjectByIdAsync(id.Value, companyId);

			if (project is null)
			{
				return NotFound();
			}


			List<BTUser> projectManagers = await _rolesService.GetUsersInRoleAsync(nameof(BTRoles.ProjectManager), companyId);
			BTUser? currentPM = await _projectService.GetProjectMangerAsync(id.Value, companyId);

			AssignPMViewModel viewModel = new AssignPMViewModel()
			{
				Project = project,
				PMId = currentPM?.Id,
				PMList = new SelectList(projectManagers, "Id", "FullName", currentPM?.Id)
			};

			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = nameof(BTRoles.Admin))]
		public async Task<IActionResult> AssignPM(AssignPMViewModel viewModel)
		{
			if (viewModel.Project?.Id != null)
			{
				if (string.IsNullOrEmpty(viewModel.PMId))
				{
					await _projectService.RemoveProjectManagerAsync(viewModel.Project.Id, User.Identity!.GetCompanyId());
				}
				else
				{
					await _projectService.AddProjectManagerAsync(viewModel.PMId, viewModel.Project.Id, User.Identity!.GetCompanyId());
				}

				return RedirectToAction(nameof(Details), new { id = viewModel.Project.Id });
			}

			return BadRequest();
		}

		// GET: Projects/Details/5
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			BTUser? user = await _userManager.GetUserAsync(User);

			var project = await _context.Projects
				.Where(c => c.CompanyId == user!.CompanyId)
				.Include(p => p.Company)
				.Include(p => p.ProjectPriority)
				.FirstOrDefaultAsync(m => m.Id == id);

			if (project == null)
			{
				return NotFound();
			}

			return View(project);
		}

		[Authorize(Roles = $"{nameof(BTRoles.Admin)}, {nameof(BTRoles.ProjectManager)}")]
		public IActionResult Create()
		{
			ViewData["ProjectPriorityId"] = new SelectList(_context.ProjectPriorities, "Id", "Name");
			return View();
		}

		// POST: Projects/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = $"{nameof(BTRoles.Admin)}, {nameof(BTRoles.ProjectManager)}")]
		public async Task<IActionResult> Create([Bind("Name,Description,StartDate,EndDate,ProjectPriorityId,ImageFormFile")] Project project)
		{
			ModelState.Remove("project.CompanyId");

			if (ModelState.IsValid)
			{
				project.Created = DateTime.UtcNow;
				project.StartDate = DateTime.SpecifyKind(project.StartDate, DateTimeKind.Utc);
				project.EndDate = DateTime.SpecifyKind(project.EndDate, DateTimeKind.Utc);

				BTUser? user = await _userManager.GetUserAsync(User);
				project.CompanyId = user!.CompanyId;

				if (project.ImageFormFile is not null)
				{
					project.ImageFileData = await _fileService.ConvertFileToByteArrayAsync(project.ImageFormFile);
					project.ImageFileType = project.ImageFormFile.ContentType;
				}

				if (User.IsInRole(nameof(BTRoles.ProjectManager)))
				{
					project.Members.Add(user);
				}

				await _projectService.AddProjectAsync(project);
				return RedirectToAction(nameof(Index));
			}

			ViewData["ProjectPriorityId"] = new SelectList(_context.ProjectPriorities, "Id", "Name", project.ProjectPriorityId);
			return View(project);
		}

		// GET: Projects/Edit/5
		[Authorize(Roles = $"{nameof(BTRoles.Admin)}, {nameof(BTRoles.ProjectManager)}")]
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			BTUser? user = await _userManager.GetUserAsync(User);

			var project = await _context.Projects
										.Where(c => c.CompanyId == user!.CompanyId)
										.FirstOrDefaultAsync(p => p.Id == id);

			if (project == null)
			{
				return NotFound();
			}
			ViewData["ProjectPriorityId"] = new SelectList(_context.ProjectPriorities, "Id", "Name", project.ProjectPriorityId);
			return View(project);
		}

		// POST: Projects/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = $"{nameof(BTRoles.Admin)}, {nameof(BTRoles.ProjectManager)}")]
		public async Task<IActionResult> Edit(int id, [Bind("Id,CompanyId,Name,Created,Description,StartDate,EndDate,ProjectPriorityId,ImageFormFile,Archived")] Project project)
		{
			if (id != project.Id)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					project.Created = DateTime.SpecifyKind(project.Created, DateTimeKind.Utc);
					project.StartDate = DateTime.SpecifyKind(project.StartDate, DateTimeKind.Utc);
					project.EndDate = DateTime.SpecifyKind(project.EndDate, DateTimeKind.Utc);

					var existingProject = await _context.Projects.FindAsync(project.Id);

					if (existingProject != null)
					{
						if (project.ImageFormFile != null)
						{
							project.ImageFileData = await _fileService.ConvertFileToByteArrayAsync(project.ImageFormFile);
							project.ImageFileType = project.ImageFormFile.ContentType;
						}
						else
						{
							project.ImageFileData = existingProject.ImageFileData;
							project.ImageFileType = existingProject.ImageFileType;
						}

						_context.Entry(existingProject).CurrentValues.SetValues(project);
						await _context.SaveChangesAsync();
					}
					else
					{
						return NotFound();
					}
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!ProjectExists(project.Id))
					{
						return NotFound();
					}
					else
					{
						throw;
					}
				}
				return RedirectToAction(nameof(Details), new { id = project.Id });
			}

			ViewData["ProjectPriorityId"] = new SelectList(_context.ProjectPriorities, "Id", "Name", project.ProjectPriorityId);
			return View(project);
		}

		// GET: Projects/Archive/5
		[Authorize(Roles = $"{nameof(BTRoles.Admin)}, {nameof(BTRoles.ProjectManager)}")]
		public async Task<IActionResult> Archive(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			BTUser? user = await _userManager.GetUserAsync(User);

			var project = await _context.Projects
				.Where(c => c.CompanyId == user!.CompanyId)
				.Include(p => p.Company)
				.Include(p => p.ProjectPriority)
				.FirstOrDefaultAsync(m => m.Id == id);

			if (project == null)
			{
				return NotFound();
			}

			return View(project);
		}

		// POST: Projects/Archive/5
		[HttpPost, ActionName("Archive")]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = $"{nameof(BTRoles.Admin)}, {nameof(BTRoles.ProjectManager)}")]
		public async Task<IActionResult> ArchiveConfirmed(int id)
		{
			var project = await _context.Projects.FindAsync(id);

			if (project != null)
			{
				project.Archived = true;
			}

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		private bool ProjectExists(int id)
		{
			return _context.Projects.Any(e => e.Id == id);
		}
	}
}
