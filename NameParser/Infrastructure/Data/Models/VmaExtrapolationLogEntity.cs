using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NameParser.Infrastructure.Data.Models
{
	/// <summary>
	/// Persisted audit log describing how a VMA value was extrapolated for a member:
	/// which calibration method was used, how many calibration points were available,
	/// the regression parameters and the resulting predicted VMA. Consultable from the UI.
	/// </summary>
	[Table("VmaExtrapolationLogs")]
	public class VmaExtrapolationLogEntity
	{
		[Key]
		public int Id { get; set; }

		[Required]
		[MaxLength(100)]
		public string MemberFirstName { get; set; }

		[Required]
		[MaxLength(100)]
		public string MemberLastName { get; set; }

		public DateTime CreatedDate { get; set; }

		/// <summary>
		/// Calibration method used: "Regression", "Ratio" or "None".
		/// </summary>
		[MaxLength(50)]
		public string Method { get; set; }

		/// <summary>
		/// Number of calibration points (other members with a measured VMA and race data).
		/// </summary>
		public int CalibrationPointCount { get; set; }

		/// <summary>
		/// Target member's average race speed (km/h) used as the regression input.
		/// </summary>
		public double? TargetSpeed { get; set; }

		/// <summary>
		/// Regression slope (null when the ratio fallback or no method was used).
		/// </summary>
		public double? Slope { get; set; }

		/// <summary>
		/// Regression intercept (null when the ratio fallback or no method was used).
		/// </summary>
		public double? Intercept { get; set; }

		/// <summary>
		/// Predicted VMA (km/h), null when extrapolation was not possible.
		/// </summary>
		public double? PredictedVma { get; set; }

		public bool IsSuccess { get; set; }

		/// <summary>
		/// Human-readable explanation of how the value was derived (or why it failed).
		/// </summary>
		[MaxLength(1000)]
		public string Details { get; set; }

		[MaxLength(200)]
		public string CreatedBy { get; set; }
	}
}
