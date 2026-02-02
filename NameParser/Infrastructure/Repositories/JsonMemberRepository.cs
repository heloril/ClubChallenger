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

            return dtos?.Select(dto => new Member(dto.FirstName, dto.LastName, dto.Email)).ToList()
                   ?? new List<Member>();
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
                .Select(dto => new Member(dto.FirstName, dto.LastName, dto.Email))
                .ToList()
                ?? new List<Member>();
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
        }
    }
}
