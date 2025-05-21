using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WpfApp1.Converters
{
    public class MessageBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isMine = (bool)value;
            return isMine ? Brushes.SkyBlue : Brushes.LightGray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
