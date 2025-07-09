using System.Windows;
using WpfApp1.ViewModels;

namespace WpfApp1.Views
{
    /// <summary>
    /// Interaction logic for CallWindow.xaml
    /// </summary>
    public partial class CallWindow : Window
    {
        public CallWindow(CallViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            // Đóng cửa sổ khi ViewModel yêu cầu
            viewModel.RequestClose += (s, e) =>
            {
                // Đảm bảo đóng trên luồng UI
                Dispatcher.Invoke(() => this.Close());
            };
        }
    }
}
