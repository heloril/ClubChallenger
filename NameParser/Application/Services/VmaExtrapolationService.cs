using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NameParser.Infrastructure.Data;
using NameParser.Infrastructure.Data.Models;
using NameParser.Infrastructure.Repositories;

namespace NameParser.Application.Services
{
	/// <summary>
	/// Extrapolates an athlete's VMA from their race times by calibrating a linear
	/// regression (average race speed -> VMA) on members who already have a measured
	/// (tested) VMA. Falls back to a simple VMA/speed ratio when too few calibration
	/// points are available.
	/// </summary>
	public class VmaExtrapolationService
	{
		private readonly DatabaseMemberRepository _memberRepository;
		private readonly VmaExtrapolationLogRepository _logRepository;

		public VmaExtrapolationService(DatabaseMemberRepository memberRepository)
		{
			_memberRepository = memberRepository ?? throw new ArgumentNullException(nameof(memberRepository));
			_logRepository = new VmaExtrapolationLogRepository();
			_recentRaceVmaRepository = new RecentRaceVmaRepository();
		}

		private readonly RecentRaceVmaRepository _recentRaceVmaRepository;

		/// <summary>
		/// Returns the extrapolated VMA (km/h) for the given member, or null when the
		/// athlete has no exploitable race data or no calibration data is available.
		/// </summary>
		public double? ExtrapolateVma(string firstName, string lastName)
		{
			var averageSpeeds = GetAverageSpeedByMember();
			var measuredVmas = _memberRepository.GetLatestMeasuredVmas();

			var prediction = Predict(firstName, lastName, averageSpeeds, measuredVmas);
			WriteLog(firstName, lastName, prediction);
			return prediction.Predicted;
		}

		/// <summary>
		/// Extrapolates the VMA for every member who has more than one exploitable race,
		/// persisting a new extrapolated VMA record and a journal entry for each.
		/// Calibration always relies on measured (tested) VMAs only, never on previously
		/// extrapolated values. Returns the number of members successfully extrapolated.
		/// </summary>
		public int ExtrapolateAllVma(int minRaces = 2)
		{
			var averageSpeeds = GetAverageSpeedByMember();
			var measuredVmas = _memberRepository.GetLatestMeasuredVmas();

			var count = 0;
			foreach (var member in _memberRepository.GetAll())
			{
				var key = DatabaseMemberRepository.NormalizeName(member.FirstName, member.LastName);
				if (!averageSpeeds.TryGetValue(key, out var speedInfo) || speedInfo.RaceCount < minRaces)
					continue;

				var prediction = Predict(member.FirstName, member.LastName, averageSpeeds, measuredVmas);
				WriteLog(member.FirstName, member.LastName, prediction);

				if (prediction.Predicted.HasValue)
				{
					_memberRepository.AddVmaRecord(member.FirstName, member.LastName, prediction.Predicted.Value, isExtrapolated: true);
					count++;
				}
			}

			return count;
		}

		// ---------------------------------------------------------------------
		// Recent-races VMA estimate (up to the 5 most recent races within the
		// last 3 months). Each race's VMA is estimated from its speed using the
		// same calibration (regression/ratio on other members' measured VMAs).
		// The result is averaged and journaled in a dedicated log.
		// ---------------------------------------------------------------------

		/// <summary>
		/// Computes the recent-races VMA estimate for one member: estimates a VMA for
		/// each of the (up to 5) most recent races within the last 3 months, then
		/// averages them. Returns null when the member has no race in that window or no
		/// calibration data is available.
		/// </summary>
		public RecentRacesVmaResult ComputeRecentRacesVma(string firstName, string lastName,
			int maxRaces = 5, int monthsWindow = 3)
		{
			var measuredVmas = _memberRepository.GetLatestMeasuredVmas();
			var averageSpeeds = GetAverageSpeedByMember();
			var calibration = BuildCalibration(firstName, lastName, averageSpeeds, measuredVmas);

			var races = GetRecentRacesForMember(firstName, lastName, maxRaces, monthsWindow);

			var result = new RecentRacesVmaResult
			{
				FirstName = firstName,
				LastName = lastName
			};

			if (races.Count == 0)
			{
				result.Details = "Aucune course dans les 3 derniers mois : estimation impossible.";
				return result;
			}

			var estimates = new List<double>();
			foreach (var race in races)
			{
				double? estimated = null;
				if (calibration != null && race.Speed > 0)
				{
					var value = calibration(race.Speed);
					if (value.HasValue && value.Value > 0)
						estimated = Math.Round(value.Value, 1);
				}

				result.Races.Add(new RecentRaceVmaItem
				{
					RaceName = race.RaceName,
					DistanceKm = race.DistanceKm,
					RaceDate = race.RaceDate,
					Speed = race.Speed,
					EstimatedVma = estimated
				});

				if (estimated.HasValue)
					estimates.Add(estimated.Value);
			}

			if (estimates.Count == 0)
			{
				result.Details = $"{races.Count} course(s) trouvée(s) mais aucune donnée de calibration exploitable : estimation impossible.";
				return result;
			}

			result.AverageVma = Math.Round(estimates.Average(), 1);
			result.Details = $"VMA moyenne estimée sur {estimates.Count} course(s) récente(s) (≤ {maxRaces}, {monthsWindow} derniers mois).";
			return result;
		}

		/// <summary>
		/// Builds a calibration function (speed km/h -&gt; estimated VMA km/h) from other
		/// members' measured VMAs and average race speeds. Returns null when no
		/// calibration data is available.
		/// </summary>
		private Func<double, double?> BuildCalibration(string firstName, string lastName,
			Dictionary<string, (double AvgSpeed, int RaceCount)> averageSpeeds,
			Dictionary<string, double> measuredVmas)
		{
			var targetKey = DatabaseMemberRepository.NormalizeName(firstName, lastName);

			var points = new List<(double speed, double vma)>();
			foreach (var kv in measuredVmas)
			{
				if (string.Equals(kv.Key, targetKey, StringComparison.Ordinal))
					continue;

				if (averageSpeeds.TryGetValue(kv.Key, out var other) && other.AvgSpeed > 0 && kv.Value > 0)
					points.Add((other.AvgSpeed, kv.Value));
			}

			if (points.Count >= 2)
			{
				var (slope, intercept) = LinearRegression(points);
				return speed =>
				{
					var predicted = slope * speed + intercept;
					return predicted > 0 ? predicted : (double?)null;
				};
			}

			if (points.Count >= 1)
			{
				var averageRatio = points.Average(p => p.vma / p.speed);
				return speed =>
				{
					var predicted = speed * averageRatio;
					return predicted > 0 ? predicted : (double?)null;
				};
			}

			return null;
		}

		/// <summary>
		/// Returns the (up to <paramref name="maxRaces"/>) most recent races of a member
		/// within the last <paramref name="monthsWindow"/> months, each with its date and
		/// average speed (km/h).
		/// </summary>
		private static List<RecentRaceRow> GetRecentRacesForMember(string firstName, string lastName,
			int maxRaces, int monthsWindow)
		{
			using var context = new RaceManagementContext();

			var targetKey = DatabaseMemberRepository.NormalizeName(firstName, lastName);
			var cutoff = DateTime.Now.Date.AddMonths(-monthsWindow);

			var rows = context.Classifications
				.AsNoTracking()
				.Select(c => new
				{
					c.MemberFirstName,
					c.MemberLastName,
					c.Speed,
					c.RaceTime,
					RaceName = c.Race.Name,
					DistanceKm = (int?)c.Race.DistanceKm,
					EventDate = (DateTime?)c.Race.RaceEvent.EventDate,
					RaceCreated = c.Race.CreatedDate
				})
				.ToList();

			var races = new List<RecentRaceRow>();
			foreach (var row in rows)
			{
				var key = DatabaseMemberRepository.NormalizeName(row.MemberFirstName, row.MemberLastName);
				if (!string.Equals(key, targetKey, StringComparison.Ordinal))
					continue;

				double? speed = row.Speed;
				if ((!speed.HasValue || speed.Value <= 0)
					&& row.RaceTime.HasValue && row.RaceTime.Value.TotalHours > 0
					&& row.DistanceKm.HasValue && row.DistanceKm.Value > 0)
				{
					speed = row.DistanceKm.Value / row.RaceTime.Value.TotalHours;
				}

				if (!speed.HasValue || speed.Value <= 0)
					continue;

				var raceDate = row.EventDate ?? row.RaceCreated;
				if (raceDate < cutoff)
					continue;

				races.Add(new RecentRaceRow
				{
					RaceName = row.RaceName,
					DistanceKm = row.DistanceKm,
					RaceDate = raceDate,
					Speed = speed.Value
				});
			}

			return races
				.OrderByDescending(r => r.RaceDate)
				.Take(maxRaces)
				.ToList();
		}

		/// <summary>
		/// Returns the athletes whose recent-races VMA is closest to <paramref name="referenceVma"/>:
		/// the <paramref name="strongerCount"/> just above and <paramref name="weakerCount"/> just
		/// below, excluding the reference member. Only members with a computable recent-races VMA
		/// are considered.
		/// </summary>
		public NearbyAthletesResult GetAthletesNearVma(string firstName, string lastName,
			double referenceVma, int strongerCount = 3, int weakerCount = 3)
		{
			var result = new NearbyAthletesResult();
			if (referenceVma <= 0)
				return result;

			var measuredVmas = _memberRepository.GetLatestMeasuredVmas();
			var averageSpeeds = GetAverageSpeedByMember();
			var targetKey = DatabaseMemberRepository.NormalizeName(firstName, lastName);

			var all = new List<NearbyAthlete>();
			foreach (var member in _memberRepository.GetAll())
			{
				var key = DatabaseMemberRepository.NormalizeName(member.FirstName, member.LastName);
				if (string.Equals(key, targetKey, StringComparison.Ordinal))
					continue;

				if (!averageSpeeds.TryGetValue(key, out var speedInfo) || speedInfo.AvgSpeed <= 0)
					continue;

				var calibration = BuildCalibration(member.FirstName, member.LastName, averageSpeeds, measuredVmas);
				if (calibration == null)
					continue;

				var estimated = calibration(speedInfo.AvgSpeed);
				if (!estimated.HasValue || estimated.Value <= 0)
					continue;

				all.Add(new NearbyAthlete
				{
					FirstName = member.FirstName,
					LastName = member.LastName,
					Vma = Math.Round(estimated.Value, 1)
				});
			}

			result.Stronger = all
				.Where(a => a.Vma > referenceVma)
				.OrderBy(a => a.Vma)
				.Take(strongerCount)
				.OrderByDescending(a => a.Vma)
				.ToList();

			result.Weaker = all
				.Where(a => a.Vma < referenceVma)
				.OrderByDescending(a => a.Vma)
				.Take(weakerCount)
				.ToList();

			return result;
		}

		/// <summary>
		/// Recomputes and persists the recent-races VMA log for a single member.
		/// Called after a new race result is encoded.
		/// </summary>
		public RecentRacesVmaResult RecomputeAndSaveRecentRacesVma(string firstName, string lastName,
			int maxRaces = 5, int monthsWindow = 3)
		{
			var result = ComputeRecentRacesVma(firstName, lastName, maxRaces, monthsWindow);
			SaveRecentRacesVma(result);
			return result;
		}

		/// <summary>
		/// Recomputes and persists the recent-races VMA log for every member. Returns the
		/// number of members with a successful estimate.
		/// </summary>
		public int RecomputeAllRecentRacesVma(int maxRaces = 5, int monthsWindow = 3)
		{
			var count = 0;
			foreach (var member in _memberRepository.GetAll())
			{
				var result = ComputeRecentRacesVma(member.FirstName, member.LastName, maxRaces, monthsWindow);
				SaveRecentRacesVma(result);
				if (result.IsSuccess)
					count++;
			}
			return count;
		}

		private void SaveRecentRacesVma(RecentRacesVmaResult result)
		{
			if (result == null)
				return;

			try
			{
				var entity = new RecentRaceVmaLogEntity
				{
					MemberFirstName = result.FirstName ?? string.Empty,
					MemberLastName = result.LastName ?? string.Empty,
					CreatedDate = DateTime.Now,
					AverageVma = result.AverageVma,
					RaceCount = result.Races.Count,
					IsSuccess = result.IsSuccess,
					Details = result.Details
				};

				foreach (var race in result.Races)
				{
					entity.Lines.Add(new RecentRaceVmaLineEntity
					{
						RaceName = race.RaceName,
						DistanceKm = race.DistanceKm,
						RaceDate = race.RaceDate,
						Speed = race.Speed,
						EstimatedVma = race.EstimatedVma
					});
				}

				_recentRaceVmaRepository.Save(entity);
			}
			catch
			{
				// Persistence must never break the encoding flow.
			}
		}

		private sealed class RecentRaceRow
		{
			public string RaceName { get; set; }
			public int? DistanceKm { get; set; }
			public DateTime RaceDate { get; set; }
			public double Speed { get; set; }
		}

		/// <summary>
		/// Pure (no persistence) computation of the extrapolated VMA for one member,
		/// returning the prediction and everything required to journal it.
		/// </summary>
		private PredictionResult Predict(string firstName, string lastName,
			Dictionary<string, (double AvgSpeed, int RaceCount)> averageSpeeds,
			Dictionary<string, double> measuredVmas)
		{
			var targetKey = DatabaseMemberRepository.NormalizeName(firstName, lastName);
			if (!averageSpeeds.TryGetValue(targetKey, out var target) || target.AvgSpeed <= 0)
			{
				return PredictionResult.Failure("None", 0, null,
					"Aucune vitesse moyenne en course exploitable pour ce membre : extrapolation impossible.");
			}

			var targetSpeed = target.AvgSpeed;

			var points = new List<(double speed, double vma)>();
			foreach (var kv in measuredVmas)
			{
				// Do not calibrate on the target member's own value.
				if (string.Equals(kv.Key, targetKey, StringComparison.Ordinal))
					continue;

				if (averageSpeeds.TryGetValue(kv.Key, out var other) && other.AvgSpeed > 0 && kv.Value > 0)
					points.Add((other.AvgSpeed, kv.Value));
			}

			if (points.Count >= 2)
			{
				var (slope, intercept) = LinearRegression(points);
				var predicted = slope * targetSpeed + intercept;
				if (predicted > 0)
				{
					var rounded = Math.Round(predicted, 1);
					return new PredictionResult
					{
						Predicted = rounded,
						Method = "Regression",
						PointCount = points.Count,
						TargetSpeed = targetSpeed,
						Slope = slope,
						Intercept = intercept,
						Details = $"Régression linéaire sur {points.Count} VMA testées (meilleures courses retenues, vitesse moyenne {Fmt(targetSpeed)} km/h sur {target.RaceCount} course(s)) : VMA = {Fmt(slope)} × vitesse + {Fmt(intercept)} = {Fmt(rounded)} km/h."
					};
				}
			}

			if (points.Count >= 1)
			{
				var averageRatio = points.Average(p => p.vma / p.speed);
				var predicted = targetSpeed * averageRatio;
				if (predicted > 0)
				{
					var rounded = Math.Round(predicted, 1);
					return new PredictionResult
					{
						Predicted = rounded,
						Method = "Ratio",
						PointCount = points.Count,
						TargetSpeed = targetSpeed,
						Details = $"Trop peu de points pour une régression : ratio moyen VMA/vitesse ({Fmt(averageRatio)}) appliqué à {Fmt(targetSpeed)} km/h = {Fmt(rounded)} km/h."
					};
				}
			}

			return PredictionResult.Failure("None", points.Count, targetSpeed,
				"Aucune donnée de calibration exploitable (VMA testées d'autres membres) : extrapolation impossible.");
		}

		private static string Fmt(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

		private void WriteLog(string firstName, string lastName, PredictionResult prediction)
		{
			try
			{
				_logRepository.Add(new VmaExtrapolationLogEntity
				{
					MemberFirstName = firstName ?? string.Empty,
					MemberLastName = lastName ?? string.Empty,
					CreatedDate = DateTime.Now,
					Method = prediction.Method,
					CalibrationPointCount = prediction.PointCount,
					TargetSpeed = prediction.TargetSpeed,
					Slope = prediction.Slope,
					Intercept = prediction.Intercept,
					PredictedVma = prediction.Predicted,
					IsSuccess = prediction.Predicted.HasValue,
					Details = prediction.Details
				});
			}
			catch
			{
				// Logging must never break the extrapolation itself.
			}
		}

		private readonly struct PredictionResult
		{
			public double? Predicted { get; init; }
			public string Method { get; init; }
			public int PointCount { get; init; }
			public double? TargetSpeed { get; init; }
			public double? Slope { get; init; }
			public double? Intercept { get; init; }
			public string Details { get; init; }

			public static PredictionResult Failure(string method, int pointCount, double? targetSpeed, string details)
				=> new PredictionResult
				{
					Predicted = null,
					Method = method,
					PointCount = pointCount,
					TargetSpeed = targetSpeed,
					Details = details
				};
		}

		/// <summary>
		/// Computes the average race speed (km/h) and race count per member keyed by
		/// normalized full name. To reduce the impact of bad races, only the best
		/// <paramref name="keepBestFraction"/> share of each member's races (fastest
		/// speeds) is averaged, while the full race count is preserved for thresholds.
		/// </summary>
		private static Dictionary<string, (double AvgSpeed, int RaceCount)> GetAverageSpeedByMember(double keepBestFraction = 0.5)
		{
			using var context = new RaceManagementContext();

			// Project only the scalar columns required for the speed computation.
			// Avoid Include(c => c.Race) which would materialize the full RaceEntity,
			// including its potentially large FileContent (Excel/PDF) blob, for every row.
			var rows = context.Classifications
				.AsNoTracking()
				.Select(c => new
				{
					c.MemberFirstName,
					c.MemberLastName,
					c.Speed,
					c.RaceTime,
					DistanceKm = (int?)c.Race.DistanceKm
				})
				.ToList();

			var speedsByMember = new Dictionary<string, List<double>>();

			foreach (var row in rows)
			{
				double? speed = row.Speed;

				if ((!speed.HasValue || speed.Value <= 0)
					&& row.RaceTime.HasValue
					&& row.RaceTime.Value.TotalHours > 0
					&& row.DistanceKm.HasValue
					&& row.DistanceKm.Value > 0)
				{
					speed = row.DistanceKm.Value / row.RaceTime.Value.TotalHours;
				}

				if (!speed.HasValue || speed.Value <= 0)
					continue;

				var key = DatabaseMemberRepository.NormalizeName(row.MemberFirstName, row.MemberLastName);
				if (!speedsByMember.TryGetValue(key, out var list))
				{
					list = new List<double>();
					speedsByMember[key] = list;
				}

				list.Add(speed.Value);
			}

			var result = new Dictionary<string, (double AvgSpeed, int RaceCount)>(speedsByMember.Count);
			foreach (var kv in speedsByMember)
			{
				var speeds = kv.Value;
				var raceCount = speeds.Count;

				// Keep the fastest share of races (exclude the slowest ones).
				var keep = Math.Max(1, (int)Math.Ceiling(raceCount * keepBestFraction));
				var bestAverage = speeds
					.OrderByDescending(s => s)
					.Take(keep)
					.Average();

				result[kv.Key] = (bestAverage, raceCount);
			}

			return result;
		}

		/// <summary>
		/// Ordinary least squares linear regression returning (slope, intercept).
		/// </summary>
		private static (double slope, double intercept) LinearRegression(IReadOnlyList<(double x, double y)> points)
		{
			var n = points.Count;
			var sumX = points.Sum(p => p.x);
			var sumY = points.Sum(p => p.y);
			var sumXy = points.Sum(p => p.x * p.y);
			var sumXx = points.Sum(p => p.x * p.x);

			var denominator = (n * sumXx) - (sumX * sumX);
			if (Math.Abs(denominator) < 1e-9)
			{
				// Degenerate (all identical speeds): fall back to a flat line at the mean VMA.
				return (0, sumY / n);
			}

			var slope = ((n * sumXy) - (sumX * sumY)) / denominator;
			var intercept = (sumY - (slope * sumX)) / n;
			return (slope, intercept);
		}
	}
}
