using System.Collections.Generic;
using NameParser.Domain.Entities;

namespace NameParser.Domain.Repositories
{
    public interface IMemberRepository
    {
        List<Member> GetAll();
        List<Member> GetMembersWithLastName();
        Member GetMemberByName(string firstName, string lastName);
        void Add(Member member);
        void Update(string originalFirstName, string originalLastName, Member member);
        void Remove(Member member);
        void SaveAll(IEnumerable<Member> members);
    }
}
