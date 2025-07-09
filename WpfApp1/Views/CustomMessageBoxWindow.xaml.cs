// CustomMessageBoxWindow.xaml.cs
using System.Windows;
using System.Windows.Media.Imaging;

namespace WpfApp1
{
    public partial class CustomMessageBoxWindow : Window
    {
        public enum MessageButtons
        {
            OK,
            YesNo
        }
        public enum MessageIcon { None, Information, Error, Warning, Success, Question }
        // Sử dụng MessageBoxResult của WPF để tương thích
        public MessageBoxResult Result { get; private set; }

        public CustomMessageBoxWindow(string message, string title, MessageButtons buttons, MessageIcon icon)
        {
            InitializeComponent();
            txtMessage.Text = message;
            txtTitle.Text = title;
            this.Title = title;

            SetupButtons(buttons);
            SetupIcon(icon);
        }
        private void SetupIcon(MessageIcon icon)
        {
            // LƯU Ý: Bạn cần phải có các file ảnh (ví dụ: info.png, error.png)
            // trong project và đặt Build Action của chúng là "Resource".
            // Ví dụ: tạo thư mục Assets, chép ảnh vào và set Build Action.
            string iconPath = null;
            switch (icon)
            {
                case MessageIcon.Information:
                case MessageIcon.Success: // Dùng chung icon info
                    iconPath = "pack://application:,,,/Assets/Icons/info.png";
                    break;
                case MessageIcon.Error:
                    iconPath = "pack://application:,,,/Assets/Icons/error.png";
                    break;
                case MessageIcon.Warning:
                    iconPath = "pack://application:,,,/Assets/Icons/warning.png";
                    break;
                case MessageIcon.None:
                    case MessageIcon.Question:
                    iconPath = "pack://application:,,,/Assets/Icons/question.png";
                    break;
                default:
                    imgIcon.Visibility = Visibility.Collapsed; // Ẩn control Image nếu không có icon
                    break;
            }

            if (iconPath != null)
            {
                imgIcon.Source = new BitmapImage(new Uri(iconPath));
            }
        }
        private void SetupButtons(MessageButtons buttons)
        {
            switch (buttons)
            {
                case MessageButtons.OK:
                    btnOK.Visibility = Visibility.Visible;
                    btnYes.Visibility = Visibility.Collapsed;
                    btnNo.Visibility = Visibility.Collapsed;
                    break;
                case MessageButtons.YesNo:
                    btnOK.Visibility = Visibility.Collapsed;
                    btnYes.Visibility = Visibility.Visible;
                    btnNo.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.Result = MessageBoxResult.OK;
            this.DialogResult = true; // Đóng window
        }

        private void btnYes_Click(object sender, RoutedEventArgs e)
        {
            this.Result = MessageBoxResult.Yes;
            this.DialogResult = true;
        }

        private void btnNo_Click(object sender, RoutedEventArgs e)
        {
            this.Result = MessageBoxResult.No;
            this.DialogResult = false;

        }
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Result = MessageBoxResult.Cancel; // Hoặc có thể là No tùy theo logic
            this.DialogResult = false; // Đóng window
        }
    }
}