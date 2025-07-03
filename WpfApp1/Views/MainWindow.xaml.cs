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