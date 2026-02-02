using System.IO;

namespace NameParser.Domain.ValueObjects
{
    public class RaceFileName
    {
        public int RaceNumber { get; private set; }
        public int DistanceKm { get; private set; }
        public string RaceName { get; private set; }
        public string FilePath { get; private set; }

        public RaceFileName(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            FilePath = filePath;
            ParseFileName(Path.GetFileNameWithoutExtension(filePath));
        }

        private void ParseFileName(string fileName)
        {
            var parts = fileName.Split('.');

            if (parts.Length >= 3)
            {
                if (int.TryParse(parts[0], out var raceNum))
                    RaceNumber = raceNum;

                if (int.TryParse(parts[1], out var kms))
                    DistanceKm = kms;
                else
                    DistanceKm = 10;

                RaceName = parts[2];
            }
            else
            {
                RaceNumber = 0;
                DistanceKm = 10;
                RaceName = fileName;
            }
        }
    }
}
