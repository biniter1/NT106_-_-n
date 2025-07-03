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
            LocalizationManager.LanguageChanged += OnLanguageChanged;
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
        private void OnLanguageChanged(object sender, EventArgs e)
        {
            // Force the UI to refresh bindings
            InvalidateVisual();
            // Optionally, update specific bindings
            UpdateBindings();
        }
        private void UpdateBindings()
        {
            // Update bindings for controls that use localized strings
            foreach (var element in LogicalTreeHelper.GetChildren(this))
            {
                if (element is FrameworkElement fe)
                {
                    fe.GetBindingExpression(FrameworkElement.DataContextProperty)?.UpdateTarget();
                    // Update other bindings as needed
                }
            }
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