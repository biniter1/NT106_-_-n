using System;
using System.Windows;
using System.Windows.Media.Imaging;
using WpfApp1.Models;

namespace WpfApp1.Views
{
    public partial class IncomingCallWindow : Window
    {
        public event Action<CallSignal> CallAccepted;
        public event Action<CallSignal> CallDeclined;

        private readonly CallSignal _callSignal;

        public IncomingCallWindow(CallSignal call)
        {
            InitializeComponent();
            _callSignal = call;

            // Hiển thị thông tin người gọi
            CallerNameText.Text = call.CallerName;
            CallTypeText.Text = $"đang thực hiện cuộc gọi {(call.CallType == "Video" ? "video" : "thoại")}";

            if (!string.IsNullOrEmpty(call.CallerAvatarUrl) && Uri.TryCreate(call.CallerAvatarUrl, UriKind.Absolute, out Uri avatarUri))
            {
                CallerAvatar.Source = new BitmapImage(avatarUri);
            }
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            CallAccepted?.Invoke(_callSignal);
            this.Close();
        }

        private void DeclineButton_Click(object sender, RoutedEventArgs e)
        {
            CallDeclined?.Invoke(_callSignal);
            this.Close();
        }
    }
}
