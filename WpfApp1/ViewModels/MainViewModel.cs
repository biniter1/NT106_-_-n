using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Database;
using System; // Thêm using này nếu chưa có
using System.Collections.ObjectModel;
using System.Linq; // Thêm using này nếu chưa có
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

        // THÊM MỚI: Tạo instance duy nhất cho FriendListVm
        public FriendListViewModel FriendListVm { get; private set; }

        public AIChatViewModel AIChatVm { get; }

        private readonly NotificationService _notificationService;
        private IDisposable _notificationListener;

        [ObservableProperty]
        private bool _isNotificationPopupVisible;

        [ObservableProperty]
        private string _notificationMessage;

        [ObservableProperty]
        private int _unreadNotificationCount;

        [ObservableProperty]
        private bool _isAIChatVisible;
        public ObservableCollection<Notification> AllNotifications { get; } = new();

        public MainViewModel(FirebaseClient firebaseClient)
        {
            _firebaseClient = firebaseClient;

            // Khởi tạo các ViewModel con một lần duy nhất
            ChatVm = new ChatViewModel(_firebaseClient);
            SettingsVm = new SettingsViewModel(ChatVm, _firebaseClient);
            // THÊM MỚI: Khởi tạo FriendListVm
            FriendListVm = new FriendListViewModel();

            string currentEmail = SharedData.Instance.userdata.Email;
            string safeKey = currentEmail.Replace(".", "_");
            MatchingChatVm = new MatchingChatViewModel(_firebaseClient, safeKey);
            MatchingChatVm.MatchFound += OnMatchFound;

            AIChatVm = new AIChatViewModel();
            IsAIChatVisible = false; // Mặc định ẩn

            // Đặt View mặc định
            CurrentViewModel = ChatVm;

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

            // THÊM MỚI: Đăng ký lắng nghe sự kiện yêu cầu mở chat
            EventAggregator.Instance.Subscribe<StartChatEvent>(OnStartChatReceived);
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

        [RelayCommand]
        private void ShowChat()
        {
            CurrentViewModel = ChatVm;
        }

        [RelayCommand]
        private void ShowFriendList()
        {
            // THAY ĐỔI: Sử dụng lại FriendListVm đã tạo thay vì tạo mới
            CurrentViewModel = FriendListVm;
        }

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

        [RelayCommand]
        private void ShowSettingsPopup()
        {
            var settingsVM = new SettingsViewModel(ChatVm, _firebaseClient);
            var settingsWin = new SettingsWindow(ChatVm, _firebaseClient)
            {
                DataContext = settingsVM
            };

            if (Application.Current.MainWindow != null)
            {
                settingsWin.Owner = Application.Current.MainWindow;
            }
            settingsWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            settingsWin.Show();
        }

        [RelayCommand]
        private void ShowSettings()
        {
            CurrentViewModel = SettingsVm;
        }

        [RelayCommand]
        private void ShowCreateGroupPopup()
        {
            var createGroupWindow = new Views.CreateGroupWindow
            {
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            
            createGroupWindow.ShowDialog();
        }

        private void HideNotificationPopup()
        {
            IsNotificationPopupVisible = false;
        }

        [RelayCommand]
        private void ShowNotifications()
        {
            MessageBox.Show($"Bạn có {UnreadNotificationCount} thông báo chưa đọc.");
        }

        public void Cleanup()
        {
            _notificationListener?.Dispose();
            System.Diagnostics.Debug.WriteLine("MainViewModel: Starting Cleanup...");
            ChatVm?.Cleanup();
            System.Diagnostics.Debug.WriteLine("MainViewModel: Cleanup of cached child ViewModels complete.");
        }
    }
}