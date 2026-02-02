using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Threading;

namespace NameParser.UI.Services
{
    public class LocalizationService : INotifyPropertyChanged
    {
        private static LocalizationService _instance;
        private readonly ResourceManager _resourceManager;
        private CultureInfo _currentCulture;

        public static LocalizationService Instance => _instance ??= new LocalizationService();

        public event PropertyChangedEventHandler PropertyChanged;

        private LocalizationService()
        {
            _resourceManager = new ResourceManager("NameParser.UI.Resources.Strings", typeof(LocalizationService).Assembly);
            _currentCulture = Thread.CurrentThread.CurrentUICulture;
        }

        public CultureInfo CurrentCulture
        {
            get => _currentCulture;
            set
            {
                if (!Equals(_currentCulture, value))
                {
                    _currentCulture = value;
                    Thread.CurrentThread.CurrentUICulture = value;
                    Thread.CurrentThread.CurrentCulture = value;
                    OnPropertyChanged(nameof(CurrentCulture));
                    OnPropertyChanged("Item[]"); // Notify all indexed properties
                }
            }
        }

        public string this[string key]
        {
            get
            {
                if (string.IsNullOrEmpty(key))
                    return string.Empty;

                var value = _resourceManager.GetString(key, _currentCulture);
                return value ?? $"[{key}]";
            }
        }

        public void SetLanguage(string languageCode)
        {
            CurrentCulture = new CultureInfo(languageCode);
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
