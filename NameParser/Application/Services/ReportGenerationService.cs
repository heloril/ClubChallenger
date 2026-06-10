using System.Linq;
using System.Text;
using NameParser.Domain.Aggregates;
using NameParser.Domain.Repositories;

namespace NameParser.Application.Services
{
    public class ReportGenerationService
    {
        private readonly IMemberRepository _memberRepository;

        public ReportGenerationService(IMemberRepository memberRepository)
        {
            _memberRepository = memberRepository;
        }

        public string GenerateReport(Classification classification)
        {
            var report = new StringBuilder();
            var members = _memberRepository.GetAll().OrderBy(m => m.LastName).ThenBy(m => m.FirstName);
            var distinctRaceNames = classification.GetDistinctRaceNames().ToList();
            var allClassifications = classification.GetAllClassifications().ToList();

            foreach (var member in members)
            {
                var line = new StringBuilder();
                line.Append(member.ToString());

                foreach (var raceName in distinctRaceNames)
                {
                    // Find all classifications for this member and race (can be multiple if same name, different positions)
                    // Sum points and bonus km for all matching entries
                    var memberClassifications = allClassifications
                        .Where(c => c.Member.FirstName == member.FirstName && 
                                    c.Member.LastName == member.LastName && 
                                    c.RaceName == raceName)
                        .ToList();

                    if (memberClassifications.Any())
                    {
                        var totalPoints = memberClassifications.Sum(c => c.Points);
                        var totalBonusKm = memberClassifications.Sum(c => c.BonusKm);
                        line.Append($";{totalPoints};{totalBonusKm}");
                    }
                    else
                    {
                        line.Append(";0;0");
                    }
                }

                report.AppendLine(line.ToString());
            }

            return report.ToString();
        }
    }
}
