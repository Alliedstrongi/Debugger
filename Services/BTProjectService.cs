using System.ComponentModel.Design;
using Debugger.Data;
using Debugger.Models;
using Debugger.Models.Enums;
using Debugger.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Debugger.Services
{ 
    public class BTProjectService : IBTProjectService
    {
        private readonly UserManager<BTUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IBTRolesService _rolesService;

        public BTProjectService(ApplicationDbContext context, UserManager<BTUser> userManager, IBTRolesService rolesService)
        {
            _context = context;
            _userManager = userManager;
            _rolesService = rolesService;
        }

		public async Task AddProjectAsync(Project project)
		{
			try
			{
				_context.Projects.Add(project);
				await _context.SaveChangesAsync();
			}
			catch (Exception)
			{
				throw;
			}
		}

		public async Task<bool> AddProjectManagerAsync(string userId, int projectId, int companyId)
        {
            try
            {
                Project? project = await _context.Projects
                                                 .Include(p=>p.Members)
                                                 .FirstOrDefaultAsync(p=>p.Id == projectId && p.CompanyId == companyId);

                BTUser? projectManager = await _context.Users.FirstOrDefaultAsync(u=>u.Id == userId && u.CompanyId == companyId);

                if (projectManager is not null && projectManager is not null) 
                {   //make sure the user is a PM
                    if (!await _rolesService.IsUserInRole(projectManager, nameof(BTRoles.ProjectManager))) return false;

                    // remove any potentially existing pm
                    await RemoveProjectManagerAsync(projectId, companyId);

                    //assign the new pm
                    project.Members.Add(projectManager);
                    
                    //save our changes
                    await _context.SaveChangesAsync();

                    // success!
                    return true;
                }

                return false;
            }
            catch (Exception)
            {

                throw;
            }
        }

		public async Task ArchiveProjectAsync(Project project, int companyId)
		{
			try
			{
				if (project.CompanyId == companyId)
				{
					project.Archived = true;

					// Archive all the tickets
					foreach (Ticket ticket in project.Tickets)
					{
						ticket.ArchivedByProject = !ticket.Archived;
						ticket.Archived = true;
					}

					await _context.SaveChangesAsync();
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		public async Task<List<Project>> GetAllProjectsByCompanyIdAsync(int companyId)
		{
			try
			{
				List<Project> projects = await _context.Projects
					.Where(c => c.CompanyId == companyId)
					.Include(p => p.Company)
					.Include(p => p.ProjectPriority)
					.ToListAsync();

				return projects;
			}
			catch (Exception)
			{
				throw;
			}
		}

		public async Task<List<Project>> GetAllProjectsByPriorityAsync(int companyId, string priority)
		{
			try
			{
				List<Project> projects = await _context.Projects
					.Where(p => p.CompanyId == companyId && p.Archived == false)
					.Include(p => p.Tickets)
					.Include(p => p.ProjectPriority)
					.Include(p => p.Members)
					.Where(p => string.Equals(priority, p.ProjectPriority.Name))
					.ToListAsync();

				return projects;
			}
			catch (Exception)
			{
				throw;
			}
		}

		public async Task<List<Project>> GetAllUserProjectsAsync(string userId)
        {
			try
			{
				List<Project> projects = await _context.Projects
					.Where(p => p.Members.Any(m => m.Id == userId))
					.Include(p => p.Company)
					.Include(p => p.ProjectPriority)
					.ToListAsync();

				return projects;
			}
			catch (Exception)
			{
				throw;
			}
		}

        public async Task<List<Project>> GetArchivedProjectsByCompanyIdAsync(int companyId)
        {
			try
			{
				List<Project> projects = await _context.Projects
					.Where(p => p.CompanyId == companyId && p.Archived)
					.Include(p => p.Company)
					.Include(p => p.ProjectPriority)
					.Include(p => p.Members)
					.Include(p => p.Tickets)
					.ThenInclude(t => t.DeveloperUser)
					.Include(p => p.Tickets)
					.ThenInclude(t => t.SubmitterUser)
					.ToListAsync();

				return projects;
			}
			catch (Exception)
			{
				throw;
			}
		}

        public async Task<Project?> GetProjectByIdAsync(int projectId, int companyId)
        {
			try
			{
				Project? project = await _context.Projects
					.Include(p => p.Company)
					.Include(p => p.ProjectPriority)
					.Include(p => p.Members)
					.Include(p => p.Tickets)
					.ThenInclude(p => p.DeveloperUser)
					.Include(p => p.Tickets)
					.ThenInclude(p => p.SubmitterUser)
					.FirstOrDefaultAsync(p => p.Id == projectId && p.CompanyId == companyId);

				return project;
			}
			catch (Exception)
			{
				throw;
			}
		}

        public async Task<BTUser?> GetProjectMangerAsync(int projectId, int companyId)
        {
			try
			{
				Project? project = await _context.Projects
					.AsNoTracking()
					.Include(p => p.Members)
					.FirstOrDefaultAsync(p => p.Id == projectId && p.CompanyId == companyId);

				if (project != null)
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

        public async Task<List<ProjectPriority>> GetProjectPrioritiesAsync()
        {
			try
			{
				List<ProjectPriority> priorities = await _context.ProjectPriorities.ToListAsync();
				return priorities;
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
                                                 .Include(p=>p.Members)
                                                 .FirstOrDefaultAsync(p=>p.Id == projectId && p.CompanyId == companyId);
                if (project is not null)
                {
                    foreach(BTUser member in project.Members)
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

        public async Task RestoreProjectAsync(Project project, int companyId)
        {
			try
			{
				if (project.CompanyId == companyId)
				{
					project.Archived = false;

					foreach (Ticket ticket in project.Tickets)
					{
						if (ticket.ArchivedByProject == true)
							ticket.Archived = false;

						ticket.ArchivedByProject = false;
					}

					await _context.SaveChangesAsync();
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

        public async Task UpdateProjectAsync(Project project, int companyId)
        {
            try
            {
                var existingProject = await _context.Projects
                    .FirstOrDefaultAsync(p => p.Id == project.Id && p.CompanyId == companyId);

                if (existingProject != null)
                {
                    existingProject.Name = project.Name;
                    existingProject.Description = project.Description;
                    existingProject.StartDate = project.StartDate;
                    existingProject.EndDate = project.EndDate;
                    existingProject.ProjectPriorityId = project.ProjectPriorityId;

                    _context.Update(existingProject);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    throw new InvalidOperationException("Project not found.");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
