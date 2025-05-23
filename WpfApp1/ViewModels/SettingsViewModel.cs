using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using WpfApp1.LoginlSignUp; // Thêm namespace để gọi fLogin.PerformLogout

namespace WpfApp1.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        // Thuộc tính để điều khiển nội dung hiển thị bên phải
        [ObservableProperty]
        private string _currentView;

        // Thuộc tính cho thông tin tài khoản
        [ObservableProperty]
        private string _userName = "Nguyễn Thị Hà";

        [ObservableProperty]
        private string _userEmail = "nguyenha@example.com";

        [ObservableProperty]
        private DateTime _joinDate = new DateTime(2025, 5, 18);

        // Thuộc tính cho phần "Cài đặt"
        [ObservableProperty]
        private string _selectedTheme;

        [ObservableProperty]
        private bool _enableNotifications;

        [ObservableProperty]
        private bool _showDesktopNotifications;

        [ObservableProperty]
        private bool _notifyWhenOffline;

        [ObservableProperty]
        private string _selectedLanguage;

        // Tạo tham chiếu, giúp gọi được hàm của chatviewmodel
        private readonly ChatViewModel _chatViewModel;

        public SettingsViewModel(ChatViewModel chatViewModel)
        {
            _chatViewModel = chatViewModel;
        }

        // Danh sách các tùy chọn
        public ObservableCollection<string> Themes { get; } = new ObservableCollection<string> { "Light", "Dark" };
        public ObservableCollection<string> Languages { get; } = new ObservableCollection<string> { "Tiếng Việt", "English", "日本語" };

        public SettingsViewModel()
        {
            // Giá trị mặc định
            _selectedTheme = "Light";
            _enableNotifications = true;
            _showDesktopNotifications = true;
            _notifyWhenOffline = false;
            _selectedLanguage = "Tiếng Việt";
            _currentView = "AccountInfo"; // Mặc định hiển thị thông tin tài khoản
        }

        [RelayCommand]
        private void ShowAccountInfo()
        {
            CurrentView = "AccountInfo";
        }

        [RelayCommand]
        private void ShowSettings()
        {
            CurrentView = "Settings";
        }

        [RelayCommand]
        private void ShowData()
        {
            CurrentView = "Data";
        }

        [RelayCommand]
        private void ChangeLanguage()
        {
            CurrentView = "Language";
        }

        [RelayCommand]
        private void GetSupport()
        {
            CurrentView = "Support";
        }

        [RelayCommand]
        private void Logout()
        {
            _chatViewModel.Cleanup();
            MessageBox.Show("Đăng xuất thành công!");

            // Tạo instance của fLogin để gọi phương thức PerformLogout
            fLogin loginWindow = new fLogin();
            loginWindow.PerformLogout();

            // Đóng SettingsWindow
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    window.Close();
                    break;
                }
            }
        }

        [RelayCommand]
        private void Exit()
        {
            Application.Current.Shutdown();
        }

        [RelayCommand]
        private void EditProfile()
        {
            MessageBox.Show("Tính năng chỉnh sửa hồ sơ đang được phát triển!");
        }

        [RelayCommand]
        private void SaveSettings()
        {
            MessageBox.Show($"Cài đặt đã được lưu: Theme={SelectedTheme}, Notifications={EnableNotifications}, Desktop Notifications={ShowDesktopNotifications}, Notify Offline={NotifyWhenOffline}, Language={SelectedLanguage}");
        }

        [RelayCommand]
        private void ExportData()
        {
            MessageBox.Show("Xuất dữ liệu thành công!");
        }

        [RelayCommand]
        private void DeleteData()
        {
            var result = MessageBox.Show("Bạn có chắc chắn muốn xóa tất cả dữ liệu? Hành động này không thể hoàn tác.", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                MessageBox.Show("Dữ liệu đã được xóa!");
            }
        }

        [RelayCommand]
        private void ApplyLanguage()
        {
            MessageBox.Show($"Ngôn ngữ đã được áp dụng: {SelectedLanguage}");
        }

        [RelayCommand]
        private void SendFeedback()
        {
            MessageBox.Show("Phản hồi của bạn đã được gửi!");
        }

        [RelayCommand]
        private void ShowUserGuide()
        {
            MessageBox.Show("Hướng dẫn sử dụng đang được phát triển!");
        }
    }
}