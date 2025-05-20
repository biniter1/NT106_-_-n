using System; // Cần cho DateTime nếu bạn dùng
using Google.Cloud.Firestore;

namespace WpfApp1.Models // Đảm bảo namespace khớp với cấu trúc thư mục của bạn
{

    [FirestoreData]
    public class Contact
    {
        public Contact()
        {
            IsOnline = false;
        }

        [FirestoreProperty]
        public string AvatarUrl { get; set; }

        [FirestoreProperty]
        public string Name { get; set; }

        [FirestoreProperty]
        public string Email { get; set; }

        [FirestoreProperty]
        public string LastMessage { get; set; }

        [FirestoreProperty]
        public DateTime? LastMessageTime { get; set; }

        [FirestoreProperty]
        public bool IsOnline { get; set; }

        [FirestoreProperty]
        public string chatID { get; set; }

    }
}