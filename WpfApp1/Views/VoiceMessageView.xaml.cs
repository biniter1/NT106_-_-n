using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NAudio.Wave;

namespace WpfApp1.Views
{
    public partial class VoiceMessageView : UserControl
    {
        private WaveOutEvent audioPlayer;
        private MediaFoundationReader audioReader;
        private DispatcherTimer timer;

        // Dependency Property cho AudioUrl
        public static readonly DependencyProperty AudioUrlProperty =
            DependencyProperty.Register("AudioUrl", typeof(string), typeof(VoiceMessageView), new PropertyMetadata(null, OnAudioUrlChanged));

        public string AudioUrl
        {
            get { return (string)GetValue(AudioUrlProperty); }
            set { SetValue(AudioUrlProperty, value); }
        }

        // Dependency Property cho Duration
        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register("Duration", typeof(double), typeof(VoiceMessageView), new PropertyMetadata(0.0, OnDurationChanged));

        public double Duration
        {
            get { return (double)GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
        }

        public VoiceMessageView()
        {
            InitializeComponent();
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            timer.Tick += Timer_Tick;
        }

        private static void OnAudioUrlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Có thể xử lý gì đó khi URL thay đổi nếu cần
        }

        private static void OnDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as VoiceMessageView;
            if (control != null && e.NewValue is double seconds)
            {
                control.DurationText.Text = TimeSpan.FromSeconds(seconds).ToString(@"m\:ss");
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (audioPlayer != null && audioPlayer.PlaybackState == PlaybackState.Playing)
            {
                audioPlayer.Pause();
                PlayIcon.Text = ""; // Play icon
            }
            else if (audioPlayer != null && audioPlayer.PlaybackState == PlaybackState.Paused)
            {
                audioPlayer.Play();
                PlayIcon.Text = ""; // Pause icon
            }
            else // Stopped or not initialized
            {
                if (string.IsNullOrEmpty(AudioUrl)) return;

                CleanupPlayer(); // Dọn dẹp player cũ nếu có

                audioPlayer = new WaveOutEvent();
                audioReader = new MediaFoundationReader(AudioUrl);

                audioPlayer.Init(audioReader);
                audioPlayer.PlaybackStopped += (s, a) =>
                {
                    CleanupPlayer();
                    ProgressSlider.Value = 0;
                    PlayIcon.Text = ""; // Play icon
                    timer.Stop();
                };

                ProgressSlider.Maximum = audioReader.TotalTime.TotalSeconds;
                audioPlayer.Play();
                PlayIcon.Text = ""; // Pause icon
                timer.Start();
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (audioReader != null)
            {
                ProgressSlider.Value = audioReader.CurrentTime.TotalSeconds;
            }
        }

        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (audioReader != null && Math.Abs(audioReader.CurrentTime.TotalSeconds - e.NewValue) > 0.5)
            {
                audioReader.CurrentTime = TimeSpan.FromSeconds(e.NewValue);
            }
        }

        private void CleanupPlayer()
        {
            audioPlayer?.Dispose();
            audioPlayer = null;
            audioReader?.Dispose();
            audioReader = null;
        }
    }
}