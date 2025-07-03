using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace WpfApp1.LoginlSignUp
{
    /// <summary>
    /// Interaction logic for SuccessPopup.xaml
    /// </summary>
    public partial class SuccessPopup : Window
    {
        public SuccessPopup()
        {
            InitializeComponent();
            LocalizationManager.LanguageChanged += OnLanguageChanged;
            // Enable dragging the window
            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                    this.DragMove();
            };
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
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Fade out animation before closing
            DoubleAnimation fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3)
            };
            fadeOut.Completed += (s, _) => this.Close();
            this.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
    }
}