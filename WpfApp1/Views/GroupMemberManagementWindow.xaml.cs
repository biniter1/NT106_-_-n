using System;
using System.Windows;
using WpfApp1.ViewModels;

namespace WpfApp1.Views
{
    public partial class GroupMemberManagementWindow : Window
    {
        public GroupMemberManagementWindow(GroupMemberManagementViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            // Set window properties
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}