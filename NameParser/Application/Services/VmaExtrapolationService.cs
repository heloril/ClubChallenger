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
		/// Same extrapolation as <see cref="ExtrapolateVma"/> (the value shown on the VMA
		/// screen) but returns the predicted VMA together with the number of races taken
		/// into account, and without writing a journal entry (read-only, side-effect free).
		/// </summary>
		public (double? Vma, int RaceCount) ExtrapolateVmaWithRaceCount(string firstName, string lastName)
		{
			var averageSpeeds = GetAverageSpeedByMember();
			var measuredVmas = _memberRepository.GetLatestMeasuredVmas();

			var prediction = Predict(firstName, lastName, averageSpeeds, measuredVmas);

			var key = DatabaseMemberRepository.NormalizeName(firstName, lastName);
			var raceCount = averageSpeeds.TryGetValue(key, out var info) ? info.RaceCount : 0;

			return (prediction.Predicted, raceCount);
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
			int maxRaces = 5, int monthsWindow = 3, bool rejectOutliers = false)
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
					RaceId = race.RaceId,
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

			if (rejectOutliers)
			{
				var kept = RejectOutliersAroundMedian(estimates, 0.15);
				var excluded = estimates.Count - kept.Count;
				result.AverageVma = Math.Round(kept.Average(), 1);
				result.Details = excluded > 0
					? $"VMA moyenne tout temps sur {kept.Count} course(s) (±15 % autour de la médiane, {excluded} écartée(s))."
					: $"VMA moyenne tout temps sur {kept.Count} course(s) (±15 % autour de la médiane).";
				return result;
			}

			result.AverageVma = Math.Round(estimates.Average(), 1);
			result.Details = $"VMA moyenne estimée sur {estimates.Count} course(s) récente(s) (≤ {maxRaces}, {monthsWindow} derniers mois).";
			return result;
		}

		/// <summary>
		/// Garde uniquement les valeurs à ±<paramref name="tolerance"/> (fraction, 0.15 = ±15 %)
		/// autour de la médiane, en écartant les outliers. Repli sur l'ensemble complet quand
		/// il y a 2 valeurs ou moins, ou si le filtrage retirerait tout.
		/// </summary>
		private static List<double> RejectOutliersAroundMedian(List<double> values, double tolerance)
		{
			if (values == null || values.Count <= 2)
				return values ?? new List<double>();

			var sorted = values.OrderBy(v => v).ToList();
			var mid = sorted.Count / 2;
			var median = sorted.Count % 2 != 0
				? sorted[mid]
				: (sorted[mid - 1] + sorted[mid]) / 2.0;

			if (median <= 0)
				return values;

			var lower = median * (1.0 - tolerance);
			var upper = median * (1.0 + tolerance);

			var kept = values.Where(v => v >= lower && v <= upper).ToList();
			return kept.Count > 0 ? kept : values;
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
					c.RaceId,
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
					RaceId = row.RaceId,
					RaceName = row.RaceName,
					DistanceKm = row.DistanceKm,
					RaceDate = raceDate,
					Speed = speed.Value
				});
			}

			// Deduplicate by race: keep a single (fastest) line per race so a member
			// encoded twice on the same race is not counted twice.
			return races
				.GroupBy(r => r.RaceId ?? -r.GetHashCode())
				.Select(g => g.OrderByDescending(r => r.Speed).First())
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

		// Large window used to consider every race a member ever ran ("tout temps").
		private const int AllTimeMaxRaces = 10000;
		private const int AllTimeMonthsWindow = 12000;

		/// <summary>
		/// Read-only (no persistence) computation of the member's VMA over their 5 most
		/// recent races within the last 3 months.
		/// </summary>
		public RecentRacesVmaResult ComputeRecentRacesVmaReadOnly(string firstName, string lastName)
		{
			return ComputeRecentRacesVma(firstName, lastName, 5, 3, rejectOutliers: false);
		}

		/// <summary>
		/// Read-only (no persistence) computation of the member's all-time VMA (every race,
		/// deduplicated), excluding races more than ±15 % away from the median.
		/// </summary>
		public RecentRacesVmaResult ComputeAllTimeRacesVmaReadOnly(string firstName, string lastName)
		{
			return ComputeRecentRacesVma(firstName, lastName, AllTimeMaxRaces, AllTimeMonthsWindow, rejectOutliers: true);
		}

		/// <summary>
		/// Recomputes and persists the all-time (every race) VMA log for one member,
		/// deduplicated by race reference. The average excludes races whose estimated
		/// VMA is more than ±15 % away from the median.
		/// </summary>
		public RecentRacesVmaResult RecomputeAndSaveAllTimeRacesVma(string firstName, string lastName)
		{
			var result = ComputeRecentRacesVma(firstName, lastName, AllTimeMaxRaces, AllTimeMonthsWindow, rejectOutliers: true);
			SaveRecentRacesVma(result);
			return result;
		}

		/// <summary>
		/// Recomputes and persists the all-time VMA log (based on every race ever run,
		/// deduplicated by race) for every member. Returns the number of members with a
		/// successful estimate. Logs that are not tested (manual) VMAs are cleaned up first.
		/// </summary>
		public int RecomputeAllTimeRacesVma()
		{
			CleanupComputedVmaData();

			var count = 0;
			foreach (var member in _memberRepository.GetAll())
			{
				var result = RecomputeAndSaveAllTimeRacesVma(member.FirstName, member.LastName);
				if (result != null && result.IsSuccess)
					count++;
			}
			return count;
		}

		/// <summary>
		/// Recomputes and persists the all-time VMA log only for the specified members.
		/// Used to keep VMA values up to date whenever a race is loaded/processed.
		/// Names are matched after normalization to avoid recomputing the same member twice.
		/// </summary>
		public int RecomputeAllTimeRacesVmaForMembers(IEnumerable<(string FirstName, string LastName)> members)
		{
			if (members == null)
				return 0;

			var seen = new HashSet<string>(StringComparer.Ordinal);
			var count = 0;

			foreach (var (firstName, lastName) in members)
			{
				var key = DatabaseMemberRepository.NormalizeName(firstName, lastName);
				if (string.IsNullOrEmpty(key) || !seen.Add(key))
					continue;

				var result = RecomputeAndSaveAllTimeRacesVma(firstName, lastName);
				if (result != null && result.IsSuccess)
					count++;
			}

			return count;
		}

		/// <summary>
		/// Purges every computed/derived VMA artefact from the database, keeping only the
		/// tested (manually encoded) VMA history entries. This includes the extrapolated
		/// VMA history rows, the extrapolation journal and the recent/all-time race VMA logs.
		/// Called before a full all-time recompute so the stored data only reflects the
		/// latest calculation.
		/// </summary>
		public void CleanupComputedVmaData()
		{
			try
			{
				using var context = new RaceManagementContext();

				var extrapolatedHistory = context.MemberVmaHistories.Where(h => h.IsExtrapolated);
				context.MemberVmaHistories.RemoveRange(extrapolatedHistory);

				context.VmaExtrapolationLogs.RemoveRange(context.VmaExtrapolationLogs);
				context.RecentRaceVmaLogs.RemoveRange(context.RecentRaceVmaLogs);

				context.SaveChanges();
			}
			catch
			{
				// Cleanup must never break the recompute flow.
			}
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
						RaceId = race.RaceId,
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
			public int? RaceId { get; set; }
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
