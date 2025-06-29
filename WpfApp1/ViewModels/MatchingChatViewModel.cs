using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApp1.ViewModels
{
    public partial class MatchingChatViewModel : ObservableObject
    {
        public event Action<string, string> MatchFound;

        private readonly FirebaseClient _firebaseClient;
        private readonly string _currentUserEmail;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotSearching))]
        private bool _isSearching;

        [ObservableProperty]
        private string _statusText = "Sẵn sàng kết nối với người lạ!";

        public bool IsNotSearching => !IsSearching;

        private IDisposable _opponentListener;

        public MatchingChatViewModel(FirebaseClient firebaseClient, string currentUserEmail)
        {
            _firebaseClient = firebaseClient;
            _currentUserEmail = currentUserEmail;
        }

        private string EncodeEmail(string email)
        {
            return email.Replace('.', ',');
        }

        [RelayCommand]
        private async Task StartSearchAsync()
        {
            if (IsSearching) return;
            IsSearching = true;
            StatusText = "Đang tìm kiếm đối phương...";

            try
            {
                // 1. Tìm xem có ai trong hàng chờ không (lấy về một object WaitingUser)
                var waitingUsers = await _firebaseClient.Child("WaitingPool").OnceAsync<WaitingUser>();
                var opponent = waitingUsers.FirstOrDefault(u => u.Object.Email != _currentUserEmail);

                if (opponent == null)
                {
                    // 2a. Không có ai, thêm mình vào hàng chờ
                    // **SỬA LỖI:** Lưu một object thay vì một chuỗi
                    var userToWait = new WaitingUser { Email = _currentUserEmail };
                    await _firebaseClient.Child("WaitingPool").Child(EncodeEmail(_currentUserEmail)).PutAsync(userToWait);

                    StatusText = "Đang chờ người khác kết nối... Vui lòng không tắt ứng dụng.";
                    ListenForInvitation();
                }
                else
                {
                    // 2b. Tìm thấy đối phương!
                    StatusText = "Đã tìm thấy! Đang tạo phòng...";

                    // Xóa cả 2 khỏi hàng chờ (dùng key là email đã mã hóa)
                    await _firebaseClient.Child("WaitingPool").Child(EncodeEmail(_currentUserEmail)).DeleteAsync();
                    await _firebaseClient.Child("WaitingPool").Child(opponent.Key).DeleteAsync();

                    // Tạo phòng chat mới với email gốc
                    var newRoom = await _firebaseClient.Child("ChatRooms").PostAsync(new
                    {
                        Participant1 = _currentUserEmail,
                        Participant2 = opponent.Object.Email, // Lấy email từ object
                        CreatedAt = DateTime.UtcNow
                    });

                    // Thông báo cho MainViewModel
                    MatchFound?.Invoke(newRoom.Key, opponent.Object.Email);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Đã xảy ra lỗi: {ex.Message}");
                await CancelSearchAsync();
            }
        }

        [RelayCommand]
        private async Task CancelSearchAsync()
        {
            if (!IsSearching && _opponentListener == null) return;

            _opponentListener?.Dispose();
            _opponentListener = null;

            await _firebaseClient.Child("WaitingPool").Child(EncodeEmail(_currentUserEmail)).DeleteAsync();
            IsSearching = false;
            StatusText = "Đã hủy tìm kiếm.";
        }

        private void ListenForInvitation()
        {
            _opponentListener = _firebaseClient
                .Child("ChatRooms")
                .OrderBy("Participant2")
                .EqualTo(_currentUserEmail)
                .AsObservable<ChatRoom>()
                .Subscribe(e => {
                    if (e.Object != null && e.Key != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MatchFound?.Invoke(e.Key, e.Object.Participant1);
                            CancelSearchAsync();
                        });
                    }
                });
        }

        // --- CÁC LỚP PHỤ ĐỂ PARSE DỮ LIỆU TỪ FIREBASE ---
        public class ChatRoom
        {
            public string Participant1 { get; set; }
            public string Participant2 { get; set; }
        }

        public class WaitingUser
        {
            public string Email { get; set; }
        }
    }
}