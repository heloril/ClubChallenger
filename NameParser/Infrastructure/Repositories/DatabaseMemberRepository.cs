using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NameParser.Domain.Entities;
using NameParser.Domain.Repositories;
using NameParser.Infrastructure.Data;
using NameParser.Infrastructure.Data.Models;

namespace NameParser.Infrastructure.Repositories
{
	/// <summary>
	/// Database-backed implementation of <see cref="IMemberRepository"/>.
	/// Members are stored in the Members table and the VMA value is historized
	/// in MemberVmaHistories; only the most recent VMA is surfaced on the domain
	/// <see cref="Member"/> and therefore exported in reports.
	/// </summary>
	public class DatabaseMemberRepository : IMemberRepository
	{
		public List<Member> GetAll()
		{
			using var context = new RaceManagementContext();

			var entities = context.Members
				.Include(m => m.VmaHistory)
				.AsNoTracking()
				.ToList();

			return entities
				.Select(ToDomain)
				.OrderBy(m => m.LastName, StringComparer.OrdinalIgnoreCase)
				.ThenBy(m => m.FirstName, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		public List<Member> GetMembersWithLastName()
		{
			return GetAll()
				.Where(m => !string.IsNullOrWhiteSpace(m.LastName))
				.ToList();
		}

		public Member GetMemberByName(string firstName, string lastName)
		{
			using var context = new RaceManagementContext();

			var entity = context.Members
				.Include(m => m.VmaHistory)
				.AsNoTracking()
				.AsEnumerable()
				.FirstOrDefault(m => SameName(m, firstName, lastName));

			return entity == null ? null : ToDomain(entity);
		}

		public void Add(Member member)
		{
			if (member == null)
				throw new ArgumentNullException(nameof(member));
			if (string.IsNullOrWhiteSpace(member.LastName))
				throw new ArgumentException("Last name cannot be empty", nameof(member));

			using var context = new RaceManagementContext();

			var exists = context.Members.AsEnumerable()
				.Any(m => SameName(m, member.FirstName, member.LastName));
			if (exists)
				throw new InvalidOperationException($"A member named '{member.GetFullName()}' already exists.");

			var entity = new MemberEntity
			{
				FirstName = member.FirstName,
				LastName = member.LastName,
				Email = member.Email,
				IsMember = member.IsMember,
				IsChallenger = member.IsChallenger
			};

			if (member.Vma.HasValue)
			{
				entity.VmaHistory.Add(new MemberVmaHistoryEntity
				{
					Vma = member.Vma.Value,
					RecordedDate = DateTime.Now
				});
			}

			context.Members.Add(entity);
			context.SaveChanges();
		}

		public void Update(string originalFirstName, string originalLastName, Member member)
		{
			if (member == null)
				throw new ArgumentNullException(nameof(member));
			if (string.IsNullOrWhiteSpace(member.LastName))
				throw new ArgumentException("Last name cannot be empty", nameof(member));

			using var context = new RaceManagementContext();

			var entity = context.Members
				.Include(m => m.VmaHistory)
				.AsEnumerable()
				.FirstOrDefault(m => SameName(m, originalFirstName, originalLastName));

			if (entity == null)
				throw new InvalidOperationException($"Member '{originalFirstName} {originalLastName}' was not found.");

			entity.FirstName = member.FirstName;
			entity.LastName = member.LastName;
			entity.Email = member.Email;
			entity.IsMember = member.IsMember;
			entity.IsChallenger = member.IsChallenger;

			// Historize VMA only when it actually changes.
			var latestVma = entity.VmaHistory
				.OrderByDescending(h => h.RecordedDate)
				.ThenByDescending(h => h.Id)
				.FirstOrDefault()?.Vma;

			if (member.Vma.HasValue && member.Vma.Value != latestVma)
			{
				entity.VmaHistory.Add(new MemberVmaHistoryEntity
				{
					Vma = member.Vma.Value,
					RecordedDate = DateTime.Now
				});
			}

			context.SaveChanges();
		}

		public void Remove(Member member)
		{
			if (member == null)
				throw new ArgumentNullException(nameof(member));

			using var context = new RaceManagementContext();

			var entities = context.Members
				.Include(m => m.VmaHistory)
				.AsEnumerable()
				.Where(m => SameName(m, member.FirstName, member.LastName))
				.ToList();

			if (entities.Count == 0)
				return;

			context.Members.RemoveRange(entities);
			context.SaveChanges();
		}

		public void SaveAll(IEnumerable<Member> members)
		{
			if (members == null)
				throw new ArgumentNullException(nameof(members));

			using var context = new RaceManagementContext();

			var existing = context.Members
				.Include(m => m.VmaHistory)
				.ToList();

			foreach (var member in members)
			{
				if (string.IsNullOrWhiteSpace(member.LastName))
					continue;

				var entity = existing.FirstOrDefault(m => SameName(m, member.FirstName, member.LastName));
				if (entity == null)
				{
					entity = new MemberEntity
					{
						FirstName = member.FirstName,
						LastName = member.LastName,
						Email = member.Email,
						IsMember = member.IsMember,
						IsChallenger = member.IsChallenger
					};
					context.Members.Add(entity);
					existing.Add(entity);
				}
				else
				{
					entity.FirstName = member.FirstName;
					entity.LastName = member.LastName;
					entity.Email = member.Email;
					entity.IsMember = member.IsMember;
					entity.IsChallenger = member.IsChallenger;
				}

				var latestVma = entity.VmaHistory
					.OrderByDescending(h => h.RecordedDate)
					.ThenByDescending(h => h.Id)
					.FirstOrDefault()?.Vma;

				if (member.Vma.HasValue && member.Vma.Value != latestVma)
				{
					entity.VmaHistory.Add(new MemberVmaHistoryEntity
					{
						Vma = member.Vma.Value,
						RecordedDate = DateTime.Now
					});
				}
			}

			context.SaveChanges();
		}

		private static Member ToDomain(MemberEntity entity)
		{
			var latestVma = entity.VmaHistory?
				.OrderByDescending(h => h.RecordedDate)
				.ThenByDescending(h => h.Id)
				.FirstOrDefault()?.Vma;

			return new Member(
				entity.FirstName,
				entity.LastName,
				entity.Email,
				entity.IsMember,
				entity.IsChallenger,
				latestVma);
		}

		private static bool SameName(MemberEntity entity, string firstName, string lastName)
		{
			return string.Equals(entity.FirstName ?? string.Empty, firstName ?? string.Empty, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(entity.LastName ?? string.Empty, lastName ?? string.Empty, StringComparison.OrdinalIgnoreCase);
		}
	}
}
