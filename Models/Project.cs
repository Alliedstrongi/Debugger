using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Debugger.Models
{
	public class Project
	{
		public int Id { get; set; }
		public int CompanyId { get; set; }

		[Required]
		public string? Name { get; set; }

		[Required]
		public string? Description { get; set; }

		[Display(Name = "Created Date")]
		[DataType(DataType.DateTime)]
		public DateTime CreatedDate { get; set; }

		[Display(Name = "Start Date")]
		[DataType(DataType.DateTime)]
		public DateTime StartDate { get; set; }

		[Display(Name = "End Date")]
		[DataType(DataType.DateTime)]
		public DateTime EndDate { get; set; }
		public int ProjectPriorityId { get; set; }

		[NotMapped]
		public virtual IFormFile? ImageFile { get; set; }
		public virtual byte[]? ImageData { get; set; }
		public virtual string? ImageType { get; set; }
		public bool Archived { get; set; }

		//Navigation properties 
		public virtual Company? Company { get; set; }
		public virtual ProjectPriority? ProjectPriority { get; set; }
		public virtual ICollection<BTUser> Members { get; set; } = new HashSet<BTUser>();
		public virtual ICollection<Ticket> Tickets { get; set; } = new HashSet<Ticket>();
	}
}
