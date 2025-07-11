﻿using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Emoji.Wpf;
using WpfApp1.ViewModels;
namespace WpfApp1.Views
{
    /// <summary>
    /// Interaction logic for ChatView.xaml
    /// </summary>
    public partial class ChatView : UserControl
    {
        public ChatView()
        {
            InitializeComponent();
            LocalizationManager.LanguageChanged += OnLanguageChanged;
            this.DataContextChanged += ChatView_DataContextChanged;

        }
        private void ChatView_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            // Kiểm tra nếu ViewModel cũ tồn tại thì hủy đăng ký
            if (e.OldValue is ChatViewModel oldViewModel)
            {
                oldViewModel.ScrollToBottomRequested -= OnScrollToBottomRequested;
            }

            // Kiểm tra nếu ViewModel mới tồn tại thì đăng ký sự kiện
            if (e.NewValue is ChatViewModel newViewModel)
            {
                newViewModel.ScrollToBottomRequested += OnScrollToBottomRequested;
            }
        }

        private void OnScrollToBottomRequested(object sender, EventArgs e)
        {
            // Hành động cuộn ScrollViewer xuống dưới cùng
            MessagesScrollViewer.ScrollToBottom();
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
        private void Picker_Picked(object sender, Emoji.Wpf.EmojiPickedEventArgs e)
        {
            // Get the ViewModel
            var viewModel = DataContext as ViewModels.ChatViewModel;
            if (viewModel != null)
            {
                // Append the selected emoji to the current message text
                viewModel.NewMessageText += e.Emoji;
            }
        }
    }
}
