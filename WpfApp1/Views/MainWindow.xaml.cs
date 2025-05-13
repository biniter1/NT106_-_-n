using System.Collections.Specialized;
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
    }

}
