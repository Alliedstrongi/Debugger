﻿using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Debugger.Models
{
	public class Invite
	{
		public int Id { get; set; }

		[Display(Name = "Invite Date")]
		[DataType(DataType.DateTime)]
		public DateTime InviteDate { get; set; }

		[Display(Name = "Join Date")]
		[DataType(DataType.DateTime)]
		public DateTime JoinDate { get; set; }
		public Guid CompanyToken { get; set; }
		public int CompanyId { get; set; }
		public int? ProjectId { get; set; }

		[Required]
		public string? InvitorId { get; set; }
		public string? InviteeId { get; set; }

		[Required]
		public string? InviteeEmail { get; set; }

		[Required]
		public string? InviteeFirstName { get; set; }

		[Required]
		public string? InviteeLastName { get; set; }
		public string? Message { get; set; }
		public bool IsValid { get; set; }

		// Navigation properties 
			public virtual Company? Company { get; set; }
			public virtual Project? Project { get; set; }
			public virtual BTUser? Invitor { get; set; }
			public virtual BTUser? Invitee { get; set; }
	}
}
