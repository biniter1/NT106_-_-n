using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using ToastNotifications.Core;
using WpfApp1.Models;
using WpfApp1.ViewModels;
using WpfApp1.Views;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string CurrentEmail;
        private System.Timers.Timer _notificationTimer;

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

            mainViewModel.ShowNotificationRequested += MainViewModel_ShowNotificationRequested;
        }

        // Xử lý khi nhận được yêu cầu hiển thị thông báo
        private void MainViewModel_ShowNotificationRequested(object sender, NewMessageEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ShowDesktopNotification(e.Title, e.Message, e.ChatID);
            });
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

        private void ShowDesktopNotification(string title, string message, string chatID)
        {
            try
            {
                Debug.WriteLine($"ShowDesktopNotification called: {title} - {message}");

                // Ensure we're on the UI thread
                if (Application.Current.Dispatcher.CheckAccess())
                {
                    CreateNotificationWindow(title, message);
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() => CreateNotificationWindow(title, message));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing desktop notification: {ex.Message}");
            }
        }
        private void CreateNotificationWindow(string title, string message)
        {
            try
            {
                var notificationWindow = new NotificationWindow(title, message);
                notificationWindow.Show();
                Debug.WriteLine("NotificationWindow created and shown");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating notification window: {ex.Message}");
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                // Đóng tất cả notifications
                NotificationWindow.CloseAllNotifications();

                if (this.DataContext is MainViewModel mainVM)
                {
                    mainVM.ShowNotificationRequested -= MainViewModel_ShowNotificationRequested;
                    mainVM.Cleanup();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during window closing: {ex.Message}");
            }
        }
        private void CloseAllNotifications()
        {
            NotificationWindow.CloseAllNotifications();
        }
    }
}
