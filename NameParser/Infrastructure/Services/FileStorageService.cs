using System;
using System.IO;

namespace NameParser.Infrastructure.Services
{
    public class FileStorageService
    {
        /// <summary>
        /// Reads a file and returns its binary content along with metadata
        /// </summary>
        public (byte[] content, string fileName, string extension) ReadRaceFile(string sourceFilePath)
        {
            if (!File.Exists(sourceFilePath))
            {
                throw new FileNotFoundException($"Source file not found: {sourceFilePath}");
            }

            // Read file content as bytes
            var fileContent = File.ReadAllBytes(sourceFilePath);

            // Get file metadata
            var fileName = Path.GetFileName(sourceFilePath);
            var extension = Path.GetExtension(sourceFilePath);

            return (fileContent, fileName, extension);
        }

        /// <summary>
        /// Writes binary content to a temporary file for processing
        /// </summary>
        public string WriteToTempFile(byte[] fileContent, string fileName)
        {
            if (fileContent == null || fileContent.Length == 0)
            {
                throw new ArgumentException("File content cannot be null or empty", nameof(fileContent));
            }

            // Create temp directory if needed
            var tempDirectory = Path.Combine(Path.GetTempPath(), "RaceProcessing");
            if (!Directory.Exists(tempDirectory))
            {
                Directory.CreateDirectory(tempDirectory);
            }

            // Generate unique temp file path
            var tempFilePath = Path.Combine(tempDirectory, $"{Guid.NewGuid()}_{fileName}");

            // Write bytes to temp file
            File.WriteAllBytes(tempFilePath, fileContent);

            return tempFilePath;
        }

        /// <summary>
        /// Deletes a temporary file
        /// </summary>
        public void DeleteTempFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch
                {
                    // Ignore errors when deleting temp files
                }
            }
        }

        /// <summary>
        /// Checks if a file exists
        /// </summary>
        public bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }
    }
}
