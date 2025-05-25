using System.Windows;
using System.Windows.Controls;

namespace WpfApp1.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.Tag is string language)
            {
                // Assuming the DataContext is the ViewModel with SelectedLanguage and ApplyLanguageCommand
                if (DataContext is WpfApp1.ViewModels.SettingsViewModel viewModel)
                {
                    // Sử dụng method mới để xử lý thay đổi ngôn ngữ
                    viewModel.OnLanguageChanged(language);
                }
            }
        }
    }
}