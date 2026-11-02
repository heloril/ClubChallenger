using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace NameParser.Application.Services
{
	/// <summary>
	/// Résout les pourcentages de VMA présents dans un texte/HTML d'e-mail. Le texte
	/// d'origine est conservé tel quel ; une liste récapitulative est ajoutée à la fin,
	/// une ligne par pourcentage distinct rencontré (les plages "X-Y% VMA" sont étendues
	/// aux deux bornes X et Y), au format :
	///   75% VMA : 4'12"6 au km, 25"3 au 100m, 14,25 km/h
	/// Si la VMA est inconnue (null ou &lt;= 0), le texte est renvoyé inchangé.
	/// </summary>
	public static class TrainingPaceCalculator
	{
		private static readonly CultureInfo FrCulture = CultureInfo.GetCultureInfo("fr-FR");

		// Capture un pourcentage simple ou une plage, suivi (éventuellement d'espaces) de "VMA".
		// Exemples: "95% VMA", "90-95% VMA", "80%VMA", "90 à 95% VMA".
		// Tolère les espaces HTML (&nbsp;) et les tirets Unicode (–, —) que l'éditeur WYSIWYG insère.
		private const string Ws = @"(?:\s|&nbsp;|\u00A0)*";
		private const string Dash = @"(?:-|–|—|à)";
		private static readonly Regex PercentVmaRegex = new Regex(
			@"(?<low>\d+(?:[.,]\d+)?)" + Ws +
			@"(?:" + Dash + Ws + @"(?<high>\d+(?:[.,]\d+)?)" + Ws + @")?" +
			@"%" + Ws + @"VMA",
			RegexOptions.Compiled | RegexOptions.IgnoreCase);

		/// <summary>
		/// Laisse le contenu inchangé et ajoute à la fin une ligne par pourcentage VMA
		/// distinct (bornes des plages incluses). <paramref name="isHtml"/> contrôle le
		/// séparateur de ligne. Si <paramref name="vma"/> est null ou &lt;= 0, le texte
		/// est renvoyé inchangé.
		/// </summary>
		public static string Resolve(string content, double? vma, bool isHtml = true, DateTime? vmaDate = null, bool isExtrapolated = false)
		{
			if (string.IsNullOrWhiteSpace(content))
				return content;

			if (!vma.HasValue || vma.Value <= 0)
				return content;

			double vmaValue = vma.Value;
			string lineBreak = isHtml ? "<br/>" : Environment.NewLine;

			// Collecte des pourcentages distincts, dans l'ordre d'apparition.
			var matches = PercentVmaRegex.Matches(content);
			if (matches.Count == 0)
				return content;

			var percents = new List<double>();
			foreach (Match match in matches)
			{
				AddPercent(percents, ParsePercent(match.Groups["low"].Value));
				if (match.Groups["high"].Success)
					AddPercent(percents, ParsePercent(match.Groups["high"].Value));
			}

			// Construction du récapitulatif à insérer juste en dessous de l'entraînement.
			var recap = new StringBuilder();
			recap.Append(lineBreak);

			// Ligne avec la VMA de référence (en km/h) et sa date.
			var vmaLabel = isExtrapolated ? "VMA extrapolée" : "VMA testée";
			if (vmaDate.HasValue)
				recap.Append($"{vmaLabel} du {vmaDate.Value.ToString("dd/MM/yyyy", FrCulture)} : {FormatVma(vmaValue)} km/h");
			else
				recap.Append($"{vmaLabel} : {FormatVma(vmaValue)} km/h");

			foreach (var percent in percents)
			{
				recap.Append(lineBreak);
				recap.Append($"{FormatPercent(percent)}% VMA : {FormatCalc(vmaValue, percent)}");
			}

			// Insertion sur la ligne en dessous de la fin de la ligne contenant la dernière
			// mention de pourcentage VMA (fin de l'entraînement).
			var lastMatch = matches[matches.Count - 1];
			int insertionIndex = lastMatch.Index + lastMatch.Length;
			insertionIndex = FindEndOfLine(content, insertionIndex, isHtml);

			return content.Substring(0, insertionIndex) + recap + content.Substring(insertionIndex);
		}

		/// <summary>
		/// Construit un tableau HTML d'allures à partir des pourcentages "% VMA" présents
		/// dans <paramref name="segment"/> (une ligne par pourcentage distinct, bornes des
		/// plages incluses). Renvoie une chaîne vide si la VMA est inconnue ou si aucun
		/// pourcentage n'est présent.
		/// </summary>
		public static string BuildPaceTableHtml(string segment, double? vma, DateTime? vmaDate = null, bool isExtrapolated = false)
		{
			if (string.IsNullOrWhiteSpace(segment) || !vma.HasValue || vma.Value <= 0)
				return string.Empty;

			var matches = PercentVmaRegex.Matches(segment);
			if (matches.Count == 0)
				return string.Empty;

			var percents = new List<double>();
			foreach (Match match in matches)
			{
				AddPercent(percents, ParsePercent(match.Groups["low"].Value));
				if (match.Groups["high"].Success)
					AddPercent(percents, ParsePercent(match.Groups["high"].Value));
			}

			double vmaValue = vma.Value;
			var vmaLabel = isExtrapolated ? "VMA extrapolée" : "VMA testée";
			var caption = vmaDate.HasValue
				? $"{vmaLabel} du {vmaDate.Value.ToString("dd/MM/yyyy", FrCulture)} : {FormatVma(vmaValue)} km/h"
				: $"{vmaLabel} : {FormatVma(vmaValue)} km/h";

			var sb = new StringBuilder();
			sb.Append("<div style='margin: 6px 0 2px 0; font-family: Arial, sans-serif; font-size: 12px;'>");
			sb.Append($"<div style='color: #3949AB; font-weight: bold; margin-bottom: 3px;'>🎯 Tes allures <span style='font-weight: normal; color: #888;'>· {caption}</span></div>");

			foreach (var percent in percents)
			{
				sb.Append("<div style='margin: 1px 0; color: #333;'>");
				sb.Append($"<span style='display: inline-block; min-width: 42px; font-weight: bold; color: #3949AB;'>{FormatPercent(percent)}%</span>");
				sb.Append($"<span>{FormatPacePerKm(vmaValue, percent)} /km · {FormatPacePer100m(vmaValue, percent)} /100m · {FormatSpeed(vmaValue, percent)} km/h</span>");
				sb.Append("</div>");
			}

			sb.Append("</div>");
			return sb.ToString();
		}

		/// <summary>
		/// Renvoie l'index de la fin de la ligne (juste avant le prochain saut de ligne)
		/// contenant la position <paramref name="startIndex"/>. Reconnaît les sauts de
		/// </summary>
		private static int FindEndOfLine(string content, int startIndex, bool isHtml)
		{
			int best = -1;

			void Consider(int idx)
			{
				if (idx >= 0 && (best < 0 || idx < best))
					best = idx;
			}

			Consider(content.IndexOf('\n', startIndex));
			Consider(content.IndexOf('\r', startIndex));

			if (isHtml)
			{
				foreach (var delimiter in new[] { "<br", "</p>", "</div>", "</li>" })
				{
					Consider(content.IndexOf(delimiter, startIndex, StringComparison.OrdinalIgnoreCase));
				}
			}

			return best < 0 ? content.Length : best;
		}

		private static void AddPercent(List<double> percents, double percent)
		{
			foreach (var existing in percents)
			{
				if (Math.Abs(existing - percent) < 0.0001)
					return;
			}
			percents.Add(percent);
		}

		/// <summary>Allure + vitesse, ex: 4'12"6 au km, 25"3 au 100m, 14,25 km/h.</summary>
		private static string FormatCalc(double vma, double percent)
		{
			string perKm = FormatPacePerKm(vma, percent);
			string per100 = FormatPacePer100m(vma, percent);
			string speed = FormatSpeed(vma, percent);
			return $"{perKm} au km, {per100} au 100m, {speed} km/h";
		}

		/// <summary>Allure au kilomètre au format M'SS"d (dixièmes de seconde).</summary>
		public static string FormatPacePerKm(double vma, double percent)
		{
			double speed = vma * percent / 100.0; // km/h
			double secondsPerKm = 3600.0 / speed;

			int totalTenths = (int)Math.Round(secondsPerKm * 10.0, MidpointRounding.AwayFromZero);
			int minutes = totalTenths / 600;
			int remainder = totalTenths % 600;
			int seconds = remainder / 10;
			int tenths = remainder % 10;

			return $"{minutes}'{seconds:D2}\"{tenths}";
		}

		/// <summary>Allure aux 100 mètres au format SS"d (dixièmes de seconde).</summary>
		public static string FormatPacePer100m(double vma, double percent)
		{
			double speed = vma * percent / 100.0; // km/h
			double secondsPer100m = 360.0 / speed;

			int totalTenths = (int)Math.Round(secondsPer100m * 10.0, MidpointRounding.AwayFromZero);
			int seconds = totalTenths / 10;
			int tenths = totalTenths % 10;

			return $"{seconds}\"{tenths}";
		}

		private static string FormatSpeed(double vma, double percent)
		{
			double speed = vma * percent / 100.0;
			return speed.ToString("0.##", FrCulture);
		}

		private static string FormatVma(double vma)
		{
			return vma.ToString("0.##", FrCulture);
		}

		private static double ParsePercent(string raw)
		{
			return double.Parse(raw.Replace(',', '.'), CultureInfo.InvariantCulture);
		}

		private static string FormatPercent(double percent)
		{
			if (Math.Abs(percent - Math.Round(percent)) < 0.0001)
				return ((int)Math.Round(percent)).ToString(CultureInfo.InvariantCulture);

			return percent.ToString("0.##", FrCulture);
		}
	}
}
