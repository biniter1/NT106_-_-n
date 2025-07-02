using System.Windows;
using ToastNotifications.Core;

namespace WpfApp1.Models
{
    internal class CustomNotificationDisplay : NotificationDisplayPart
    {
        public CustomNotificationDisplay(FrameworkElement content, NotificationBase notification)
        {
            Content = content;
            Bind(notification);
        }
    }
}