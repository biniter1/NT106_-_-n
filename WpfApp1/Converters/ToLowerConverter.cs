// Converters/ToLowerConverter.cs
using System;
using System.Globalization;
using System.Windows; // Thêm using này nếu bạn trả về DependencyProperty.UnsetValue
using System.Windows.Data;

namespace WpfApp1.Converters
{
    public class ToLowerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue)
            {
                // Nếu đúng, chuyển sang chữ thường và trả về
                return strValue.ToLowerInvariant();
            }

            // Nếu value là null hoặc không phải string, trả về chuỗi rỗng
            // Điều này an toàn hơn cho các DataTrigger so sánh giá trị.
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}