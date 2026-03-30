using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NameParser.Application.Services;
using NameParser.Domain.Entities;
using NameParser.Domain.Repositories;
using NameParser.Domain.Services;
using NameParser.Infrastructure.Repositories;
using NameParser.Infrastructure.Services;

namespace NameParser.Examples
{
    /// <summary>
    /// Example demonstrating how to use ACN Timing integration to fetch and process race results.
    /// This shows three approaches: direct URL, cached JSON, and cached HTML.
    /// </summary>
    public class AcnTimingIntegrationExample
    {
        public static void RunExamples(string[] args)
        {
            Console.WriteLine("ACN Timing Integration Example");
            Console.WriteLine("===============================\n");

            try
            {
                // Setup repositories and services
                var memberRepository = new JsonMemberRepository("Members.json");
                var members = memberRepository.GetMembersWithLastName();
                var pointsCalculationService = new PointsCalculationService();

                Console.WriteLine($"Loaded {members.Count} members from Members.json\n");

                // Example 1: Direct URL Processing
                Example1_DirectUrlProcessing(members, pointsCalculationService);

                // Example 2: Download and Cache First
                Example2_DownloadAndCache(members, pointsCalculationService);

                // Example 3: Process Multiple Sources
                Example3_ProcessMultipleSources(members, pointsCalculationService);

                // Example 4: Using with RaceProcessingService
                Example4_RaceProcessingService(memberRepository, pointsCalculationService);

                Console.WriteLine("\n✓ All examples completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n✗ Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Example 1: Process results directly from ACN Timing URL
        /// </summary>
        private static void Example1_DirectUrlProcessing(List<Member> members, PointsCalculationService pointsService)
        {
            Console.WriteLine("Example 1: Direct URL Processing");
            Console.WriteLine("---------------------------------");

            string acnUrl = "https://www.acn-timing.com/?lng=FR#/events/2156215316811398/ctx/20260328_spa/generic/198022_3/home/LIVEMARA3";

            Console.WriteLine($"Fetching results from: {acnUrl}");

            var repository = new AcnTimingRaceResultRepository();
            var results = repository.GetRaceResults(acnUrl, members);

            Console.WriteLine($"✓ Fetched {results.Count} rows (including header)");

            // Display first few results
            int displayCount = Math.Min(5, results.Count);
            Console.WriteLine($"\nFirst {displayCount} results:");
            foreach (var result in results.Take(displayCount))
            {
                Console.WriteLine($"  [{result.Key}] {result.Value}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Example 2: Download results first, then process cached file
        /// </summary>
        private static void Example2_DownloadAndCache(List<Member> members, PointsCalculationService pointsService)
        {
            Console.WriteLine("Example 2: Download and Cache");
            Console.WriteLine("------------------------------");

            string acnUrl = "https://www.acn-timing.com/?lng=FR#/events/2156215316811398/ctx/20260328_spa/generic/198022_3/home/LIVEMARA3";

            // Step 1: Download and cache
            Console.WriteLine("Step 1: Downloading results...");
            var downloader = new AcnTimingDownloader();
            string cachedFile = downloader.DownloadRaceResults(acnUrl);
            Console.WriteLine($"✓ Results cached to: {cachedFile}");

            // Step 2: Process cached file
            Console.WriteLine("\nStep 2: Processing cached file...");
            var repository = new AcnTimingRaceResultRepository();
            var results = repository.GetRaceResults(cachedFile, members);
            Console.WriteLine($"✓ Processed {results.Count} rows from cached file");

            // Count members found
            int memberCount = results.Values.Count(r => r.Contains(";True;"));
            Console.WriteLine($"✓ Found {memberCount} club members in results");

            Console.WriteLine();
        }

        /// <summary>
        /// Example 3: Process multiple race sources (PDF, Excel, ACN Timing)
        /// </summary>
        private static void Example3_ProcessMultipleSources(List<Member> members, PointsCalculationService pointsService)
        {
            Console.WriteLine("Example 3: Process Multiple Sources");
            Console.WriteLine("------------------------------------");

            var raceSources = new Dictionary<string, string>
            {
                { "10.12.Angleur.xlsx", "Excel" },
                { "11.15.Herstal.pdf", "PDF" },
                { "https://www.acn-timing.com/?lng=FR#/events/123456/ctx/20260328_spa/generic/789012/home/LIVEMARA3", "ACN URL" },
                { "ACN_20260328_spa_198022_3_20260328_143022.json", "ACN JSON" }
            };

            foreach (var source in raceSources)
            {
                try
                {
                    Console.WriteLine($"\nProcessing: {source.Key} ({source.Value})");

                    IRaceResultRepository repository = GetRepositoryForSource(source.Key);
                    var results = repository.GetRaceResults(source.Key, members);

                    Console.WriteLine($"  ✓ Extracted {results.Count} rows");
                }
                catch (FileNotFoundException)
                {
                    Console.WriteLine($"  ⊘ File not found (skipped)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ Error: {ex.Message}");
                }
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Example 4: Complete race processing workflow with ACN Timing
        /// </summary>
        private static void Example4_RaceProcessingService(IMemberRepository memberRepository, PointsCalculationService pointsService)
        {
            Console.WriteLine("Example 4: Complete Race Processing");
            Console.WriteLine("------------------------------------");

            // Process ACN Timing race
            var acnRepository = new AcnTimingRaceResultRepository();
            var raceProcessingService = new RaceProcessingService(
                memberRepository,
                acnRepository,
                pointsService);

            string acnUrl = "https://www.acn-timing.com/?lng=FR#/events/2156215316811398/ctx/20260328_spa/generic/198022_3/home/LIVEMARA3";

            Console.WriteLine("Processing Marathon de Spa from ACN Timing...");

            var classification = raceProcessingService.ProcessRace(
                acnUrl,
                "Marathon de Spa",
                10,
                2026,
                42,
                acnRepository);

            Console.WriteLine($"✓ Classification generated");

            var allClassifications = classification.GetAllClassifications().ToList();
            Console.WriteLine($"  Total participants: {allClassifications.Count}");

            // Display top performers
            var topResults = allClassifications
                .OrderByDescending(c => c.Points)
                .Take(3);

            Console.WriteLine("\nTop 3 performers:");
            int rank = 1;
            foreach (var result in topResults)
            {
                Console.WriteLine($"  {rank}. {result.Member.FirstName} {result.Member.LastName} - {result.Points} points");
                rank++;
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Helper method to select appropriate repository based on source type
        /// </summary>
        private static IRaceResultRepository GetRepositoryForSource(string source)
        {
            if (source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                source.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return new AcnTimingRaceResultRepository();
            }

            if (source.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                source.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                // ACN Timing cached files
                return new AcnTimingRaceResultRepository();
            }

            if (source.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return new PdfRaceResultRepository();
            }

            if (source.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                source.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
            {
                return new ExcelRaceResultRepository();
            }

            throw new NotSupportedException($"Unsupported file format: {source}");
        }
    }
}
