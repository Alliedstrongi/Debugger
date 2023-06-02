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
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> AddProjectManagerAsync(string userId, int projectId, int companyId)
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

        public async Task ArchiveProjectAsync(Project project, int companyId)
        {
            try

            {
                if (project.CompanyId == companyId)
                {
                    project.Archived = true;

                    //archive all the tickets
                    foreach (Ticket ticket in project.Tickets)
                    {
                        //archive by project if the ticket is not already archived
                        ticket.ArchivedByProject = !ticket.Archived;

                        ticket.Archived = true;
                    }

                    _context.Update(project);
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
            return await _context.Projects
                .Where(p => p.CompanyId == companyId)
                .ToListAsync();
        }


        public async Task<List<Project>> GetAllProjectsByPriorityAsync(int companyId, string priority)
        {
            return await _context.Projects
                .Where(p => p.CompanyId == companyId && p.ProjectPriority!.Name == priority)
                .ToListAsync();
        }

        public async Task<List<Project>> GetAllUserProjectsAsync(string userId)
        {
            return await _context.Projects
                .Where(p => p.Members.Any(m => m.Id == userId))
                .ToListAsync();
        }

        public async Task<List<Project>> GetArchivedProjectsByCompanyIdAsync(int companyId)
        {
            return await _context.Projects
                .Where(p => p.CompanyId == companyId && p.Archived == true)
                .ToListAsync();
        }


        public async Task<Project?> GetProjectByIdAsync(int projectId, int companyId)
        {
            try
            {
                return await _context.Projects.Include(p => p.Company)
                                              .Include(p => p.ProjectPriority)
                                              .Include(p => p.Members)
                                              .Include(p => p.Tickets)
                                              .ThenInclude(t => t.DeveloperUser)
                                              .Include(p => p.Tickets)
                                              .ThenInclude(t => t.SubmitterUser)
                                              .FirstOrDefaultAsync(p => p.Id == projectId && p.CompanyId == companyId);

            }
            catch (Exception)
            {

                throw;
            }
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

        public async Task<List<ProjectPriority>> GetProjectPrioritiesAsync()
        {
            List<ProjectPriority> priorities = await _context.ProjectPriorities.ToListAsync();
            return priorities;
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

        public async Task RestoreProjectAsync(Project project, int companyId)
        {
            try
            {
                if (project.CompanyId == companyId)
                {
                    project.Archived = false;
                    foreach (Ticket ticket in project.Tickets)
                    {
                        ticket.Archived = !ticket.ArchivedByProject;

                        ticket.ArchivedByProject = false;
                    }
                    _context.Update(project);
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
                if (project.CompanyId == companyId)
                {
                    _context.Update(project);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    throw new InvalidOperationException("Project not found");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<Project>> GetUnassignedProjectsByCompanyIdAsync(int companyId)
        {
            try
            {
                List<Project> allProjects = await GetAllProjectsByCompanyIdAsync(companyId);
                List<Project> unassignedProjects = new();

                foreach (Project project in allProjects)
                {
                    BTUser? projectManager = await GetProjectManagerAsync(project.Id, companyId);

                    if (projectManager is null) unassignedProjects.Add(project);
                }
                return unassignedProjects;
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}