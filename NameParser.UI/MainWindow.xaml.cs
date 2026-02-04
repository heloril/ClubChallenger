using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using NameParser.UI.ViewModels;
using NameParser.UI.Converters;

namespace NameParser.UI
{
    public partial class MainWindow : Window
    {
        private bool _isUpdatingEmailBody;

        public MainWindow()
        {
            InitializeComponent();

            // Wire up RichTextBox for HTML content
            if (DataContext is MainViewModel viewModel)
            {
                Loaded += (s, e) =>
                {
                    // Configure RichTextBox for HTML content
                    if (EmailBodyRichTextBox != null)
                    {
                        // Listen for changes in EmailBody (when template is generated)
                        viewModel.ChallengeMailingViewModel.PropertyChanged += (sender, args) =>
                        {
                            if (args.PropertyName == nameof(viewModel.ChallengeMailingViewModel.EmailBody))
                            {
                                if (_isUpdatingEmailBody) return; // Prevent recursion

                                var html = viewModel.ChallengeMailingViewModel.EmailBody;
                                if (!string.IsNullOrWhiteSpace(html))
                                {
                                    _isUpdatingEmailBody = true;
                                    try
                                    {
                                        // Convert HTML to FlowDocument
                                        var flowDoc = HtmlToFlowDocumentConverter.Convert(html);
                                        EmailBodyRichTextBox.Document = flowDoc;
                                    }
                                    finally
                                    {
                                        _isUpdatingEmailBody = false;
                                    }
                                }
                            }
                        };

                        // Listen for changes in RichTextBox (when user edits)
                        EmailBodyRichTextBox.TextChanged += (sender, args) =>
                        {
                            if (_isUpdatingEmailBody) return; // Prevent recursion

                            _isUpdatingEmailBody = true;
                            try
                            {
                                // Convert FlowDocument back to HTML
                                var html = HtmlToFlowDocumentConverter.FlowDocumentToHtml(EmailBodyRichTextBox.Document);
                                viewModel.ChallengeMailingViewModel.EmailBody = html;
                            }
                            finally
                            {
                                _isUpdatingEmailBody = false;
                            }
                        };
                    }
                };
            }
        }

        private void RacesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                var dataGrid = sender as DataGrid;
                viewModel.SelectedRaces = dataGrid?.SelectedItems.Cast<object>().ToList();
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            // Open the context menu when the button is clicked
            if (sender is Button button && button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }

        private void ChallengerExportButton_Click(object sender, RoutedEventArgs e)
        {
            // Open the context menu when the button is clicked
            if (sender is Button button && button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }
    }
}
