using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace WpfApp1.Converters
{
    public class AvatarUrlConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path && !string.IsNullOrEmpty(path))
            {
                try
                {
                    // Handle file:// or local paths
                    return new BitmapImage(new Uri(path, UriKind.Absolute));
                }
                catch
                {
                    // Return default image if path is invalid
                    return new BitmapImage(new Uri("pack://application:,,,/Assets/DefaultAvatar.png"));
                }
            }
            // Return default image if path is null
            return new BitmapImage(new Uri("pack://application:,,,/Assets/DefaultAvatar.png"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
