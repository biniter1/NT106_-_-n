using System;

namespace WpfApp1.Models
{
    public class Message
    {
        // Nội dung tin nhắn
        public string Content { get; set; }

        // Thời gian gửi/nhận tin nhắn
        public DateTime Timestamp { get; set; }

        // Xác định tin nhắn này có phải do người dùng hiện tại gửi hay không
        // true: tin nhắn gửi đi (hiển thị bên phải)
        // false: tin nhắn nhận được (hiển thị bên trái)
        public bool IsMine { get; set; }

        // Có thể thêm thông tin người gửi nếu là chat nhóm
        // public string SenderName { get; set; }
        // public string SenderAvatarUrl { get; set; }
        // public int SenderId { get; set; }

        // Có thể thêm ID tin nhắn
        // public Guid MessageId { get; set; } // Dùng Guid cho ID duy nhất

        public Message()
        {
            // Gán thời gian hiện tại khi tin nhắn được tạo
            Timestamp = DateTime.Now;
            // Mặc định là tin nhắn nhận được
            IsMine = false;
        }
    }
}