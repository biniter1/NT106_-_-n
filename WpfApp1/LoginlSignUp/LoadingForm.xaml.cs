using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace WpfApp1.LoginlSignUp
{
    public partial class LoadingForm : Window
    {
        public LoadingForm()
        {
            InitializeComponent();
            // Fade in animation on load
            Loaded += (s, e) =>
            {
                DoubleAnimation fadeIn = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.3)
                };
                this.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            };
        }

        public void CloseWithFadeOut()
        {
            DoubleAnimation fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3)
            };
            fadeOut.Completed += (s, e) => this.Close();
            this.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
    }
}