using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NameParser.UI.Converters
{
	/// <summary>
	/// Returns <see cref="Visibility.Visible"/> when the bound value is not null
	/// (and, for strings, not empty), otherwise <see cref="Visibility.Collapsed"/>.
	/// </summary>
	public class NullToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var hasValue = value != null && !(value is string s && string.IsNullOrEmpty(s));
			return hasValue ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
