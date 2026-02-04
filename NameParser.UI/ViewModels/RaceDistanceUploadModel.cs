using NameParser.Infrastructure.Data.Models;

namespace NameParser.UI.ViewModels
{
    /// <summary>
    /// Represents a race distance with its associated file upload information
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
                }
            }
        }

        public string FileName => string.IsNullOrEmpty(FilePath) 
            ? "No file selected" 
            : System.IO.Path.GetFileName(FilePath);

        public bool HasFile
        {
            get => _hasFile;
            private set => SetProperty(ref _hasFile, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }
    }
}
