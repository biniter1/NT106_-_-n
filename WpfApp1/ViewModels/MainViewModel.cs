using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        private ObservableObject _currentViewModel; // Kiểu là ObservableObject hoặc lớp cơ sở chung khác
        private ChatViewModel _chatViewModel = new ChatViewModel();
        // Constructor
        public MainViewModel()
        {
            // Khởi tạo View/ViewModel mặc định khi ứng dụng bắt đầu
            // Ví dụ: Hiển thị ChatView làm mặc định
            CurrentViewModel = new ChatViewModel();
        }

        // --- Commands để thay đổi CurrentViewModel (hiển thị trong ContentControl) ---

        [RelayCommand]
        private void ShowChat()
        {
            // Kiểm tra để tránh tạo lại nếu đang hiển thị rồi (tùy chọn)
            if (CurrentViewModel is not ChatViewModel)
            {
                CurrentViewModel = new ChatViewModel();
            }
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
            // Tạo và hiển thị cửa sổ Profile (giả sử tên là ProfileWindow)
            // ProfileWindow profileWin = new ProfileWindow();
            // profileWin.Owner = Application.Current.MainWindow; // Gán Owner nếu cần
            // profileWin.Show();
            MessageBox.Show("ProfileWindow chưa được tạo!"); // Thông báo tạm thời
        }

        [RelayCommand]
        private void ShowSettingsPopup()
        {
            var settingsVM = new SettingsViewModel(_chatViewModel);
            var settingsWin = new SettingsWindow
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
            if (CurrentViewModel is not SettingsViewModel)
            {
                CurrentViewModel = new SettingsViewModel();
            }
        }
        // (Tùy chọn) Các thuộc tính khác cho MainWindow Shell
        // Ví dụ: Thông tin người dùng đang đăng nhập hiển thị ở đâu đó trên Shell
        // [ObservableProperty]
        // private User _loggedInUser;

        public void Cleanup()
        {
            System.Diagnostics.Debug.WriteLine("MainViewModel: Starting Cleanup...");

            _chatViewModel.Cleanup();

            System.Diagnostics.Debug.WriteLine("MainViewModel: Cleanup of cached child ViewModels complete.");
        }
    }
}