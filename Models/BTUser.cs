using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Debugger.Models
{
	public class BTUser : IdentityUser
	{
		[Required]
		[StringLength(600, ErrorMessage = "The {0} must be at least {2} and max {1} characters long.", MinimumLength = 2)]
		public string? FirstName { get; set; }

		[Required]
		[StringLength(600, ErrorMessage = "The {0} must be at least {2} and max {1} characters long.", MinimumLength = 2)]
		public string? LastName { get; set; }
        [NotMapped]
        public string? FullName { get { return $"{FirstName} {LastName}"; } }

        // Image Properties
        [NotMapped]
		public IFormFile? ImageFormFile { get; set; }
		public byte[]? ImageFileData { get; set; }
		public string? ImageFileType { get; set; }
		public int CompanyId { get; set; }

		public virtual Company? Company { get; set; }

		public virtual ICollection<Project> Projects { get; set; } = new HashSet<Project>();
	}
}
