using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography.X509Certificates;

namespace Debugger.Models
{
	public class TicketAttachment
	{
		public int Id { get; set; }
		public string? Description { get; set; }

		[Display(Name = "Created Date")]
		[DataType(DataType.DateTime)]
		public DateTime Created { get; set; }

		public int TicketId { get; set; }

		[Required]
		public string? BTUserId { get; set; }

		[NotMapped]
		public virtual IFormFile? FormFile { get; set; }
		public virtual byte[]? FileData { get; set; }
		public virtual string? FileType { get; set; }

		// Navigation Properties

		public virtual Ticket? Ticket { get; set; }
		public virtual BTUser? BTUser { get; set; }
	}
}
