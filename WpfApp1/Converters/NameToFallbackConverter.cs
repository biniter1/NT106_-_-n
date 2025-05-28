using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace WpfApp1.Converters
{
    public class NameToFallbackConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(value?.ToString()))
            {
                if (Application.Current.Resources["LocalizationDictionary"] is ResourceDictionary localizationDict &&
                    localizationDict.Contains("SelectContactPrompt"))
                {
                    return localizationDict["SelectContactPrompt"];
                }
                return "Select a contact or group to view details"; // Fallback mặc định
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}