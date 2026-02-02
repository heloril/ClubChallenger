using System;
using System.Globalization;
using System.Windows.Data;

namespace NameParser.UI.Converters
{
    public class TimeSpanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan timeSpan)
            {
                // Format as hh:mm:ss or mm:ss depending on duration
                if (timeSpan.TotalHours >= 1)
                {
                    return timeSpan.ToString(@"h\:mm\:ss");
                }
                else
                {
                    return timeSpan.ToString(@"mm\:ss");
                }
            }
            
            // Handle nullable TimeSpan
            if (value == null)
            {
                return "-";
            }
            
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue && !string.IsNullOrWhiteSpace(strValue) && strValue != "-")
            {
                if (TimeSpan.TryParse(strValue, culture, out var result))
                {
                    return result;
                }
            }
            return null;
        }
    }
}
