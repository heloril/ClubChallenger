using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NameParser.Infrastructure.Data.Models;

namespace NameParser.Infrastructure.Data
{
	/// <summary>
	/// Persists and queries the per-member "recent-races VMA" estimate
	/// (<see cref="RecentRaceVmaLogEntity"/> and its <see cref="RecentRaceVmaLineEntity"/>
	/// race lines). A single, latest log is kept per member: saving a new one replaces
	/// the previous log for that member.
	/// </summary>
	public class RecentRaceVmaRepository
	{
		/// <summary>
		/// Replaces the existing recent-races VMA log for the member (matched by first/last
		/// name, case-insensitive) with the provided one.
		/// </summary>
		public void Save(RecentRaceVmaLogEntity log)
		{
			if (log == null)
				throw new ArgumentNullException(nameof(log));

			using var context = new RaceManagementContext();

			if (log.CreatedDate == default)
				log.CreatedDate = DateTime.Now;

			var first = log.MemberFirstName ?? string.Empty;
			var last = log.MemberLastName ?? string.Empty;

			var existing = context.RecentRaceVmaLogs
				.Include(l => l.Lines)
				.Where(l => l.MemberFirstName == first && l.MemberLastName == last)
				.ToList();

			if (existing.Count > 0)
				context.RecentRaceVmaLogs.RemoveRange(existing);

			context.RecentRaceVmaLogs.Add(log);
			context.SaveChanges();
		}

		/// <summary>
		/// Returns the latest recent-races VMA log (with its race lines) for the member,
		/// or null when none exists.
		/// </summary>
		public RecentRaceVmaLogEntity GetForMember(string firstName, string lastName)
		{
			using var context = new RaceManagementContext();

			firstName ??= string.Empty;
			lastName ??= string.Empty;

			return context.RecentRaceVmaLogs
				.Include(l => l.Lines)
				.AsNoTracking()
				.Where(l => l.MemberFirstName == firstName && l.MemberLastName == lastName)
				.OrderByDescending(l => l.CreatedDate)
				.ThenByDescending(l => l.Id)
				.FirstOrDefault();
		}

		/// <summary>
		/// Returns the latest recent-races VMA log per member (header only, no lines).
		/// </summary>
		public List<RecentRaceVmaLogEntity> GetAllLatest()
		{
			using var context = new RaceManagementContext();

			return context.RecentRaceVmaLogs
				.AsNoTracking()
				.OrderByDescending(l => l.CreatedDate)
				.ThenByDescending(l => l.Id)
				.ToList();
		}
	}
}
