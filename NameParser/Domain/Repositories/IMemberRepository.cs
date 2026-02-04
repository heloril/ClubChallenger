using System.Collections.Generic;
using NameParser.Domain.Entities;

namespace NameParser.Domain.Repositories
{
    public interface IMemberRepository
    {
        List<Member> GetAll();
        List<Member> GetMembersWithLastName();
        Member GetMemberByName(string firstName, string lastName);
    }
}
