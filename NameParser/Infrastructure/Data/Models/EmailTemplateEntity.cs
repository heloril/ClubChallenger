using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NameParser.Infrastructure.Data.Models
{
	/// <summary>
	/// A saved, named email template (subject + HTML body) used to reuse a mailing
	/// layout. Templates are stored so they can be reloaded into the editor and used
	/// as the basis for the per-member preview/sending screen.
	/// </summary>
	[Table("EmailTemplates")]
	public class EmailTemplateEntity
	{
		[Key]
		public int Id { get; set; }

		[Required]
		[MaxLength(200)]
		public string Name { get; set; }

		[MaxLength(500)]
		public string Subject { get; set; }

		public string Body { get; set; }

		public DateTime CreatedDate { get; set; }

		public DateTime UpdatedDate { get; set; }
	}
}
