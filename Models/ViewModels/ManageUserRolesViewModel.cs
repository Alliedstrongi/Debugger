using Microsoft.AspNetCore.Mvc.Rendering;

namespace Debugger.Models.ViewModels
{
    public class ManageUserRolesViewModel
    {
        public SelectList? Roles { get; set; }
        public BTUser? User { get; set; }
        public string? SelectedRoles { get; set; }
    }
}
