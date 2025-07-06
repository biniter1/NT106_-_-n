// CustomMessageBox.cs
using System.Windows;
using WpfApp1;

namespace WpfApp1.Views
{ 
    public static class CustomMessageBox
    {
        // Cập nhật phương thức Show
        public static MessageBoxResult Show(string message, string title = "Thông báo",
                                            CustomMessageBoxWindow.MessageButtons buttons = CustomMessageBoxWindow.MessageButtons.OK,
                                            CustomMessageBoxWindow.MessageIcon icon = CustomMessageBoxWindow.MessageIcon.None) // Tham số icon MỚI
        {
            var window = new CustomMessageBoxWindow(message, title, buttons, icon); // Truyền icon vào
            window.ShowDialog();
            return window.Result;
        }
    }
}