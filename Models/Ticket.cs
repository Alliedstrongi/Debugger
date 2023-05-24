using System.ComponentModel.DataAnnotations;

namespace Debugger.Models
{
	public class Ticket
	{
		public int Id { get; set; }

		[Required]
		public string? Title { get; set; }

		[Required]
		public string? Description { get; set; }

		[Display(Name = "Created Date")]
		[DataType(DataType.DateTime)]
		public DateTime CreatedDate { get; set; }

		[Display(Name = "Updated Date")]
		[DataType(DataType.DateTime)]
		public DateTime UpdatedDate { get; set; }

		public bool Archived { get; set; }

		public bool ArchivedByProject { get; set; }
		public int ProjectId { get; set; }
		public int TicketTypeId { get; set; }
		public int TicketStatusId { get; set;}
		public int TicketPriorityId { get; set;}
		public string? DeveloperUserId { get; set; }

		[Required]
		public string? SubmitterUserId { get; set; }

		//Navigation Properties

		public virtual Project? Project { get; set; }
		public virtual TicketPriority? TicketPriority { get; set; }
		public virtual TicketType? TicketType { get; set; }
		public virtual TicketStatus? TicketStatus { get; set; }
		public virtual BTUser? DeveloperUsers { get; set; }
		public virtual BTUser? SubmitterUser { get; set; }
		public virtual ICollection<TicketComment> Comments { get; set; } = new HashSet<TicketComment>();
		public virtual ICollection<TicketAttachment> Attachments { get; set; } = new HashSet<TicketAttachment>();
		public virtual TicketHistory? TicketHistory { get; set; }
	}
}
