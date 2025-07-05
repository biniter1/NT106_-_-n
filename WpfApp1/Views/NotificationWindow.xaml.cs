using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace WpfApp1.Views
{
    public partial class NotificationWindow : Window
    {
        private readonly string _chatID;
        public static event Action<string> NotificationClicked;

        private DispatcherTimer _autoCloseTimer;
        private DispatcherTimer _progressTimer;
        private int _totalSeconds = 5;
        private int _currentSecond = 0;
        private bool _isClosing = false;

        public NotificationWindow(string title, string message, string chatID)
        {
            InitializeComponent();

            _chatID = chatID;
            TitleText.Text = title;
            MessageText.Text = message;

            SetupAutoCloseTimer();
            SetupProgressTimer();

            // THAY ĐỔI CHÍNH: Di chuyển việc định vị và bắt đầu animation vào sự kiện Loaded
            // để đảm bảo ActualWidth và ActualHeight đã có giá trị chính xác.
            this.Loaded += (s, e) =>
            {
                PositionWindow();
                StartFadeInAnimation();
            };

            this.MouseLeftButtonDown += NotificationWindow_MouseLeftButtonDown;
            Debug.WriteLine($"NotificationWindow created: {title} - {message} - ChatID: {_chatID}");
        }

        private void NotificationWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Bỏ qua nếu người dùng click vào nút đóng
            if (e.OriginalSource is FrameworkElement element && element.Name == "CloseButton")
            {
                return;
            }

            Debug.WriteLine($"Notification clicked for chat ID: {_chatID}");
            NotificationClicked?.Invoke(_chatID);
            CloseNotification();
        }

        /// <summary>
        /// Vị trí cửa sổ thông báo ở góc dưới bên phải.
        /// </summary>
        private void PositionWindow()
        {
            var workArea = SystemParameters.WorkArea;

            // Lấy danh sách các thông báo đang hiển thị, sắp xếp từ trên xuống dưới
            var existingNotifications = Application.Current.Windows.OfType<NotificationWindow>()
                                             .Where(w => w.IsVisible && w != this)
                                             .OrderBy(w => w.Top)
                                             .ToList();

            // Kích thước của cửa sổ này
            var notificationWidth = this.ActualWidth;
            var notificationHeight = this.ActualHeight;
            const double margin = 20.0;
            const double spacing = 10.0;

            // Tính toán vị trí Left
            this.Left = workArea.Right - notificationWidth - margin;

            // Tính toán vị trí Top, xếp chồng lên các thông báo đã có
            this.Top = workArea.Bottom - notificationHeight - margin - (existingNotifications.Count * (notificationHeight + spacing));

            Debug.WriteLine($"Positioned at: Left={this.Left}, Top={this.Top}");
        }

        private void SetupAutoCloseTimer()
        {
            _autoCloseTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(_totalSeconds) };
            _autoCloseTimer.Tick += (sender, e) =>
            {
                _autoCloseTimer.Stop();
                CloseNotification();
            };
        }

        private void SetupProgressTimer()
        {
            _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _progressTimer.Tick += (sender, e) =>
            {
                _currentSecond += 100;
                double progress = (double)_currentSecond / (_totalSeconds * 1000) * 100;
                ProgressBar.Value = progress;

                if (_currentSecond >= _totalSeconds * 1000)
                {
                    _progressTimer.Stop();
                }
            };
        }

        private void StartFadeInAnimation()
        {
            var fadeInStoryboard = (Storyboard)this.Resources["FadeInStoryboard"];
            fadeInStoryboard.Begin(this);
            _autoCloseTimer.Start();
            _progressTimer.Start();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseNotification();
        }

        private void CloseNotification()
        {
            if (_isClosing) return;
            _isClosing = true;
            _autoCloseTimer?.Stop();
            _progressTimer?.Stop();
            var fadeOutStoryboard = (Storyboard)this.Resources["FadeOutStoryboard"];
            fadeOutStoryboard.Begin(this);
        }

        private void FadeOut_Completed(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            _autoCloseTimer?.Stop();
            _progressTimer?.Stop();
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!_isClosing)
            {
                _autoCloseTimer?.Start();
                _progressTimer?.Start();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            // Hủy đăng ký sự kiện để tránh rò rỉ bộ nhớ
            this.MouseLeftButtonDown -= NotificationWindow_MouseLeftButtonDown;
            RepositionRemainingNotifications();
        }

        /// <summary>
        /// Sắp xếp lại các thông báo còn lại một cách mượt mà.
        /// </summary>
        private void RepositionRemainingNotifications()
        {
            try
            {
                var notifications = Application.Current.Windows.OfType<NotificationWindow>()
                                         .Where(w => w.IsVisible && w != this)
                                         .OrderBy(w => w.Top)
                                         .ToList();

                var workArea = SystemParameters.WorkArea;
                const double margin = 20.0;
                const double spacing = 10.0;

                for (int i = 0; i < notifications.Count; i++)
                {
                    var notif = notifications[i];
                    var notificationHeight = notif.ActualHeight;

                    var targetTop = workArea.Bottom - notificationHeight - margin - (i * (notificationHeight + spacing));

                    // Animate to new position
                    var animation = new DoubleAnimation(targetTop, TimeSpan.FromMilliseconds(300))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    notif.BeginAnimation(Window.TopProperty, animation);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error repositioning notifications: {ex.Message}");
            }
        }

        public static void CloseAllNotifications()
        {
            foreach (var window in Application.Current.Windows.OfType<NotificationWindow>().ToList())
            {
                window.CloseNotification();
            }
        }
    }
}
