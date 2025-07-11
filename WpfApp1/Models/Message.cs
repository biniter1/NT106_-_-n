using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
namespace WpfApp1.Models

{
    public partial class Message : ObservableObject
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

        [JsonProperty("likedBy")]
        [ObservableProperty]
        private Dictionary<string, bool> _likedBy = new Dictionary<string, bool>();

        [ObservableProperty]
        [JsonIgnore] // Không lưu thuộc tính này vào Firebase
        private bool _isPinned;

        partial void OnLikedByChanged(Dictionary<string, bool> value)
        {
            OnPropertyChanged(nameof(LikeCount));
            OnPropertyChanged(nameof(HasLikes));
        }
        
        [JsonIgnore]
        public int LikeCount => LikedBy?.Values.Count(isLiked => isLiked) ?? 0; 

        [JsonIgnore]
        public bool HasLikes => LikeCount > 0;
        public new void OnPropertyChanged(string propertyName)
        {
            base.OnPropertyChanged(propertyName);
        }
    }
}
