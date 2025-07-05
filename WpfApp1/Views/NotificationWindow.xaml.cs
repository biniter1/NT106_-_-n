using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace WpfApp1.Views
{
    public partial class NotificationWindow : Window
    {
        private DispatcherTimer _autoCloseTimer;
        private DispatcherTimer _progressTimer;
        private int _totalSeconds = 5;
        private int _currentSecond = 0;
        private bool _isClosing = false;

        public NotificationWindow(string title, string message)
        {
            InitializeComponent();

            // Set content
            TitleText.Text = title;
            MessageText.Text = message;

            // Position window
            PositionWindow();

            // Setup timers
            SetupAutoCloseTimer();
            SetupProgressTimer();

            // Show with animation
            this.Loaded += (s, e) => StartFadeInAnimation();

            Debug.WriteLine($"NotificationWindow created: {title} - {message}");
        }

        private void PositionWindow()
        {
            // Position at bottom-right corner of screen
            var workArea = SystemParameters.WorkArea;

            // Calculate position (accounting for potential multiple notifications)
            var notificationHeight = 130; // Estimated height including margin
            var existingNotifications = GetExistingNotificationsCount();

            this.Left = workArea.Right - 370; // 350 width + 20 margin
            this.Top = workArea.Bottom - 130 - 20 - (existingNotifications * notificationHeight); // Bottom position

            Debug.WriteLine($"Positioned at: Left={this.Left}, Top={this.Top}");
        }

        private int GetExistingNotificationsCount()
        {
            // Count existing notification windows
            int count = 0;
            foreach (Window window in Application.Current.Windows)
            {
                if (window is NotificationWindow && window != this && window.IsVisible)
                {
                    count++;
                }
            }
            return count;
        }

        private void SetupAutoCloseTimer()
        {
            _autoCloseTimer = new DispatcherTimer();
            _autoCloseTimer.Interval = TimeSpan.FromSeconds(_totalSeconds);
            _autoCloseTimer.Tick += (sender, e) =>
            {
                Debug.WriteLine("Auto-close timer triggered");
                _autoCloseTimer.Stop();
                CloseNotification();
            };
        }

        private void SetupProgressTimer()
        {
            _progressTimer = new DispatcherTimer();
            _progressTimer.Interval = TimeSpan.FromMilliseconds(100); // Update every 100ms for smooth progress
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

            // Start timers after fade in
            _autoCloseTimer.Start();
            _progressTimer.Start();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Close button clicked");
            CloseNotification();
        }

        private void CloseNotification()
        {
            if (_isClosing) return;

            _isClosing = true;
            Debug.WriteLine("Closing notification with animation");

            // Stop timers
            _autoCloseTimer?.Stop();
            _progressTimer?.Stop();

            // Start fade out animation
            var fadeOutStoryboard = (Storyboard)this.Resources["FadeOutStoryboard"];
            fadeOutStoryboard.Begin(this);
        }

        private void FadeOut_Completed(object sender, EventArgs e)
        {
            Debug.WriteLine("Fade out animation completed, closing window");
            this.Close();
        }

        private void Window_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Debug.WriteLine("Mouse entered notification - pausing timers");
            _autoCloseTimer?.Stop();
            _progressTimer?.Stop();
        }

        private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Debug.WriteLine("Mouse left notification - resuming timers");
            if (!_isClosing)
            {
                _autoCloseTimer?.Start();
                _progressTimer?.Start();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            Debug.WriteLine("NotificationWindow closed");

            // Cleanup timers
            _autoCloseTimer?.Stop();
            _progressTimer?.Stop();
            _autoCloseTimer = null;
            _progressTimer = null;

            // Reposition remaining notifications
            RepositionRemainingNotifications();

            base.OnClosed(e);
        }

        private void RepositionRemainingNotifications()
        {
            try
            {
                var notifications = new System.Collections.Generic.List<NotificationWindow>();

                // Collect all remaining notification windows
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is NotificationWindow notif && window != this && window.IsVisible)
                    {
                        notifications.Add(notif);
                    }
                }

                // Reposition them (from bottom up)
                var workArea = SystemParameters.WorkArea;
                var notificationHeight = 130;

                for (int i = 0; i < notifications.Count; i++)
                {
                    var targetTop = workArea.Bottom - 130 - 20 - (i * notificationHeight);

                    // Animate to new position
                    var storyboard = new Storyboard();
                    var animation = new DoubleAnimation
                    {
                        To = targetTop,
                        Duration = TimeSpan.FromMilliseconds(300),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };

                    Storyboard.SetTarget(animation, notifications[i]);
                    Storyboard.SetTargetProperty(animation, new PropertyPath("Top"));
                    storyboard.Children.Add(animation);
                    storyboard.Begin();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error repositioning notifications: {ex.Message}");
            }
        }

        // Method to manually close all notifications
        public static void CloseAllNotifications()
        {
            try
            {
                var notificationsToClose = new System.Collections.Generic.List<NotificationWindow>();

                foreach (Window window in Application.Current.Windows)
                {
                    if (window is NotificationWindow notif && window.IsVisible)
                    {
                        notificationsToClose.Add(notif);
                    }
                }

                foreach (var notification in notificationsToClose)
                {
                    notification.CloseNotification();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error closing all notifications: {ex.Message}");
            }
        }
    }
}