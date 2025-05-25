using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
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

        public MainWindow(string email)
        {
            InitializeComponent();
            CurrentEmail = email;
            Loaded += MainWindow_Loaded; // Use Loaded event to ensure UI is ready
            this.Closing += MainWindow_Closing;
            LocalizationManager.LanguageChanged += OnLanguageChanged;
        }
        private void OnLanguageChanged(object sender, EventArgs e)
        {
            // Force the UI to refresh bindings
            InvalidateVisual();
            // Optionally, update specific bindings
            UpdateBindings();
        }
        private void UpdateBindings()
        {
            // Update bindings for controls that use localized strings
            foreach (var element in LogicalTreeHelper.GetChildren(this))
            {
                if (element is FrameworkElement fe)
                {
                    fe.GetBindingExpression(FrameworkElement.DataContextProperty)?.UpdateTarget();
                    // Update other bindings as needed
                }
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataCurrentUser(CurrentEmail);

            // Initialize ChatViewModel after user data is loaded
            var chatView = new ChatView
            {
                DataContext = new ChatViewModel()
            };
            //chatGrid.Children.Add(chatView); // Replace "chatGrid" with the actual Grid name in XAML

            var mainViewModel = new MainViewModel();
            this.DataContext = mainViewModel;
        }

        public async Task LoadDataCurrentUser(string email)
        {
            var db = FirestoreHelper.database;
            if (db == null)
            {
                MessageBox.Show("Firestore database is not initialized!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var doc = db.Collection("users").Document(email);
            var snap = await doc.GetSnapshotAsync();
            if (snap.Exists)
            {
                var user = snap.ConvertTo<User>();
                SharedData.Instance.userdata = user; // Assign the entire user object
                Debug.WriteLine($"Loaded user data for: {user.Email}");
            }
            else
            {
                Debug.WriteLine($"User document not found for email: {email}");
                MessageBox.Show($"User not found for email: {email}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AddFriendWindow addFriendWin = new AddFriendWindow();
            addFriendWin.Owner = this;
            addFriendWin.Show();
        }
        public void ShowNotification(string message)
        {
            NotificationText.Text = message;
            MessageNotificationPopup.IsOpen = true;

            // Tạo và cấu hình Timer
            System.Timers.Timer timer = new System.Timers.Timer(3000); // 3000ms = 3 giây
            timer.AutoReset = false; // Chỉ chạy một lần

            // Gán sự kiện Elapsed
            timer.Elapsed += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    MessageNotificationPopup.IsOpen = false;
                });
                timer.Dispose(); // Giải phóng tài nguyên
            };

            timer.Start();
        }
        private void NotificationText_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Tìm liên hệ từ nội dung thông báo
            string notificationText = NotificationText.Text;
            string contactName = notificationText.Split(':')[0].Replace("Tin nhắn mới từ ", "").Trim();

            // Tìm ViewModel hiện tại
            if (DataContext is MainViewModel mainViewModel && mainViewModel.CurrentViewModel is ChatViewModel chatViewModel)
            {
                // Tìm liên hệ trong danh sách Contacts
                var contact = chatViewModel.Contacts.FirstOrDefault(c => c.Name == contactName);
                if (contact != null)
                {
                    chatViewModel.SelectedContact = contact; // Chuyển đến cuộc trò chuyện
                }
            }

            MessageNotificationPopup.IsOpen = false; // Đóng Popup
        }

        // Sự kiện đóng Popup
        private void CloseNotification_Click(object sender, RoutedEventArgs e)
        {
            MessageNotificationPopup.IsOpen = false;
        }

        // Sự kiện closing
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            // Lấy MainViewModel từ DataContext của MainWindow
            if (this.DataContext is MainViewModel mainVM)
            {
                System.Diagnostics.Debug.WriteLine("MainWindow_Closing: Calling MainViewModel.Cleanup().");
                mainVM.Cleanup(); // Gọi Cleanup của MainViewModel
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("MainWindow_Closing: MainViewModel not found in DataContext. Cleanup might be incomplete.");
            }
        }
    }

}
