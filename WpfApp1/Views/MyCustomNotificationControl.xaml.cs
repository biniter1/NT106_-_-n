using System;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1.Views
{
    public partial class MyCustomNotificationControl : UserControl
    {
        // Action này sẽ được gọi từ MainWindow để thực hiện việc đóng thông báo
        public Action CloseAction { get; set; }

        public MyCustomNotificationControl(string title, string message)
        {
            InitializeComponent();

            TitleText.Text = title;
            MessageText.Text = message;
        }

        // Phương thức xử lý sự kiện click cho nút đóng
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Khi người dùng nhấn nút "X", gọi Action đã được gán từ MainWindow
            CloseAction?.Invoke();
        }
    }
}