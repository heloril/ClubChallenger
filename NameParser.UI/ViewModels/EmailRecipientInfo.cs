using System;
using System.ComponentModel;

namespace NameParser.UI.ViewModels
{
    public class EmailRecipientInfo : INotifyPropertyChanged
    {
        private string _email;
        private string _name;
        private string _status;
        private DateTime? _lastSentDate;
        private string _lastError;
        private bool _isSending;

        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanged(nameof(Email));
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusIcon));
            }
        }

        public DateTime? LastSentDate
        {
            get => _lastSentDate;
            set
            {
                _lastSentDate = value;
                OnPropertyChanged(nameof(LastSentDate));
                OnPropertyChanged(nameof(LastSentDateDisplay));
            }
        }

        public string LastError
        {
            get => _lastError;
            set
            {
                _lastError = value;
                OnPropertyChanged(nameof(LastError));
            }
        }

        public bool IsSending
        {
            get => _isSending;
            set
            {
                _isSending = value;
                OnPropertyChanged(nameof(IsSending));
            }
        }

        public string StatusIcon
        {
            get
            {
                return Status switch
                {
                    "Sent" => "✅",
                    "Failed" => "❌",
                    "Pending" => "⏳",
                    "Sending" => "📤",
                    _ => "❓"
                };
            }
        }

        public string LastSentDateDisplay => LastSentDate.HasValue 
            ? LastSentDate.Value.ToString("dd/MM/yyyy HH:mm") 
            : "Never";

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
