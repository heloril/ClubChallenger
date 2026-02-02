using System;
using System.Collections.Generic;
using System.Linq;
using NameParser.Domain.Entities;

namespace NameParser.Domain.Aggregates
{
    public class Classification
    {
        private readonly Dictionary<string, MemberClassification> _classifications;

        public Classification()
        {
            _classifications = new Dictionary<string, MemberClassification>();
        }

        public void AddOrUpdateResult(Member member, Race race, int points)
        {
            AddOrUpdateResult(member, race, points, null, null, null, null, null, true);
        }

        public void AddOrUpdateResult(Member member, Race race, int points, TimeSpan? raceTime, TimeSpan? timePerKm)
        {
            AddOrUpdateResult(member, race, points, raceTime, timePerKm, null, null, null, true);
        }

        public void AddOrUpdateResult(Member member, Race race, int points, TimeSpan? raceTime, TimeSpan? timePerKm, int? position)
        {
            AddOrUpdateResult(member, race, points, raceTime, timePerKm, position, null, null, true);
        }

        public void AddOrUpdateResult(Member member, Race race, int points, TimeSpan? raceTime, TimeSpan? timePerKm, int? position, string team, double? speed, bool isMember)
        {
            AddOrUpdateResult(member, race, points, raceTime, timePerKm, position, team, speed, isMember, null, null, null, null);
        }

        public void AddOrUpdateResult(Member member, Race race, int points, TimeSpan? raceTime, TimeSpan? timePerKm, int? position, string team, double? speed, bool isMember, string sex, int? positionBySex, string ageCategory, int? positionByCategory)
        {
            var key = GetKey(member, race);

            if (_classifications.TryGetValue(key, out var existing))
            {
                existing.UpdatePoints(points);
                existing.AddBonus(race.DistanceKm);
                existing.UpdateTimes(raceTime, timePerKm);
                existing.UpdatePosition(position);
                existing.UpdateTeamAndSpeed(team, speed);
                existing.UpdateCategoryInfo(sex, positionBySex, ageCategory, positionByCategory);
            }
            else
            {
                _classifications[key] = new MemberClassification(member, race, points, race.DistanceKm, raceTime, timePerKm, position, team, speed, isMember, sex, positionBySex, ageCategory, positionByCategory);
            }
        }

        public IEnumerable<MemberClassification> GetAllClassifications()
        {
            return _classifications.Values.OrderBy(c => c.Member.LastName).ThenBy(c => c.Member.FirstName);
        }

        public MemberClassification GetClassification(Member member, Race race)
        {
            var key = GetKey(member, race);
            return _classifications.TryGetValue(key, out var classification) ? classification : null;
        }

        public IEnumerable<string> GetDistinctRaceNames()
        {
            return _classifications.Values.Select(c => c.RaceName).Distinct();
        }

        private string GetKey(Member member, Race race)
        {
            return $"{member.GetFullName()}_{race.Name}";
        }
    }

    public class MemberClassification
    {
        public Member Member { get; private set; }
        public string RaceName { get; private set; }
        public int Points { get; private set; }
        public int BonusKm { get; private set; }
        public TimeSpan? RaceTime { get; private set; }
        public TimeSpan? TimePerKm { get; private set; }
        public int? Position { get; private set; }
        public string Team { get; private set; }
        public double? Speed { get; private set; }
        public string Sex { get; private set; }
        public int? PositionBySex { get; private set; }
        public string AgeCategory { get; private set; }
        public int? PositionByCategory { get; private set; }
        public bool IsMember { get; private set; }
        public bool IsChallenger { get; private set; }

        public MemberClassification(Member member, Race race, int points, int bonusKm, TimeSpan? raceTime = null, TimeSpan? timePerKm = null, int? position = null, string team = null, double? speed = null, bool isMember = true, string sex = null, int? positionBySex = null, string ageCategory = null, int? positionByCategory = null)
        {
            Member = member ?? throw new ArgumentNullException(nameof(member));
            RaceName = race?.Name ?? throw new ArgumentNullException(nameof(race));
            Points = points;
            BonusKm = bonusKm;
            RaceTime = raceTime;
            TimePerKm = timePerKm;
            Position = position;
            Team = team;
            Speed = speed;
            IsMember = isMember;
            IsChallenger = member.IsChallenger;
            Sex = sex;
            PositionBySex = positionBySex;
            AgeCategory = ageCategory;
            PositionByCategory = positionByCategory;
        }

        public void UpdatePoints(int newPoints)
        {
            if (newPoints > Points)
                Points = newPoints;
        }

        public void UpdateTimes(TimeSpan? raceTime, TimeSpan? timePerKm)
        {
            RaceTime = raceTime;
            TimePerKm = timePerKm;
        }

        public void UpdatePosition(int? position)
        {
            Position = position;
        }

        public void UpdateTeamAndSpeed(string team, double? speed)
        {
            Team = team;
            Speed = speed;
        }

        public void UpdateCategoryInfo(string sex, int? positionBySex, string ageCategory, int? positionByCategory)
        {
            Sex = sex;
            PositionBySex = positionBySex;
            AgeCategory = ageCategory;
            PositionByCategory = positionByCategory;
        }

        public void AddBonus(int kilometers)
        {
            BonusKm += kilometers;
        }
    }
}
