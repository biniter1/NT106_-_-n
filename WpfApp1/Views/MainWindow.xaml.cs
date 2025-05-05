using System.Collections.Specialized;
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
            var viewModel = new MainViewModel();
            this.DataContext = viewModel;
            //// === THÊM CODE TỰ ĐỘNG CUỘN ===
            //// Kiểm tra xem ViewModel và Messages collection có hợp lệ không
            //if (viewModel.Messages is INotifyCollectionChanged notifyCollection)
            //{
            //    // Đăng ký sự kiện CollectionChanged
            //    notifyCollection.CollectionChanged += Messages_CollectionChanged;

            //    // Xử lý trường hợp ScrollViewer được load sau ListBox (an toàn hơn)
            //    MessagesScrollViewer.Loaded += (s, e) => ScrollToBottomAfterLoad(notifyCollection);

            //}
            //// ============================
            ///
            CurrentEmail=email;
            LoadDataCurrentUser(email);
        }
        public async void LoadDataCurrentUser(string eamil)
        {
            var db=FirestoreHelper.database;
            var doc = db.Collection("users").Document(eamil);
            var snap=await doc.GetSnapshotAsync();
            if (snap.Exists)
            {
                var user=snap.ConvertTo<User>();
                SharedData.Instance.userdata.Ho=user.Ho;
                SharedData.Instance.userdata.Name = user.Name;
                SharedData.Instance.userdata.Email = user.Email;
                SharedData.Instance.userdata.Password = user.Password;
                SharedData.Instance.userdata.Username = user.Username;
                SharedData.Instance.userdata.Phone = user.Phone;
                SharedData.Instance.userdata.gender=user.gender;
                SharedData.Instance.userdata.DateTime = user.DateTime;  
                SharedData.Instance.userdata.Address = user.Address;
                SharedData.Instance.userdata.IdToken = user.IdToken;    
                SharedData.Instance.userdata.AvatarUrl = user.AvatarUrl;    
            }
        }
        //// Xử lý sự kiện khi collection Messages thay đổi
        //private void Messages_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        //{
        //    // Chỉ xử lý khi có item mới được thêm vào
        //    if (e.Action == NotifyCollectionChangedAction.Add)
        //    {
        //        // Dispatcher.InvokeAsync để đảm bảo việc cuộn xảy ra trên UI thread
        //        // và sau khi layout đã được cập nhật
        //        Dispatcher.InvokeAsync(() =>
        //        {
        //            MessagesScrollViewer.ScrollToBottom();
        //        });
        //    }
        //}

        //// Hàm phụ trợ để cuộn xuống khi control được load (nếu có item ban đầu)
        //private void ScrollToBottomAfterLoad(INotifyCollectionChanged collection)
        //{
        //    if (collection is System.Collections.ICollection { Count: > 0 }) // Kiểm tra collection có item không
        //    {
        //        Dispatcher.InvokeAsync(() =>
        //        {
        //            MessagesScrollViewer.ScrollToBottom();
        //        });
        //    }
        //}


        //protected override void OnClosed(EventArgs e)
        //{
        //    if (this.DataContext is ChatViewModel viewModel && viewModel.Messages is INotifyCollectionChanged notifyCollection)
        //   {
        //       notifyCollection.CollectionChanged -= Messages_CollectionChanged;
        //    }
        //    base.OnClosed(e);
        //}

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AddFriendWindow addFriendWin = new AddFriendWindow();
            addFriendWin.Owner = this;
            addFriendWin.Show();
        }
    }

}
