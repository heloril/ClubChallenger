using System;
using System.Threading.Tasks;
using NameParser.Infrastructure.Services;

namespace NameParser.Utilities
{
    /// <summary>
    /// Command-line utility to download race results from ACN Timing website.
    /// Usage: Call RunDownloader method with URL and optional output path
    /// </summary>
    public class AcnTimingDownloadUtility
    {
        public static async Task RunDownloader(string[] args)
        {
            Console.WriteLine("ACN Timing Results Downloader");
            Console.WriteLine("=============================\n");

            try
            {
                string url;
                string outputPath = null;

                if (args.Length == 0)
                {
                    // Interactive mode
                    Console.WriteLine("Enter ACN Timing race results URL:");
                    Console.WriteLine("Example: https://www.acn-timing.com/?lng=FR#/events/2156215316811398/ctx/20260328_spa/generic/198022_3/home/LIVEMARA3");
                    Console.Write("> ");
                    url = Console.ReadLine()?.Trim();

                    if (string.IsNullOrWhiteSpace(url))
                    {
                        Console.WriteLine("Error: URL cannot be empty");
                        return;
                    }

                    Console.WriteLine("\nEnter output file path (press Enter for auto-generated name):");
                    Console.Write("> ");
                    outputPath = Console.ReadLine()?.Trim();
                    if (string.IsNullOrWhiteSpace(outputPath))
                    {
                        outputPath = null;
                    }
                }
                else
                {
                    // Command-line mode
                    url = args[0];
                    if (args.Length > 1)
                    {
                        outputPath = args[1];
                    }
                }

                Console.WriteLine($"\nDownloading results from:");
                Console.WriteLine(url);
                Console.WriteLine();

                var downloader = new AcnTimingDownloader();
                var savedPath = await downloader.DownloadRaceResultsAsync(url, outputPath);

                Console.WriteLine($"\n✓ Success! Results saved to:");
                Console.WriteLine(savedPath);
                Console.WriteLine("\nYou can now use this file with the race processing system:");
                Console.WriteLine($"  - Use AcnTimingRaceResultRepository.GetRaceResults(\"{savedPath}\", members)");
                Console.WriteLine($"  - Or process directly with RaceProcessingService");
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
                Console.WriteLine("  Command-line mode: AcnTimingDownloadUtility.exe <url> [outputPath]");
                Console.WriteLine("\nExample:");
                Console.WriteLine("  AcnTimingDownloadUtility.exe \"https://www.acn-timing.com/?lng=FR#/events/2156215316811398/ctx/20260328_spa/generic/198022_3/home/LIVEMARA3\"");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
