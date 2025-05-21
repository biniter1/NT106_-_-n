using System;
using System.Globalization;
using System.Windows.Data; // Cần cho IValueConverter

namespace WpfApp1.Converters // Đảm bảo namespace đúng
{
    public class FileTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string fileType = value as string;

            // Sử dụng switch expression (C# 8.0+) cho gọn
            return fileType?.ToLowerInvariant() switch // Chuyển về chữ thường để không phân biệt hoa/thường
            {
                "pdf" => "\uea90",
                "png" or "jpg" or "jpeg" or "gif" or "bmp" => "\uEB9F", // Icon Picture
                "docx" or "doc" => "\uE8A5", // Icon Document Word
                "xlsx" or "xls" => "\ue9f9", 
                "pptx" or "ppt" => "\ueb3b",
                "txt" => "\uE7C3", // Icon Document
                "zip" or "rar" => "\uE7B8", // Icon Package
                // Thêm các loại file khác nếu cần
                _ => "\uE7C3", // Icon Document mặc định cho các loại không xác định
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing; // Không cần chuyển ngược
        }
    }
}