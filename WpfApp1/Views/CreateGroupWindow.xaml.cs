using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using WpfApp1.Models;
using WpfApp1.ViewModels;

namespace WpfApp1.Views
{
    public class SelectableFriend : INotifyPropertyChanged
    {
        private bool _isSelected;
        
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public partial class CreateGroupWindow : Window, INotifyPropertyChanged
    {
        private string _groupName = string.Empty;
        private string _groupDescription = string.Empty;
        private string _selectedAvatarPath = string.Empty;
        private ObservableCollection<SelectableFriend> _availableFriends = new();
        private ObservableCollection<SelectableFriend> _filteredFriends = new();

        public string GroupName
        {
            get => _groupName;
            set
            {
                _groupName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanCreateGroup));
                UpdateGroupNameValidation();
            }
        }

        public string GroupDescription
        {
            get => _groupDescription;
            set
            {
                _groupDescription = value;
                OnPropertyChanged();
            }
        }

        public string SelectedAvatarPath
        {
            get => _selectedAvatarPath;
            set
            {
                _selectedAvatarPath = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<SelectableFriend> AvailableFriends
        {
            get => _filteredFriends;
            set
            {
                _filteredFriends = value;
                OnPropertyChanged();
            }
        }

        public bool CanCreateGroup => !string.IsNullOrWhiteSpace(GroupName);

        public CreateGroupWindow()
        {
            InitializeComponent();
            DataContext = this;
            
            // Load friends data
            _ = LoadFriendsAsync();
            
            // Set focus to group name textbox after window loads
            Loaded += (s, e) => GroupNameTextBox.Focus();
        }

        private async Task LoadFriendsAsync()
        {
            try
            {
                var friendListVM = new FriendListViewModel();
                var friends = await friendListVM.LoadFriendsAsync(SharedData.Instance.userdata.Email);
                
                _availableFriends.Clear();
                foreach (var friend in friends)
                {
                    _availableFriends.Add(new SelectableFriend
                    {
                        Email = friend.Email,
                        Name = friend.Name,
                        AvatarUrl = friend.AvatarUrl
                    });
                }
                
                // Initially show all friends
                AvailableFriends = new ObservableCollection<SelectableFriend>(_availableFriends);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể tải danh sách bạn bè: {ex.Message}", "Lỗi", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SearchFriendsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchFriendsTextBox.Text?.ToLower() ?? string.Empty;
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                AvailableFriends = new ObservableCollection<SelectableFriend>(_availableFriends);
            }
            else
            {
                var filtered = _availableFriends.Where(f => 
                    f.Name.ToLower().Contains(searchText) || 
                    f.Email.ToLower().Contains(searchText)).ToList();
                AvailableFriends = new ObservableCollection<SelectableFriend>(filtered);
            }
        }

        public List<string> GetSelectedFriendEmails()
        {
            return _availableFriends.Where(f => f.IsSelected).Select(f => f.Email).ToList();
        }

        private void UpdateGroupNameValidation()
        {
            if (string.IsNullOrWhiteSpace(GroupName) && !string.IsNullOrEmpty(GroupNameTextBox?.Text))
            {
                GroupNameError.Visibility = Visibility.Visible;
            }
            else
            {
                GroupNameError.Visibility = Visibility.Collapsed;
            }
        }

        private void AvatarButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Chọn ảnh đại diện nhóm",
                Filter = "Image files (*.jpg;*.jpeg;*.png;*.gif;*.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp|All files (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.DecodePixelWidth = 200; // Optimize for display
                    bitmap.EndInit();

                    AvatarImage.Source = bitmap;
                    AvatarImage.Visibility = Visibility.Visible;
                    AvatarPlaceholder.Visibility = Visibility.Collapsed;
                    
                    SelectedAvatarPath = openFileDialog.FileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể tải ảnh: {ex.Message}", "Lỗi", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(GroupName))
            {
                GroupNameError.Visibility = Visibility.Visible;
                GroupNameTextBox.Focus();
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}