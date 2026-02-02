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
    }
}