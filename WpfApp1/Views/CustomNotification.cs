using WpfApp1.Views;

namespace WpfApp1
{
    internal class CustomNotification
    {
        private MyCustomNotificationControl notificationControl;
        private TimeSpan expirationTime;

        public CustomNotification(MyCustomNotificationControl notificationControl, TimeSpan expirationTime)
        {
            this.notificationControl = notificationControl;
            this.expirationTime = expirationTime;
        }
    }
}