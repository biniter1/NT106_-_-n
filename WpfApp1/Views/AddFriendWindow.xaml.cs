using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WpfApp1.ViewModels;

namespace WpfApp1.Views
{
    /// <summary>
    /// Interaction logic for AddFriendWindow.xaml
    /// </summary>
    public partial class AddFriendWindow : Window
    {
        public AddFriendWindow()
        {
            InitializeComponent();
            var viewModel = new AddFriendViewModel();
            this.DataContext = viewModel;
            LocalizationManager.LanguageChanged += OnLanguageChanged;
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
