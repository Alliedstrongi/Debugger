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

		[Display(Name = "Created")]
		[DataType(DataType.DateTime)]
		public DateTime Created { get; set; }

		[Display(Name = "Start Date")]
		[DataType(DataType.DateTime)]
		public DateTime StartDate { get; set; }

		[Display(Name = "End Date")]
		[DataType(DataType.DateTime)]
		public DateTime EndDate { get; set; }
		public int ProjectPriorityId { get; set; }

		[NotMapped]
		public IFormFile? ImageFormFile { get; set; }
		public byte[]? ImageFileData { get; set; }
		public string? ImageFileType { get; set; }
		public bool Archived { get; set; }

		//Navigation properties 
		public virtual Company? Company { get; set; }
		public virtual ProjectPriority? ProjectPriority { get; set; }
		public virtual ICollection<BTUser> Members { get; set; } = new HashSet<BTUser>();
		public virtual ICollection<Ticket> Tickets { get; set; } = new HashSet<Ticket>();

        public static implicit operator object?(Project? v)
        {
            throw new NotImplementedException();
        }
    }
}
