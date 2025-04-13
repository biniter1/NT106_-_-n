namespace WpfApp1.Models
{
    public class FileItem
    {
        // Đường dẫn đến icon đại diện cho loại file (ví dụ: icon PDF, Word, ảnh)
        // Hoặc có thể chỉ là một string để xác định loại file rồi chọn icon trong XAML
        public string IconPathOrType { get; set; }

        // Tên file hiển thị
        public string FileName { get; set; }

        // Thông tin bổ sung (ví dụ: "PDF - 1.2MB", "PNG - 450KB")
        public string FileInfo { get; set; }

        // Đường dẫn thực tế đến file hoặc URL để tải về (nếu cần)
        public string FilePathOrUrl { get; set; }

        // Có thể thêm ID
        // public int Id { get; set; }

        public FileItem()
        {
            // Gán giá trị mặc định nếu cần
        }
    }
}