using System.Collections.Generic;
using NameParser.Domain.Entities;

namespace NameParser.Domain.Repositories
{
    public interface IRaceResultRepository
    {
        Dictionary<int, string> GetRaceResults(string filePath, List<Member> members);
    }
}
