using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using WpfApp1.LoginlSignUp;
using System.Windows.Markup;
using System.Windows.Input;
using WpfApp1.Models;

namespace WpfApp1.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {

        [ObservableProperty]
        private string _currentView;

        // Thuộc tính cho thông tin tài khoản
        [ObservableProperty]
        private string _userName = SharedData.Instance.userdata.Name;

        [ObservableProperty]
        private string _userEmail = SharedData.Instance.userdata.Email;

        [ObservableProperty]
        private DateTime _joinDate = SharedData.Instance.userdata.DateTime;

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

        [ObservableProperty]
        private string _userAvatarUrl;

        // Tạo tham chiếu, giúp gọi được hàm của chatviewmodel
        private readonly ChatViewModel _chatViewModel;

        public SettingsViewModel(ChatViewModel chatViewModel)
        {
            _chatViewModel = chatViewModel;
            InitializeSettings();
            // Subscribe to AvatarUpdated event from EditProfileViewModel
            EditProfileViewModel.AvatarUpdated += OnAvatarUpdated;
        }

        public SettingsViewModel()
        {
            InitializeSettings();
            // Subscribe to AvatarUpdated event from EditProfileViewModel
            EditProfileViewModel.AvatarUpdated += OnAvatarUpdated;
        }

        // Danh sách các tùy chọn
        public ObservableCollection<string> Themes { get; } = new ObservableCollection<string> { "Light", "Dark" };
        public ObservableCollection<string> Languages { get; } = new ObservableCollection<string> { "Tiếng Việt", "English", "日本語" };

        private void InitializeSettings()
        {
            // Giá trị mặc định
            _selectedTheme = "Light";
            _enableNotifications = true;
            _showDesktopNotifications = true;
            _notifyWhenOffline = false;
            _selectedLanguage = "Tiếng Việt";
            _currentView = "AccountInfo"; // Mặc định hiển thị thông tin tài khoản
            _userAvatarUrl = SharedData.Instance.userdata.AvatarUrl ?? "";
            var savedLanguage = Properties.Settings.Default.SelectedLanguage;
            _selectedLanguage = savedLanguage switch
            {
                "Tiếng Việt" => "Tiếng Việt",
                "English" => "English",
                _ => "Tiếng Việt" // Default to Tiếng Việt
            };
        }

        // Handle avatar update from EditProfileViewModel
        private void OnAvatarUpdated(string newAvatarUrl)
        {
            UserAvatarUrl = newAvatarUrl;
            OnPropertyChanged(nameof(UserAvatarUrl));
        }

        // Phương thức tiện ích để lấy chuỗi từ ResourceDictionary
        private string GetLocalizedString(string key)
        {
            if (Application.Current.Resources["LocalizationDictionary"] is ResourceDictionary localizationDict)
            {
                return localizationDict.Contains(key) ? localizationDict[key].ToString() : key;
            }
            return key; // Fallback nếu không tìm thấy
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
            _chatViewModel?.Cleanup();
            MessageBox.Show(GetLocalizedString("LogoutSuccessMessage"));

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
            try
            {
                var editProfileViewModel = new EditProfileViewModel();
                // Subscribe to ProfileUpdated event
                editProfileViewModel.ProfileUpdated += OnProfileUpdated;

                var editProfileWindow = new Views.EditProfileWindow
                {
                    Owner = Application.Current.MainWindow,
                    DataContext = editProfileViewModel
                };

                bool? result = editProfileWindow.ShowDialog();

                // No need to manually update here since ProfileUpdated event handles it
                if (result == true)
                {
                    MessageBox.Show(GetLocalizedString("ProfileUpdatedMessage") ?? "Profile updated successfully!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening edit profile: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnProfileUpdated(bool success)
        {
            if (success)
            {
                // Update properties from SharedData
                UserName = SharedData.Instance.userdata.Name;
                UserEmail = SharedData.Instance.userdata.Email;
                // Notify UI to refresh
                OnPropertyChanged(nameof(UserName));
                OnPropertyChanged(nameof(UserEmail));
            }
        }

        [RelayCommand]
        private void SaveSettings()
        {
            MessageBox.Show(GetLocalizedString("SettingsSavedMessage") +
                           $" Theme={SelectedTheme}, Notifications={EnableNotifications}, Desktop Notifications={ShowDesktopNotifications}, Notify Offline={NotifyWhenOffline}, Language={SelectedLanguage}");
        }

        [RelayCommand]
        private void ExportData()
        {
            MessageBox.Show(GetLocalizedString("ExportDataSuccess"));
        }

        [RelayCommand]
        private void DeleteData()
        {
            var result = MessageBox.Show(GetLocalizedString("DeleteDataConfirmation"), GetLocalizedString("Confirmation"),
                                        MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                MessageBox.Show(GetLocalizedString("DataDeletedMessage"));
            }
        }

        [RelayCommand]
        private void ApplyLanguage()
        {
            try
            {
                // Áp dụng thay đổi ngôn ngữ toàn cục
                ApplyLanguageGlobally(SelectedLanguage);

                MessageBox.Show($"Language applied: {SelectedLanguage}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying language: {ex.Message}");
            }
        }

        private void ApplyLanguageGlobally(string language)
        {
            try
            {
                // 1. Xác định đường dẫn resource file
                string resourcePath = language switch
                {
                    "Tiếng Việt" => "Resources/StringResources.vi.xaml",
                    "English" => "Resources/StringResources.en.xaml",
                    _ => "Resources/StringResources.vi.xaml"
                };

                // 2. Cập nhật Application resources
                var app = Application.Current;
                if (app == null) return;

                // 3. Tạo resource dictionary mới
                ResourceDictionary newResourceDict;
                try
                {
                    newResourceDict = new ResourceDictionary
                    {
                        Source = new Uri(resourcePath, UriKind.Relative)
                    };
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Could not load resource: {resourcePath}. Error: {ex.Message}");
                    return;
                }

                // 4. Tìm và xóa localization dictionary cũ
                ResourceDictionary oldDict = null;
                foreach (var dict in app.Resources.MergedDictionaries)
                {
                    if (dict.Source != null && dict.Source.ToString().Contains("StringResources"))
                    {
                        oldDict = dict;
                        break;
                    }
                }

                if (oldDict != null)
                {
                    app.Resources.MergedDictionaries.Remove(oldDict);
                }

                // 5. Thêm dictionary mới
                app.Resources.MergedDictionaries.Add(newResourceDict);

                // 6. Lưu cài đặt ngôn ngữ
                Properties.Settings.Default.SelectedLanguage = language;
                Properties.Settings.Default.Save();

                // 7. Notify về việc thay đổi ngôn ngữ thay vì refresh windows
                NotifyLanguageChanged();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ApplyLanguageGlobally: {ex.Message}");
                throw;
            }
        }

        private void NotifyLanguageChanged()
        {
            try
            {
                // Sử dụng Dispatcher để đảm bảo UI update an toàn
                Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                {
                    // Trigger property changed để UI cập nhật
                    OnPropertyChanged(nameof(SelectedLanguage));

                    // Force update layout của current window
                    foreach (Window window in Application.Current.Windows)
                    {
                        window?.InvalidateVisual();
                    }
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error notifying language change: {ex.Message}");
            }
        }

        [RelayCommand]
        private void SendFeedback()
        {
            MessageBox.Show(GetLocalizedString("FeedbackSentMessage"));
        }

        [RelayCommand]
        private void ShowUserGuide()
        {
            MessageBox.Show(GetLocalizedString("UserGuideUnderDevelopment"));
        }

        // Method được gọi từ RadioButton event trong code-behind
        public void OnLanguageChanged(string language)
        {
            if (SelectedLanguage != language)
            {
                SelectedLanguage = language;
                // Áp dụng ngay lập tức mà không cần nhấn Apply
                ApplyLanguageGlobally(language);
            }
        }
    }
}