using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NameParser.Infrastructure.Data.Models;

namespace NameParser.Infrastructure.Data
{
	/// <summary>
	/// Persists and queries saved, named email templates
	/// (see <see cref="EmailTemplateEntity"/>). Saving a template with an existing
	/// name updates it; otherwise a new template is created.
	/// </summary>
	public class EmailTemplateRepository
	{
		/// <summary>
		/// Creates or updates a template identified by its (case-insensitive) name.
		/// Returns the persisted entity.
		/// </summary>
		public EmailTemplateEntity Save(string name, string subject, string body)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Template name is required.", nameof(name));

			using var context = new RaceManagementContext();

			var trimmed = name.Trim();
			var existing = context.EmailTemplates
				.FirstOrDefault(t => t.Name == trimmed);

			var now = DateTime.Now;

			if (existing != null)
			{
				existing.Subject = subject;
				existing.Body = body;
				existing.UpdatedDate = now;
			}
			else
			{
				existing = new EmailTemplateEntity
				{
					Name = trimmed,
					Subject = subject,
					Body = body,
					CreatedDate = now,
					UpdatedDate = now
				};
				context.EmailTemplates.Add(existing);
			}

			context.SaveChanges();
			return existing;
		}

		/// <summary>
		/// Returns all saved templates, most recently updated first.
		/// </summary>
		public List<EmailTemplateEntity> GetAll()
		{
			using var context = new RaceManagementContext();

			return context.EmailTemplates
				.AsNoTracking()
				.OrderByDescending(t => t.UpdatedDate)
				.ThenBy(t => t.Name)
				.ToList();
		}

		/// <summary>
		/// Deletes the template with the given id. No-op when it does not exist.
		/// </summary>
		public void Delete(int id)
		{
			using var context = new RaceManagementContext();

			var existing = context.EmailTemplates.FirstOrDefault(t => t.Id == id);
			if (existing != null)
			{
				context.EmailTemplates.Remove(existing);
				context.SaveChanges();
			}
		}
	}
}
