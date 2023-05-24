using System.ComponentModel.DataAnnotations;

namespace Debugger.Models
{
	public class TicketComment
	{
		public int Id { get; set; }

		[Required]
		public string? Comment { get; set; }

		[Display(Name = "Created Date")]
		[DataType(DataType.DateTime)]
		public DateTime CreatedDate { get; set; }

		public int TicketId { get; set; }

		[Required]
		public string? BTUserId { get; set; }

		//Navigation Properties

		public virtual Ticket? Ticket { get; set; }
		public virtual BTUser? User { get; set; }
	}
}
