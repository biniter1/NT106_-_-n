using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfApp1.Converters
{
    public class UtcToLocalTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime utcDate)
            {                
                DateTime localDate = utcDate.ToLocalTime();
                DateTime now = DateTime.Now;
                                
                if (localDate.Date == now.Date)
                {
                    // Nếu tin nhắn trong ngày hôm nay -> Chỉ hiện giờ:phút
                    return localDate.ToString("HH:mm");
                }
                else if (localDate.Date == now.Date.AddDays(-1))
                {
                    // Nếu tin nhắn của ngày hôm qua -> Hiện "Hôm qua, HH:mm"
                    return $"Hôm qua, {localDate:HH:mm}";
                }
                else
                {
                    // Nếu tin nhắn cũ hơn -> Hiện ngày/tháng/năm
                    return localDate.ToString("dd/MM/yyyy");
                }
            }
                       
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {            
            throw new NotImplementedException();
        }
    }
}