// WpfApp1/Services/NotificationService.cs
using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Threading.Tasks;

namespace WpfApp1.Services
{
    // Model cho một thông báo
    public class Notification
    {
        public string Id { get; set; } // Sẽ lưu key của Firebase
        public string Type { get; set; }
        public string SenderEmail { get; set; }
        public string Message { get; set; }
        public long Timestamp { get; set; }
        public bool IsRead { get; set; }
        public string ReferenceId { get; set; }
    }

    public class NotificationService
    {
        private readonly FirebaseClient _firebaseClient;

        public NotificationService(FirebaseClient firebaseClient)
        {
            _firebaseClient = firebaseClient;
        }

        private string EncodeEmail(string email)
        {
            return email.Replace('.', ',');
        }

        // Gửi một thông báo cho người dùng
        public async Task SendNotificationAsync(string recipientEmail, Notification notification)
        {
            await _firebaseClient
                .Child("Notifications")
                .Child(EncodeEmail(recipientEmail))
                .PostAsync(notification);
        }

        // Lắng nghe các thông báo mới
        public IDisposable ListenForNotifications(string recipientEmail, Action<Notification> onNotificationReceived)
        {
            return _firebaseClient
                .Child("Notifications")
                .Child(EncodeEmail(recipientEmail))
                .OrderBy("Timestamp")
                .AsObservable<Notification>()
                .Subscribe(e =>
                {
                    if (e.Object != null)
                    {
                        // Gán Id cho object để sau này dễ quản lý
                        e.Object.Id = e.Key;
                        onNotificationReceived(e.Object);
                    }
                });
        }

        // Đánh dấu một thông báo là đã đọc
        public async Task MarkAsReadAsync(string recipientEmail, string notificationId)
        {
            await _firebaseClient
                .Child("Notifications")
                .Child(EncodeEmail(recipientEmail))
                .Child(notificationId)
                .Child("IsRead")
                .PutAsync(true);
        }
    }
}