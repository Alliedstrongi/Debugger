using Debugger.Models;
using Debugger.Models.Enums;

namespace Debugger.Services.Interfaces
{
    public interface IBTProjectService
    {
        Task AddProjectAsync(Project project);
        Task<List<Project>> GetAllProjectsByCompanyIdAsync(int companyId);
        Task<List<Project>> GetAllProjectsByPriorityAsync(int companyId, string priority);
        Task<List<Project>> GetAllUserProjectsAsync(string userId);
        Task<List<Project>> GetArchivedProjectsByCompanyIdAsync(int companyId);
        Task<Project?> GetProjectByIdAsync(int projectId, int companyId);
        Task<List<ProjectPriority>> GetProjectPrioritiesAsync();
        Task ArchiveProjectAsync(Project project, int companyId);
        Task RestoreProjectAsync(Project project, int companyId);
        Task UpdateProjectAsync(Project project, int companyId);
        Task<BTUser?> GetProjectMangerAsync(int projectId, int companyId);
        Task<bool> AddProjectManagerAsync(string userId, int projectId, int companyId);
        Task RemoveProjectManagerAsync(int projectId, int companyId);
    }
}
