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

            // Sanitize fileName to remove invalid file system characters
            // This handles cases where fileName might be a URL or contain special characters
            var sanitizedFileName = SanitizeFileName(fileName);

            // Generate unique temp file path
            var tempFilePath = Path.Combine(tempDirectory, $"{Guid.NewGuid()}_{sanitizedFileName}");

            // Write bytes to temp file
            File.WriteAllBytes(tempFilePath, fileContent);

            return tempFilePath;
        }

        /// <summary>
        /// Sanitizes a filename by removing or replacing invalid file system characters
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return "temp_file";
            }

            // Remove or replace invalid characters for Windows file systems
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = fileName;

            foreach (var c in invalidChars)
            {
                sanitized = sanitized.Replace(c, '_');
            }

            // Additional cleanup for common URL characters that might not be in GetInvalidFileNameChars
            sanitized = sanitized.Replace(":", "_")
                                 .Replace("?", "_")
                                 .Replace("&", "_")
                                 .Replace("=", "_")
                                 .Replace("/", "_")
                                 .Replace("\\", "_");

            // Limit filename length to avoid path too long errors (max 200 chars)
            if (sanitized.Length > 200)
            {
                var extension = Path.GetExtension(sanitized);
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized);
                sanitized = nameWithoutExtension.Substring(0, Math.Min(nameWithoutExtension.Length, 200 - extension.Length)) + extension;
            }

            return sanitized;
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
