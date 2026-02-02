using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NameParser.UI.Converters
{
    public class RankToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
            {
                return Visibility.Collapsed;
            }

            // Convert both value and parameter to int for comparison
            if (int.TryParse(value.ToString(), out int rank) && 
                int.TryParse(parameter.ToString(), out int targetRank))
            {
                return rank == targetRank ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
