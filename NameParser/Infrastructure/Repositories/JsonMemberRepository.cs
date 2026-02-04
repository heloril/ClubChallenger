using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            return dtos?.Select(dto => new Member(
                dto.FirstName, 
                dto.LastName, 
                dto.Email, 
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
                    dto.FirstName, 
                    dto.LastName, 
                    dto.Email,
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
