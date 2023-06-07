using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace Debugger.Models
{
	public class CategoryEmailData
	{
		[Required]
		public string? EmailAddress { get; set; }
		[Required]
		public string? EmailSubject { get; set; }
		[Required]
		public string? EmailBody { get; set; }
		public string? FirstName { get; set; }
		public string? LastName { get; set; }
		public string? CompanyName { get; set; }
	}
}
