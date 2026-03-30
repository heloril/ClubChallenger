using NameParser.Infrastructure.Data.Models;

namespace NameParser.UI.ViewModels
{
    /// <summary>
    /// Represents a race distance with its associated file upload or URL information
    /// </summary>
    public class RaceDistanceUploadModel : ViewModelBase
    {
        private string _filePath;
        private bool _hasFile;
        private string _statusMessage;

        public RaceDistanceUploadModel(RaceEventDistanceEntity distance)
        {
            Distance = distance;
        }

        public RaceEventDistanceEntity Distance { get; }

        public decimal DistanceKm => Distance.DistanceKm;

        public string FilePath
        {
            get => _filePath;
            set
            {
                if (SetProperty(ref _filePath, value))
                {
                    HasFile = !string.IsNullOrEmpty(value);
                    OnPropertyChanged(nameof(FileName));
                    OnPropertyChanged(nameof(IsUrl));
                    OnPropertyChanged(nameof(SourceType));
                }
            }
        }

        public string FileName
        {
            get
            {
                if (string.IsNullOrEmpty(FilePath))
                    return "No file or URL selected";

                if (IsUrl)
                    return "ACN Timing URL";

                return System.IO.Path.GetFileName(FilePath);
            }
        }

        public bool HasFile
        {
            get => _hasFile;
            private set => SetProperty(ref _hasFile, value);
        }

        public bool IsUrl => !string.IsNullOrEmpty(FilePath) && 
                            (FilePath.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase) || 
                             FilePath.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase));

        public string SourceType
        {
            get
            {
                if (string.IsNullOrEmpty(FilePath))
                    return "";
                if (IsUrl)
                    return "🌐 URL";
                var ext = System.IO.Path.GetExtension(FilePath).ToUpperInvariant();
                return ext == ".PDF" ? "📄 PDF" : ext == ".XLSX" ? "📊 Excel" : "📁 File";
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }
    }
}
