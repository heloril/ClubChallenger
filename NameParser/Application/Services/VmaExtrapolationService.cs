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
		}

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
