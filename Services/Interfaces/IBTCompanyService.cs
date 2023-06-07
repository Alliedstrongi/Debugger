using Debugger.Models;

namespace Debugger.Services.Interfaces
{
    public interface IBTCompanyService
    {
        Task<Company> GetCompanyInfoAsync(int companyId);

        Task<List<BTUser>> GetCompanyMembersAsync(int? companyId);
    }
}
