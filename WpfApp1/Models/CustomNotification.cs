using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ToastNotifications.Core;

namespace WpfApp1.Models
{
    public class MyCustomNotification : NotificationBase
    {
        private readonly FrameworkElement _content;

        public MyCustomNotification(string message, FrameworkElement content, MessageOptions options)
            : base(message, options) // Gọi constructor bắt buộc
        {
            _content = content;
        }

        public override NotificationDisplayPart DisplayPart => new CustomNotificationDisplay(_content, this);
    }
}
