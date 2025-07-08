using System;

namespace WpfApp1.Models
{
    public class Message
    {
        //ID MESSAGE
        public string Id { get; set; }
        // ID người gửi
        public string SenderId { get; set; }

        // Tên hiển thị của người gửi
        public string SenderDisplayName { get; set; }

        // Nội dung tin nhắn
        public string Content { get; set; }

        // Thời gian gửi tin nhắn (UTC để đồng bộ nhiều máy)
        public DateTime Timestamp { get; set; }

        // Cờ xác định tin nhắn là của người dùng hiện tại
        public bool IsMine { get; set; }

        public string Alignment { get; set; }
        public bool IsImage { get; set; }
        public bool IsVideo { get; set; }
        public string ImageUrl { get; set; }
        public string VideoUrl { get; set; }
        public string FileUrl { get; set; }
        public bool IsVoiceMessage { get; set; } = false;
        public string VoiceMessageUrl { get; set; }
        public double VoiceMessageDuration { get; set; }
        public bool IsReply { get; set; } = false;
        public string ReplyToMessageId { get; set; }
        public string ReplyToMessageContent { get; set; }
        public string ReplyToSenderName { get; set; }

        public bool IsSystemMessage { get; set; } = false;
        public Message()
        {
            // Gán mặc định
            Timestamp = DateTime.UtcNow;
            IsMine = false;
        }
    }
}
