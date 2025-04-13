using System; // Cần cho DateTime nếu bạn dùng

namespace WpfApp1.Models // Đảm bảo namespace khớp với cấu trúc thư mục của bạn
{
    public class Contact
    {
        // URL hoặc đường dẫn đến ảnh đại diện
        public string AvatarUrl { get; set; }

        // Tên của liên hệ
        public string Name { get; set; }

        // Đoạn tin nhắn cuối cùng hiển thị trong danh sách
        public string LastMessage { get; set; }

        // Thời gian của tin nhắn cuối cùng (có thể dùng để sắp xếp hoặc hiển thị)
        // Kiểu DateTime? cho phép giá trị null nếu không có tin nhắn nào
        public DateTime? LastMessageTime { get; set; }

        // Trạng thái online
        public bool IsOnline { get; set; }

        // Có thể thêm ID để định danh duy nhất nếu cần
        // public int Id { get; set; }

        // Constructor (hàm khởi tạo) nếu cần gán giá trị mặc định
        public Contact()
        {
            // Ví dụ: Đặt trạng thái mặc định là offline
            IsOnline = false;
            // Có thể gán giá trị mặc định khác nếu muốn
        }
    }
}