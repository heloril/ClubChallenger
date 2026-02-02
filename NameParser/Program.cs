using System;
using System.IO;
using NameParser.Application.Services;
using NameParser.Domain.Services;
using NameParser.Infrastructure.Repositories;
using NameParser.Infrastructure.Services;

namespace NameParser.Presentation
{
    internal class Program
    {
        private const string ResultFileName = "result.txt";

        private static void Main(string[] args)
        {
            try
            {
                var configuration = ParseArguments(args);

                var memberRepository = new JsonMemberRepository(configuration.MembersFileName);
                var raceResultRepository = new ExcelRaceResultRepository();
                var pointsCalculationService = new PointsCalculationService();

                var raceProcessingService = new RaceProcessingService(
                    memberRepository,
                    raceResultRepository,
                    pointsCalculationService);

                var reportGenerationService = new ReportGenerationService(memberRepository);
                var fileOutputService = new FileOutputService();

                var raceFiles = Directory.GetFiles(configuration.Path, "*.xlsx");

                Console.WriteLine($"Processing {raceFiles.Length} race files from '{configuration.Path}'...");

                var classification = raceProcessingService.ProcessAllRaces(raceFiles);

                var report = reportGenerationService.GenerateReport(classification);

                fileOutputService.WriteToFile(ResultFileName, report);

                Console.WriteLine($"\nReport generated successfully: {ResultFileName}");
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.ReadKey();
            }
        }

        private static Configuration ParseArguments(string[] args)
        {
            var config = new Configuration
            {
                Path = ".",
                MembersFileName = "Members.json"
            };

            if (args.Length > 0)
                config.Path = args[0];

            if (args.Length > 1)
                config.MembersFileName = args[1];

            return config;
        }

        private class Configuration
        {
            public string Path { get; set; }
            public string MembersFileName { get; set; }
        }
    }
}