using System.ComponentModel.DataAnnotations.Schema;

namespace Debugger.Models
{
	public class BTUser
	{
		public string? FirstName { get; set; }
		public string? LastName { get; set; }
		public string? FullName { get; set; }

		// Image Properties
		[NotMapped]
		public virtual IFormFile? ImageFile { get; set; }
		public byte[]? ImageData { get; set; }
		public string? ImageType { get; set; }
		public int CompanyId { get; set; }

		public virtual Company? Company { get; set; }

		public virtual ICollection<Project> Projects { get; set; } = new HashSet<Project>();
	}
}
