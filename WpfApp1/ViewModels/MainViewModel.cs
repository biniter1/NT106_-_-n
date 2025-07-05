using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using WpfApp1.Models;
using WpfApp1.Services;
using WpfApp1.ViewModels;
using WpfApp1.Views;

namespace WpfApp1.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // Thuộc tính quan trọng: Giữ ViewModel của View đang hiển thị trong ContentControl
        [ObservableProperty]
        private ObservableObject _currentViewModel;

        private readonly FirebaseClient _firebaseClient;

        // Giữ các instance của ViewModel con để tái sử dụng
        public ChatViewModel ChatVm { get; private set; }
        public SettingsViewModel SettingsVm { get; private set; }
        public MatchingChatViewModel MatchingChatVm { get; }
        public FriendListViewModel FriendListVm { get; private set; }
        public AIChatViewModel AIChatVm { get; }
        public class IncomingCallEventArgs : EventArgs
        {
            public CallSignal Call { get; }
            public IncomingCallEventArgs(CallSignal call) { Call = call; }
        }
        private IDisposable _callListener;

        private readonly NotificationService _notificationService;
        private IDisposable _notificationListener;

        [ObservableProperty] private bool _isNotificationPopupVisible;
        [ObservableProperty] private string _notificationMessage;
        [ObservableProperty] private int _unreadNotificationCount;
        [ObservableProperty] private bool _isAIChatVisible;
        public ObservableCollection<Notification> AllNotifications { get; } = new();

        // THÊM 1: KHAI BÁO EVENT ĐỂ MAINWINDOW LẮNG NGHE
        public event EventHandler<NewMessageEventArgs> ShowNotificationRequested;
        public event EventHandler<IncomingCallEventArgs> IncomingCallReceived;
        public MainViewModel(FirebaseClient firebaseClient)
        {

            _firebaseClient = firebaseClient;

            // Khởi tạo các ViewModel con một lần duy nhất
            ChatVm = new ChatViewModel(_firebaseClient);
            SettingsVm = new SettingsViewModel(ChatVm, _firebaseClient);
            FriendListVm = new FriendListViewModel();

            string currentEmail = SharedData.Instance.userdata.Email;
            string safeKey = currentEmail.Replace(".", "_");
            MatchingChatVm = new MatchingChatViewModel(_firebaseClient, safeKey);
            MatchingChatVm.MatchFound += OnMatchFound;

            AIChatVm = new AIChatViewModel();
            IsAIChatVisible = false; // Mặc định ẩn

            // Đặt View mặc định
            CurrentViewModel = ChatVm;

            // Hệ thống notification có sẵn của bạn
            _notificationService = new NotificationService(firebaseClient);
            _notificationListener = _notificationService.ListenForNotifications(safeKey, (notification) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (!notification.IsRead && !AllNotifications.Any(n => n.Id == notification.Id))
                    {
                        AllNotifications.Insert(0, notification);
                        UnreadNotificationCount = AllNotifications.Count(n => !n.IsRead);
                        NotificationMessage = notification.Message;
                        IsNotificationPopupVisible = true;
                    }
                });
            });

            // Đăng ký lắng nghe sự kiện yêu cầu mở chat
            EventAggregator.Instance.Subscribe<StartChatEvent>(OnStartChatReceived);

            // THÊM 2: ĐĂNG KÝ LẮNG NGHE SỰ KIỆN TIN NHẮN MỚI TỪ CHATVIEWMODEL
            if (ChatVm != null)
            {
                ChatVm.NewMessageNotificationRequested += OnNewMessageNotificationFromChat;

            }
            ListenForIncomingCalls();
        }
        private void ListenForIncomingCalls()
        {
            var currentUser = SharedData.Instance.userdata;
            if (currentUser == null || string.IsNullOrEmpty(currentUser.Email)) return;

            string safeEmail = currentUser.Email.Replace('.', '_');

            _callListener = _firebaseClient
                .Child("calls")
                .Child(safeEmail)
                .AsObservable<CallSignal>()
                .Subscribe(callEvent =>
                {
                    if (callEvent.EventType == FirebaseEventType.InsertOrUpdate && callEvent.Object != null)
                    {
                        var call = callEvent.Object;
                        // Chỉ xử lý các cuộc gọi mới đang ở trạng thái "Ringing"
                        if (call.Status == "Ringing")
                        {
                            Debug.WriteLine($"Incoming call received from {call.CallerName}. Call ID: {call.CallId}");
                            // Phát event để MainWindow xử lý (hiển thị popup)
                            IncomingCallReceived?.Invoke(this, new IncomingCallEventArgs(call));
                        }
                    }
                }, ex =>
                {
                    Debug.WriteLine($"Error listening for calls: {ex.Message}");
                });
        }
        public void OpenChat(string chatID)
        {
            if (string.IsNullOrEmpty(chatID)) return;

            Debug.WriteLine($"MainViewModel: Received request to open chat for ID: {chatID}");

            // Tìm contact tương ứng trong ChatViewModel
            var contactToOpen = ChatVm?.Contacts.FirstOrDefault(c => c.chatID == chatID);

            if (contactToOpen != null)
            {
                Debug.WriteLine($"Found contact: {contactToOpen.Name}. Initiating chat.");

                // Sử dụng phương thức đã có sẵn trong ChatViewModel để chọn contact
                ChatVm.InitiateChatWith(contactToOpen);

                // Quan trọng: Đảm bảo giao diện chính đang hiển thị ChatView
                CurrentViewModel = ChatVm;
            }
            else
            {
                Debug.WriteLine($"Contact with ChatID {chatID} not found in the current contact list.");
                // Tùy chọn: Xử lý trường hợp contact chưa được tải.
                // Hiện tại, chúng ta giả định contact đã có trong danh sách.
            }
        }


        // THÊM 3: PHƯƠNG THỨC XỬ LÝ KHI NHẬN ĐƯỢC EVENT TỪ CHATVIEWMODEL
        private void OnNewMessageNotificationFromChat(object sender, NewMessageEventArgs e)
        {
            // Chỉ cần chuyển tiếp (phát lại) event này để MainWindow bắt được
            ShowNotificationRequested?.Invoke(this, e);
        }

        // THÊM MỚI: Phương thức xử lý sự kiện được phát ra từ FriendListViewModel
        private void OnStartChatReceived(StartChatEvent e)
        {
            if (e?.FriendToChat == null) return;

            // Bước 1: Yêu cầu ChatViewModel chuẩn bị cuộc trò chuyện
            ChatVm.InitiateChatWith(e.FriendToChat);

            // Bước 2: Chuyển View hiện tại sang ChatView
            CurrentViewModel = ChatVm;
        }

        [RelayCommand]
        private void ToggleAIChat()
        {
            IsAIChatVisible = !IsAIChatVisible;
        }

        // --- Commands để thay đổi CurrentViewModel ---
        [RelayCommand] private void ShowChat() => CurrentViewModel = ChatVm;
        [RelayCommand] private void ShowFriendList() => CurrentViewModel = FriendListVm;
        [RelayCommand] private void ShowSettings() => CurrentViewModel = SettingsVm;

        // ... các command và phương thức khác của bạn giữ nguyên ...
        [RelayCommand]
        private void ShowAddFriendPopup()
        {
            var addFriendWin = new AddFriendWindow();
            if (Application.Current.MainWindow != null)
            {
                addFriendWin.Owner = Application.Current.MainWindow;
            }
            addFriendWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            addFriendWin.Show();
        }

        private void OnMatchFound(string roomId, string opponentId)
        {
            MessageBox.Show($"Đã ghép cặp thành công với {opponentId}! Vào phòng chat: {roomId}");
            CurrentViewModel = ChatVm;
        }

        // ... các phương thức khác giữ nguyên ...

        public void Cleanup()
        {
            _notificationListener?.Dispose();
            System.Diagnostics.Debug.WriteLine("MainViewModel: Starting Cleanup...");

            // THÊM 4: HỦY ĐĂNG KÝ SỰ KIỆN KHI DỌN DẸP ĐỂ TRÁNH MEMORY LEAK
            if (ChatVm != null)
            {
                ChatVm.NewMessageNotificationRequested -= OnNewMessageNotificationFromChat;
            }
            _callListener?.Dispose();
            ChatVm?.Cleanup();
            System.Diagnostics.Debug.WriteLine("MainViewModel: Cleanup of cached child ViewModels complete.");
        }
    }
}
