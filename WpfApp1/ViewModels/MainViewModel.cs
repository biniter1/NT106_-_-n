using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Database;
using System.Collections.ObjectModel;
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
        private ChatViewModel _chatViewModel;

        public ChatViewModel ChatVm { get; private set; }
        public SettingsViewModel SettingsVm { get; private set; }
        public MatchingChatViewModel MatchingChatVm { get; }

        private readonly NotificationService _notificationService;
        private IDisposable _notificationListener;

        [ObservableProperty]
        private bool _isNotificationPopupVisible;

        [ObservableProperty]
        private string _notificationMessage;

        [ObservableProperty]
        private int _unreadNotificationCount;

        public ObservableCollection<Notification> AllNotifications { get; } = new();

        public MainViewModel(FirebaseClient firebaseClient)
        {
            // Gán FirebaseClient được truyền vào
            _firebaseClient = firebaseClient;

            // Khởi tạo View/ViewModel mặc định khi ứng dụng bắt đầu
            // Ví dụ: Hiển thị ChatView làm mặc định
            ChatVm = new ChatViewModel(_firebaseClient); // ChatViewModel giờ nhận FirebaseClient
            _chatViewModel = ChatVm; // Gán reference để dùng trong các method khác
            string currentEmail=SharedData.Instance.userdata.Email;
            string safeKey = currentEmail.Replace(".", "_");
            MatchingChatVm = new MatchingChatViewModel(_firebaseClient, safeKey);
            MatchingChatVm.MatchFound += OnMatchFound;

            SettingsVm = new SettingsViewModel(ChatVm, _firebaseClient);
            CurrentViewModel = ChatVm; // Sử dụng ChatVm đã tạo thay vì tạo mới

            _notificationService = new NotificationService(firebaseClient);

            _notificationListener = _notificationService.ListenForNotifications(safeKey, (notification) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Tránh thêm thông báo trùng lặp và chỉ xử lý thông báo chưa đọc
                    if (!notification.IsRead && !AllNotifications.Any(n => n.Id == notification.Id))
                    {
                        AllNotifications.Insert(0, notification); // Thêm vào đầu danh sách
                        UnreadNotificationCount = AllNotifications.Count(n => !n.IsRead);
                        NotificationMessage = notification.Message;
                        IsNotificationPopupVisible = true;
                    }
                });
            });
        }

        // --- Commands để thay đổi CurrentViewModel (hiển thị trong ContentControl) ---

        [RelayCommand]
        private void ShowChat()
        {
            // Sử dụng lại ChatVm đã tạo thay vì tạo mới
            CurrentViewModel = ChatVm;
        }

        [RelayCommand]
        private void ShowFriendList()
        {
            // Kiểm tra để tránh tạo lại nếu đang hiển thị rồi 
            if (CurrentViewModel is not FriendListViewModel)
            {
                CurrentViewModel = new FriendListViewModel();
            }
        }

        // Command để mở cửa sổ AddFriendWindow popup
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

        // --- Commands để mở cửa sổ Popup ---
        private void OnMatchFound(string roomId, string opponentId)
        {
            // Được gọi từ MatchingChatViewModel
            MessageBox.Show($"Đã ghép cặp thành công với {opponentId}! Vào phòng chat: {roomId}");

            // Yêu cầu ChatViewModel tải phòng chat mới
            // ChatVm.LoadRoom(roomId); // Giả sử ChatVm có phương thức này

            // Tự động chuyển về màn hình chat
            CurrentViewModel = ChatVm;
        }

        [RelayCommand]
        private void ShowSettingsPopup()
        {
            var settingsVM = new SettingsViewModel(_chatViewModel, _firebaseClient);
            var settingsWin = new SettingsWindow(_chatViewModel, _firebaseClient)
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
            // Sử dụng lại SettingsVm đã tạo thay vì tạo mới
            CurrentViewModel = SettingsVm;
        }
        [RelayCommand]
        private void HideNotificationPopup()
        {
            IsNotificationPopupVisible = false;
        }

        [RelayCommand]
        private void ShowNotifications()
        {
            // Tương lai: có thể mở một View riêng để hiển thị danh sách tất cả thông báo.
            MessageBox.Show($"Bạn có {UnreadNotificationCount} thông báo chưa đọc.");
        }
        public void Cleanup()
        {
            _notificationListener?.Dispose();

            System.Diagnostics.Debug.WriteLine("MainViewModel: Starting Cleanup...");

            _chatViewModel?.Cleanup();

            System.Diagnostics.Debug.WriteLine("MainViewModel: Cleanup of cached child ViewModels complete.");
        }
    }
}