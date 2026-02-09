using System;
using System.Collections.Generic;
using System.Linq;
using NameParser.Domain.Entities;
using NameParser.Domain.Repositories;

namespace NameParser.Application.Services
{
    public class MemberService
    {
        private readonly IMemberRepository _memberRepository;
        private readonly IMemberRepository _challengerRepository;

        public MemberService(IMemberRepository memberRepository, IMemberRepository challengerRepository)
        {
            _memberRepository = memberRepository;
            _challengerRepository = challengerRepository;
        }

        public List<Member> GetAllMembersAndChallengers()
        {
            var members = _memberRepository.GetMembersWithLastName()
                .Select(m => new Member(m.FirstName, m.LastName, m.Email, isMember: true, isChallenger: false))
                .ToList();

            var challengers = _challengerRepository.GetMembersWithLastName()
                .Select(c => new Member(c.FirstName, c.LastName, c.Email, isMember: false, isChallenger: true))
                .ToList();

            var result = new List<Member>();
            var processedKeys = new HashSet<string>();

            foreach (var member in members)
            {
                var key = GetMemberKey(member);
                processedKeys.Add(key);

                var matchingChallenger = challengers.FirstOrDefault(c => GetMemberKey(c) == key);
                if (matchingChallenger != null)
                {
                    member.IsChallenger = true;
                }

                result.Add(member);
            }

            foreach (var challenger in challengers)
            {
                var key = GetMemberKey(challenger);
                if (!processedKeys.Contains(key))
                {
                    result.Add(challenger);
                }
            }

            return result;
        }

        private string GetMemberKey(Member member)
        {
            var normalizedFirstName = member.FirstName?.NormalizeForComparison() ?? "";
            var normalizedLastName = member.LastName?.NormalizeForComparison() ?? "";
            return $"{normalizedFirstName}|{normalizedLastName}";
        }
    }
}
