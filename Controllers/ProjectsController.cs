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


        public ProjectsController(ApplicationDbContext context, UserManager<BTUser> userManager, IBTFileService fileService, IBTRolesService rolesService, IBTProjectService projectService)
        {
            _context = context;
            _userManager = userManager;
            _fileService = fileService;
            _rolesService = rolesService;
            _projectService = projectService;
        }

        // GET: Projects
        public IActionResult Index()
        {
            if (User.IsInRole(nameof(BTRoles.Admin))) return RedirectToAction(nameof(AllProjects));
            else return RedirectToAction(nameof(MyProjects));
        }

        public async Task<IActionResult> MyProjects()
        {
            List<Project> projects = await _projectService.GetAllUserProjectsAsync(_userManager.GetUserId(User)!);

            ViewData["Title"] = "My Projects";
            return View(nameof(MyProjects), projects);
        }

        public async Task<IActionResult> AllProjects()
        {
            List<Project> projects = await _projectService.GetAllProjectsByCompanyIdAsync(User.Identity!.GetCompanyId());

            ViewData["Title"] = "All Projects";
            return View(nameof(Index), projects);
        }

        public async Task<IActionResult> ArchivedProjects()
        {
            List<Project> projects = await _projectService.GetArchivedProjectsByCompanyIdAsync(User.Identity!.GetCompanyId());

            ViewData["Title"] = "Archived Projects";
            return View(nameof(ArchivedProjects), projects);
        }

        public async Task<IActionResult> UnassignedProjects()
        {
            List<Project> projects = await _projectService.GetUnassignedProjectsByCompanyIdAsync(User.Identity!.GetCompanyId());

            ViewData["Title"] = "Unassigned Projects";
            return View(nameof(Index), projects);
        }

        [HttpGet]
        [Authorize(Roles = nameof(BTRoles.Admin))]
        public async Task<IActionResult> AssignPm(int? id)
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
            BTUser? currentPm = await _projectService.GetProjectManagerAsync(id.Value, companyId);

            AssignPMViewModel viewModel = new AssignPMViewModel()
            {
                Project = project,
                PMId = currentPm?.Id,
                PMList = new SelectList(projectManagers, "Id", "FullName", currentPm?.Id)
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = nameof(BTRoles.Admin))]
        [ValidateAntiForgeryToken]
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

                return RedirectToAction(nameof(Details), new { id = viewModel.Project!.Id });
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

            int companyId = user!.CompanyId;

            var project = await _projectService.GetProjectByIdAsync(id.Value, companyId);


            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }


        [Authorize(Roles = $"{nameof(BTRoles.Admin)}, {nameof(BTRoles.ProjectManager)}")]
        public async Task<IActionResult> Create()
        {
            ViewData["ProjectPriorityId"] = new SelectList(await _projectService.GetProjectPrioritiesAsync(), "Id", "Name");
            return View();
        }

        // POST: Projects/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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
            ViewData["ProjectPriorityId"] = new SelectList(await _projectService.GetProjectPrioritiesAsync(), "Id", "Name", project.ProjectPriorityId);
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
            int companyId = user!.CompanyId;

            var project = await _projectService.GetProjectByIdAsync(id.Value, companyId);


            if (project == null)
            {
                return NotFound();
            }
            ViewData["ProjectPriorityId"] = new SelectList(await _projectService.GetProjectPrioritiesAsync(), "Id", "Name", project.ProjectPriorityId);
            return View(project);
        }

        // POST: Projects/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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



                    if (project.ImageFormFile != null)
                    {
                        project.ImageFileData = await _fileService.ConvertFileToByteArrayAsync(project.ImageFormFile);
                        project.ImageFileType = project.ImageFormFile.ContentType;
                    }
                    int companyId = User.Identity!.GetCompanyId();
                    await _projectService.UpdateProjectAsync(project, companyId);

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

            ViewData["ProjectPriorityId"] = new SelectList(await _projectService.GetProjectPrioritiesAsync(), "Id", "Name", project.ProjectPriorityId);
            return View(project);
        }


        // GET: Projects/Archive/5
        [Authorize(Roles = $"{nameof(BTRoles.Admin)}, {nameof(BTRoles.ProjectManager)}")]
        public async Task<IActionResult> Archive(int? id)
        {
            try
            {
                if (id == null)
                {
                    return NotFound();
                }

                int companyId = User.Identity!.GetCompanyId();
                var project = await _projectService.GetProjectByIdAsync(id.Value, companyId);

                if (project == null)
                {
                    return NotFound();
                }

                return View(project);
            }
            catch (Exception)
            {

                throw;
            }
        }

        // POST: Projects/Archive/5
        [HttpPost, ActionName("Archive")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{nameof(BTRoles.Admin)}, {nameof(BTRoles.ProjectManager)}")]
        public async Task<IActionResult> ArchiveConfirmed(int id)
        {
            try
            {
                int companyId = User.Identity!.GetCompanyId();
                var project = await _projectService.GetProjectByIdAsync(id, companyId);

                if (project != null)
                {
                    project.Archived = true;
                }

                await _projectService.ArchiveProjectAsync(project!, companyId);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {

                throw;
            }
        }

        private bool ProjectExists(int id)
        {
            return (_context.Projects?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}