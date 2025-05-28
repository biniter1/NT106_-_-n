using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfApp1.Converters
{
    public class StringToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string selectedLanguage && parameter is string language)
            {
                return selectedLanguage.Equals(language, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked && parameter is string language)
            {
                return language;
            }
            return Binding.DoNothing;
        }
    }
}