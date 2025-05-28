using System.Windows;
using WpfApp1.ViewModels;

namespace WpfApp1.Views
{
    public partial class EditProfileWindow : Window
    {
        public EditProfileWindow()
        {
            InitializeComponent();
            LocalizationManager.LanguageChanged += OnLanguageChanged;
            // Subscribe to profile updated event
            if (DataContext is EditProfileViewModel viewModel)
            {
                viewModel.ProfileUpdated += OnProfileUpdated;
            }
        }

        private void OnProfileUpdated(bool success)
        {
            if (success)
            {
                DialogResult = true;
            }
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            // Unsubscribe from events
            if (DataContext is EditProfileViewModel viewModel)
            {
                viewModel.ProfileUpdated -= OnProfileUpdated;
            }
            base.OnClosed(e);
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
    }
}