using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Azure;

namespace Debugger.Models
{
	public class Company
	{
		public int Id { get; set; }

		[Required]
		public string? Name { get; set; }
		public string? Description { get; set; }
		[NotMapped]
		public virtual IFormFile? ImageFile { get; set; }
		public virtual byte[]? ImageData { get; set; }
		public virtual string? ImageType { get; set; }

		// Navigation properties

		public virtual ICollection<Project> Projects { get; set; } = new HashSet<Project>();
		public virtual ICollection<BTUser> Members { get; set; } = new HashSet<BTUser>();
		public virtual ICollection<Invite> Invites { get; set; } = new HashSet<Invite>();
	}
}
