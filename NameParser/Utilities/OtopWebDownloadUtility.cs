using System;
using System.Threading.Tasks;
using NameParser.Infrastructure.Services;

namespace NameParser.Utilities
{
	/// <summary>
	/// Command-line utility to download race results from an O'Top / RaceResult web
	/// classification page (https://web.otop.be/presenters/?event=...&amp;n=...&amp;k=...).
	///
	/// Produces one normalized JSON results file per distance/contest.
	/// Usage: OtopWebDownloadUtility.RunDownloader(args)
	/// </summary>
	public class OtopWebDownloadUtility
	{
		public static async Task RunDownloader(string[] args)
		{
			Console.WriteLine("O'Top Web Results Downloader");
			Console.WriteLine("============================\n");

			try
			{
				string url;
				string outputDirectory = null;

				if (args.Length == 0)
				{
					Console.WriteLine("Enter O'Top presenter URL:");
					Console.WriteLine("Example: https://web.otop.be/presenters/?event=CAPSS&n=O'Top%20-%20Web%20-%20Classements%20(Mobile)&k=B4c7I6oz460C");
					Console.Write("> ");
					url = Console.ReadLine()?.Trim();

					if (string.IsNullOrWhiteSpace(url))
					{
						Console.WriteLine("Error: URL cannot be empty");
						return;
					}

					Console.WriteLine("\nEnter output directory (press Enter for current directory):");
					Console.Write("> ");
					outputDirectory = Console.ReadLine()?.Trim();
					if (string.IsNullOrWhiteSpace(outputDirectory))
					{
						outputDirectory = null;
					}
				}
				else
				{
					url = args[0];
					if (args.Length > 1)
					{
						outputDirectory = args[1];
					}
				}

				Console.WriteLine($"\nDownloading results from:");
				Console.WriteLine(url);
				Console.WriteLine();

				var downloader = new OtopWebDownloader();
				var savedFiles = await downloader.DownloadAllDistancesAsync(url, outputDirectory);

				Console.WriteLine($"\n✓ Success! {savedFiles.Count} distance file(s) saved:");
				foreach (var file in savedFiles)
				{
					Console.WriteLine($"  {file}");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"\n✗ Error: {ex.Message}");
				if (ex.InnerException != null)
				{
					Console.WriteLine($"  Details: {ex.InnerException.Message}");
				}
				Console.WriteLine("\nUsage:");
				Console.WriteLine("  Interactive mode: Run without arguments");
				Console.WriteLine("  Command-line mode: OtopWebDownloadUtility <url> [outputDirectory]");
			}
		}
	}
}
