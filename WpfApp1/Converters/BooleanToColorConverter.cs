using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WpfApp1.Converters
{
    public class BooleanToColorConverter : IValueConverter
    {
        // Phương thức này sẽ được gọi khi chuyển từ ViewModel (true/false) ra UI (Màu sắc)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Kiểm tra giá trị đầu vào có phải là boolean không
            if (value is not bool isTrue)
            {
                return Brushes.Gray; // Trả về màu mặc định nếu có lỗi
            }

            // Lấy tham số màu từ XAML (ví dụ: "Crimson;Gray")
            if (parameter is not string colorString || string.IsNullOrEmpty(colorString))
            {
                return Brushes.Black; // Trả về màu mặc định nếu không có tham số
            }

            // Tách chuỗi tham số thành hai màu
            string[] colors = colorString.Split(';');
            if (colors.Length < 2)
            {
                return Brushes.Black; // Trả về màu mặc định nếu tham số sai định dạng
            }

            // Nếu giá trị là true, trả về màu đầu tiên. Nếu là false, trả về màu thứ hai.
            string colorToUse = isTrue ? colors[0] : colors[1];

            // Chuyển tên màu (string) thành đối tượng Brush
            return (SolidColorBrush)new BrushConverter().ConvertFrom(colorToUse);
        }

        // Phương thức này không cần dùng đến trong trường hợp này
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}