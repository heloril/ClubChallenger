using System;
using System.Collections.Generic;
using System.Linq;
using NameParser.Infrastructure.Data.Models;

namespace NameParser.Infrastructure.Data
{
	/// <summary>
	/// Persists and queries <see cref="VmaExtrapolationLogEntity"/> rows that explain
	/// how each VMA extrapolation was computed.
	/// </summary>
	public class VmaExtrapolationLogRepository
	{
		public void Add(VmaExtrapolationLogEntity log)
		{
			if (log == null)
				throw new ArgumentNullException(nameof(log));

			using var context = new RaceManagementContext();

			if (log.CreatedDate == default)
				log.CreatedDate = DateTime.Now;
			if (string.IsNullOrEmpty(log.CreatedBy))
				log.CreatedBy = Environment.UserName;

			context.VmaExtrapolationLogs.Add(log);
			context.SaveChanges();
		}

		/// <summary>
		/// Returns the extrapolation logs for a given member, newest first.
		/// </summary>
		public List<VmaExtrapolationLogEntity> GetLogsForMember(string firstName, string lastName)
		{
			using var context = new RaceManagementContext();

			firstName ??= string.Empty;
			lastName ??= string.Empty;

			return context.VmaExtrapolationLogs
				.Where(l => l.MemberFirstName == firstName && l.MemberLastName == lastName)
				.OrderByDescending(l => l.CreatedDate)
				.ThenByDescending(l => l.Id)
				.ToList();
		}

		public List<VmaExtrapolationLogEntity> GetRecentLogs(int maxCount = 200)
		{
			using var context = new RaceManagementContext();

			return context.VmaExtrapolationLogs
				.OrderByDescending(l => l.CreatedDate)
				.ThenByDescending(l => l.Id)
				.Take(maxCount)
				.ToList();
		}
	}
}
