using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Database;
using System.Windows; // Cần cho Application.Current.MainWindow nếu dùng làm Owner popup

// Thêm using cho các ViewModel khác và các View (Window) bạn sẽ mở popup
using WpfApp1.ViewModels; // Chứa ChatViewModel, AddFriendViewModel, FriendListViewModel...
using WpfApp1.Views;      // Chứa ProfileWindow, SettingsWindow...

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

        public MainViewModel(FirebaseClient firebaseClient)
        {
            // Gán FirebaseClient được truyền vào
            _firebaseClient = firebaseClient;

            // Khởi tạo View/ViewModel mặc định khi ứng dụng bắt đầu
            // Ví dụ: Hiển thị ChatView làm mặc định
            ChatVm = new ChatViewModel(_firebaseClient); // ChatViewModel giờ nhận FirebaseClient
            _chatViewModel = ChatVm; // Gán reference để dùng trong các method khác

            SettingsVm = new SettingsViewModel(ChatVm, _firebaseClient);
            CurrentViewModel = ChatVm; // Sử dụng ChatVm đã tạo thay vì tạo mới
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

        [RelayCommand]
        private void ShowProfilePopup()
        {
            MessageBox.Show("ProfileWindow chưa được tạo!"); // Thông báo tạm thời
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

        public void Cleanup()
        {
            System.Diagnostics.Debug.WriteLine("MainViewModel: Starting Cleanup...");

            _chatViewModel?.Cleanup();

            System.Diagnostics.Debug.WriteLine("MainViewModel: Cleanup of cached child ViewModels complete.");
        }
    }
}