using System; // Cần cho DateTime

namespace WpfApp1.Models // Đảm bảo namespace là thư mục Models của bạn
{
    /// <summary>
    /// Lớp Model đại diện cho dữ liệu của một lời mời kết bạn.
    /// Chỉ chứa các thuộc tính dữ liệu thuần túy.
    /// </summary>
    public class FriendRequest
    {
        /// <summary>
        /// ID duy nhất của chính lời mời kết bạn này.
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// ID của người đã gửi lời mời kết bạn.
        /// </summary>
        public string RequesterId { get; set; }

        /// <summary>
        /// Tên hiển thị của người gửi lời mời.
        /// </summary>
        public string RequesterName { get; set; }

        /// <summary>
        /// URL hoặc đường dẫn đến ảnh đại diện của người gửi lời mời.
        /// </summary>
        public string RequesterAvatarUrl { get; set; }

        /// <summary>
        /// Thời điểm lời mời được gửi.
        /// </summary>
        public DateTime RequestTime { get; set; }

        // Bạn có thể thêm các thuộc tính khác nếu cần, ví dụ:
        // public string Message { get; set; } // Lời nhắn kèm theo (nếu có)
        // public RequestStatus Status { get; set; } // Trạng thái của lời mời (Pending, Accepted, Declined) - nếu cần quản lý ở đây

        // Constructor có thể có hoặc không tùy nhu cầu
        public FriendRequest()
        {
            RequestTime = DateTime.Now;
        }
    }
}