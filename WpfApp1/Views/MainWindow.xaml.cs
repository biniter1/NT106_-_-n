using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Media;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Linq; // Thêm using này
using WpfApp1.Models;
using WpfApp1.ViewModels;
using WpfApp1.Views;
using ToastNotifications.Messages;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Core;
namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string CurrentEmail;
        private System.Timers.Timer _notificationTimer; // Lưu trữ timer để có thể dispose

        public MainWindow(string email)
        {
            InitializeComponent();
            CurrentEmail = email;
            Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
            LocalizationManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            InvalidateVisual();
            UpdateBindings();
        }

        private void UpdateBindings()
        {
            foreach (var element in LogicalTreeHelper.GetChildren(this))
            {
                if (element is FrameworkElement fe)
                {
                    fe.GetBindingExpression(FrameworkElement.DataContextProperty)?.UpdateTarget();
                }
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataCurrentUser(CurrentEmail);

            if (App.AppFirebaseClient == null)
            {
                MessageBox.Show("Lỗi khởi tạo Firebase: FirebaseClient chưa được thiết lập. Vui lòng đăng nhập lại.",
                    "Lỗi Firebase", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            var mainViewModel = new MainViewModel(App.AppFirebaseClient);
            this.DataContext = mainViewModel;

            if (mainViewModel.ChatVm != null)
            {
                mainViewModel.ChatVm.OnNewMessageNotificationRequested += HandleNewMessageNotification;
                Debug.WriteLine("Đã đăng ký sự kiện OnNewMessageNotificationRequested từ ChatViewModel.");
            }
            else
            {
                Debug.WriteLine("ChatViewModel (ChatVm) trong MainViewModel là null. Không thể đăng ký thông báo.");
            }
        }

        public async Task LoadDataCurrentUser(string email)
        {
            var db = FirestoreHelper.database;
            if (db == null)
            {
                MessageBox.Show("Firestore database is not initialized!", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var doc = db.Collection("users").Document(email);
                var snap = await doc.GetSnapshotAsync();
                if (snap.Exists)
                {
                    var user = snap.ConvertTo<User>();
                    SharedData.Instance.userdata = user;
                    Debug.WriteLine($"Loaded user data for: {user.Email}");
                }
                else
                {
                    Debug.WriteLine($"User document not found for email: {email}");
                    MessageBox.Show($"User not found for email: {email}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading user data: {ex.Message}");
                MessageBox.Show($"Error loading user data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AddFriendWindow addFriendWin = new AddFriendWindow();
            addFriendWin.Owner = this;
            addFriendWin.Show();
        }

        // Method đơn giản cho notification cơ bản
        public void ShowNotification(string message)
        {
            ShowNotificationWithChatId(message, null);
        }

        // Method chính cho notification với chatID
        public void ShowNotificationWithChatId(string message, string chatID)
        {
            try
            {
                NotificationText.Text = message;
                if (!string.IsNullOrEmpty(chatID))
                {
                    NotificationText.Tag = chatID;
                }
                MessageNotificationPopup.IsOpen = true;

                // Dispose timer cũ nếu có
                _notificationTimer?.Dispose();

                // Tạo timer mới
                _notificationTimer = new System.Timers.Timer(3000);
                _notificationTimer.AutoReset = false;
                _notificationTimer.Elapsed += (s, e) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageNotificationPopup.IsOpen = false;
                    });
                    _notificationTimer?.Dispose();
                    _notificationTimer = null;
                };
                _notificationTimer.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing notification: {ex.Message}");
            }
        }

        private string GetLocalizedString(string key)
        {
            try
            {
                if (Application.Current.Resources["LocalizationDictionary"] is ResourceDictionary localizationDict)
                {
                    return localizationDict.Contains(key) ? localizationDict[key].ToString() : key;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting localized string: {ex.Message}");
            }
            return key;
        }

        private void HandleNewMessageNotification(string senderName, string messageContent, string chatID)
        {
            try
            {
                PlayNotificationSound();

                bool enableNotifications = true;
                bool showDesktopNotifications = true;

                if (this.DataContext is MainViewModel mainVm && mainVm.SettingsVm != null)
                {
                    enableNotifications = mainVm.SettingsVm.EnableNotifications;
                    showDesktopNotifications = mainVm.SettingsVm.ShowDesktopNotifications;
                }

                if (!enableNotifications)
                {
                    Debug.WriteLine("Thông báo đã bị tắt trong cài đặt. Bỏ qua hiển thị.");
                    return;
                }

                string notificationTitle = GetLocalizedString("NewMessageArrived");
                string fullNotificationMessage = $"{GetLocalizedString("Notification_NewMessageFrom")} {senderName}: {messageContent}";

                // Kiểm tra xem có cần hiển thị notification không
                var mainViewModel = this.DataContext as MainViewModel;
                ChatViewModel currentChatViewModel = mainViewModel?.ChatVm;
                bool isCurrentChatSelected = !string.IsNullOrEmpty(chatID) &&
                    currentChatViewModel?.SelectedContact?.chatID == chatID;

                if (!this.IsActive || (!isCurrentChatSelected && this.IsActive))
                {
                    ShowNotificationWithChatId(fullNotificationMessage, chatID);
                }

                if (showDesktopNotifications)
                {
                    ShowDesktopNotification(notificationTitle, fullNotificationMessage, chatID);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling new message notification: {ex.Message}");
            }
        }

        private void ShowDesktopNotification(string title, string message, string chatID)
        {
            try
            {
                if (App.AppNotifier != null)
                {
                    // Tạo UserControl với nội dung thông báo động
                    var notificationControl = new MyCustomNotificationControl(message);

                    // Tạo custom notification
                    var notification = new MyCustomNotification(
                                    message,                       // text quản lý
                                    notificationControl,           // UserControl hiển thị
                                    new MessageOptions
                                    {           // options
                                        FreezeOnMouseEnter = true,
                                        UnfreezeOnMouseLeave = true,
                                        // Bạn có thể thêm Duration, v.v.
                                    }
                                );

                    App.AppNotifier.Notify<MyCustomNotification>(() => notification);
                }
                else
                {
                    Debug.WriteLine("AppNotifier is null. Không thể hiển thị desktop notification.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing desktop notification: {ex.Message}");
            }
        }

        private void PlayNotificationSound()
        {
            try
            {
                SystemSounds.Beep.Play();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi khi phát âm thanh thông báo: {ex.Message}");
            }
        }

        private void NotificationText_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                string chatIDToSwitch = (sender as TextBlock)?.Tag?.ToString();

                if (!string.IsNullOrEmpty(chatIDToSwitch))
                {
                    if (DataContext is MainViewModel mainViewModel && mainViewModel.ChatVm is ChatViewModel chatViewModel)
                    {
                        var contactToSwitch = chatViewModel.Contacts?.FirstOrDefault(c => c.chatID == chatIDToSwitch);
                        if (contactToSwitch != null)
                        {
                            chatViewModel.SelectedContact = contactToSwitch;
                            mainViewModel.CurrentViewModel = chatViewModel;
                            Debug.WriteLine($"Đã chuyển đến cuộc trò chuyện với: {contactToSwitch.Name}");
                        }
                    }
                }

                MessageNotificationPopup.IsOpen = false;

                if (this.WindowState == WindowState.Minimized)
                {
                    this.WindowState = WindowState.Normal;
                }
                this.Activate();
                this.Topmost = true;
                this.Topmost = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling notification click: {ex.Message}");
                MessageNotificationPopup.IsOpen = false;
            }
        }

        private void CloseNotification_Click(object sender, RoutedEventArgs e)
        {
            MessageNotificationPopup.IsOpen = false;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                // Dispose timer khi đóng window
                _notificationTimer?.Dispose();
                _notificationTimer = null;

                if (this.DataContext is MainViewModel mainVM)
                {
                    System.Diagnostics.Debug.WriteLine("MainWindow_Closing: Calling MainViewModel.Cleanup().");
                    mainVM.Cleanup();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("MainWindow_Closing: MainViewModel not found in DataContext. Cleanup might be incomplete.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during window closing: {ex.Message}");
            }
        }
    }
}