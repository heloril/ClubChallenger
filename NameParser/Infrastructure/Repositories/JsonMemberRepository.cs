using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NameParser.Domain.Entities;
using NameParser.Domain.Repositories;
using Newtonsoft.Json;

namespace NameParser.Infrastructure.Repositories
{
    public class JsonMemberRepository : IMemberRepository
    {
        private readonly string _jsonFileName;

        public JsonMemberRepository(string jsonFileName = "Members.json")
        {
            _jsonFileName = jsonFileName;
        }

        public List<Member> GetAll()
        {
            var filePath = GetFilePath();
            if (!File.Exists(filePath))
                return new List<Member>();

            var json = File.ReadAllText(filePath);
            var dtos = JsonConvert.DeserializeObject<List<MemberDto>>(json);

            return dtos?
                .Where(dto => !string.IsNullOrWhiteSpace(dto.LastName))
                .Select(dto => new Member(
                    FixEncoding(dto.FirstName), 
                    FixEncoding(dto.LastName), 
                    FixEncoding(dto.Email), 
                    dto.IsMember ?? true,  // Default to true if not specified
                    dto.IsChallenger ?? false  // Default to false if not specified
                )).ToList() ?? new List<Member>();
        }

        public List<Member> GetMembersWithLastName()
        {
            var filePath = GetFilePath();
            if (!File.Exists(filePath))
                return new List<Member>();

            var json = File.ReadAllText(filePath);
            var dtos = JsonConvert.DeserializeObject<List<MemberDto>>(json);

            return dtos?
                .Where(dto => !string.IsNullOrWhiteSpace(dto.LastName))
                .Select(dto => new Member(
                    FixEncoding(dto.FirstName), 
                    FixEncoding(dto.LastName), 
                    FixEncoding(dto.Email),
                    dto.IsMember ?? true,  // Default to true if not specified
                    dto.IsChallenger ?? false  // Default to false if not specified
                ))
                .ToList()
                ?? new List<Member>();
        }

        public Member GetMemberByName(string firstName, string lastName)
        {
            var members = GetAll();
            return members.FirstOrDefault(m => 
                m.FirstName.Equals(firstName, StringComparison.OrdinalIgnoreCase) && 
                m.LastName.Equals(lastName, StringComparison.OrdinalIgnoreCase));
        }

        public void Add(Member member)
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member));
            if (string.IsNullOrWhiteSpace(member.LastName))
                throw new ArgumentException("Last name cannot be empty", nameof(member));

            var members = GetAll();
            if (members.Any(m => SameName(m, member.FirstName, member.LastName)))
                throw new InvalidOperationException($"A member named '{member.GetFullName()}' already exists.");

            members.Add(member);
            SaveAll(members);
        }

        public void Update(string originalFirstName, string originalLastName, Member member)
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member));
            if (string.IsNullOrWhiteSpace(member.LastName))
                throw new ArgumentException("Last name cannot be empty", nameof(member));

            var members = GetAll();
            var index = members.FindIndex(m => SameName(m, originalFirstName, originalLastName));
            if (index < 0)
                throw new InvalidOperationException($"Member '{originalFirstName} {originalLastName}' was not found.");

            members[index] = member;
            SaveAll(members);
        }

        public void Remove(Member member)
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member));

            var members = GetAll();
            members.RemoveAll(m => SameName(m, member.FirstName, member.LastName));
            SaveAll(members);
        }

        public void SaveAll(IEnumerable<Member> members)
        {
            if (members == null)
                throw new ArgumentNullException(nameof(members));

            var dtos = members.Select(m => new MemberDto
            {
                FirstName = m.FirstName,
                LastName = m.LastName,
                Email = m.Email,
                IsMember = m.IsMember,
                IsChallenger = m.IsChallenger
            }).ToList();

            var json = JsonConvert.SerializeObject(dtos, Formatting.Indented);
            File.WriteAllText(GetFilePath(), json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        /// <summary>
        /// Repairs classic UTF-8-interpreted-as-Latin1 mojibake (e.g. "MichaÃ«l" -> "Michaël").
        /// Returns the original value when it is not mojibake or cannot be safely repaired.
        /// </summary>
        private static string FixEncoding(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // Only attempt a repair when typical mojibake markers are present.
            if (value.IndexOf('\u00C3') < 0 && value.IndexOf('\u00C2') < 0)
                return value;

            try
            {
                var latin1Bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(value);
                var strictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
                return strictUtf8.GetString(latin1Bytes);
            }
            catch (Exception)
            {
                // Not valid UTF-8 after re-encoding: keep the original value.
                return value;
            }
        }

        private static bool SameName(Member member, string firstName, string lastName)
        {
            return string.Equals(member.FirstName ?? string.Empty, firstName ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(member.LastName ?? string.Empty, lastName ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        private string GetFilePath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _jsonFileName);
        }

        private class MemberDto
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public bool? IsMember { get; set; }
            public bool? IsChallenger { get; set; }
        }
    }
}
