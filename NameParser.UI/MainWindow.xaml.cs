using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NameParser.UI.ViewModels;

namespace NameParser.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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
