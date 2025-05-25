using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfApp1.Converters
{
    public class LanguageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value is the SelectedLanguage (e.g., "Tiếng Việt" or "English")
            // parameter is the language to compare against (e.g., "Tiếng Việt" or "English")
            string selectedLanguage = value as string;
            string targetLanguage = parameter as string;

            // Return true if the selected language matches the target language
            return selectedLanguage == targetLanguage;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value is the IsChecked state of the RadioButton (true/false)
            // parameter is the language associated with the RadioButton (e.g., "Tiếng Việt" or "English")
            bool isChecked = (bool)value;
            string targetLanguage = parameter as string;

            // If the RadioButton is checked, return the associated language; otherwise, return null
            return isChecked ? targetLanguage : null;
        }
    }
}