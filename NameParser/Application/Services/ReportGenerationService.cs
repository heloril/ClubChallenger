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

            foreach (var member in members)
            {
                var line = new StringBuilder();
                line.Append(member.ToString());

                foreach (var raceName in distinctRaceNames)
                {
                    var raceDistance = new Domain.Entities.RaceDistance(0, raceName, 0);
                    var memberClassification = classification.GetClassification(member, raceDistance);

                    if (memberClassification != null)
                    {
                        line.Append($";{memberClassification.Points};{memberClassification.BonusKm}");
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
