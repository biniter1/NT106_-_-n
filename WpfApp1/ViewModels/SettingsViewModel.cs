using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using WpfApp1.LoginlSignUp;
using System.Windows.Markup;
using System.Windows.Input;
using WpfApp1.Models;
using Firebase.Database;
using System.Diagnostics;
using Firebase.Database.Query;
using Microsoft.Win32;
using System.Text;
using System.IO;

namespace WpfApp1.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly FirebaseClient _firebaseClient;
        private readonly ChatViewModel _chatViewModel;

        [ObservableProperty]
        private string _currentView;

        [ObservableProperty]
        private object _currentViewContent;

        [ObservableProperty]
        private string _userName = SharedData.Instance.userdata.Name;

        [ObservableProperty]
        private string _userEmail = SharedData.Instance.userdata.Email;

        [ObservableProperty]
        private DateTime _joinDate = SharedData.Instance.userdata.DateTime;

        [ObservableProperty]
        private string _selectedTheme;

        [ObservableProperty]
        private bool _enableNotifications;

        [ObservableProperty]
        private bool _showDesktopNotifications;

        [ObservableProperty]
        private bool _notifyWhenOffline;

        [ObservableProperty]
        private string _selectedLanguage;

        [ObservableProperty]
        private string _userAvatarUrl;

        public ObservableCollection<string> Themes { get; } = new ObservableCollection<string> { "Light", "Dark" };
        public ObservableCollection<string> Languages { get; } = new ObservableCollection<string> { "Tiếng Việt", "English", "日本語" };

        public SettingsViewModel(ChatViewModel chatViewModel, FirebaseClient firebaseClient)
        {
            _chatViewModel = chatViewModel;
            _firebaseClient = firebaseClient;
            InitializeSettings();
            EditProfileViewModel.AvatarUpdated += OnAvatarUpdated;
        }

        //public SettingsViewModel()
        //{
        //    InitializeSettings();
        //    EditProfileViewModel.AvatarUpdated += OnAvatarUpdated;
        //}

        private void InitializeSettings()
        {
            _selectedTheme = "Light";
            _enableNotifications = true;
            _showDesktopNotifications = true;
            _notifyWhenOffline = false;
            _selectedLanguage = Properties.Settings.Default.SelectedLanguage ;
            _currentView = "AccountInfo";
            _userAvatarUrl = SharedData.Instance.userdata.AvatarUrl ?? "";
            var savedLanguage = Properties.Settings.Default.SelectedLanguage;
            _selectedLanguage = savedLanguage switch
            {
                "Tiếng Việt" => "Tiếng Việt",
                "English" => "English",
                _ => "Tiếng Việt"
            };
        }

        private void OnAvatarUpdated(string newAvatarUrl)
        {
            UserAvatarUrl = newAvatarUrl;
            OnPropertyChanged(nameof(UserAvatarUrl));
        }

        private string GetLocalizedString(string key)
        {
            if (Application.Current.Resources["LocalizationDictionary"] is ResourceDictionary localizationDict)
            {
                return localizationDict.Contains(key) ? localizationDict[key].ToString() : key;
            }
            return key;
        }

        [RelayCommand]
        private void ShowAccountInfo()
        {
            CurrentView = "AccountInfo";
        }

        [RelayCommand]
        private void ShowSettings()
        {
            CurrentView = "Settings";
        }

        [RelayCommand]
        private void ShowData()
        {
            CurrentView = "Data";
        }

        [RelayCommand]
        private void ShowLanguage()
        {
            CurrentView = "Language";
        }

        [RelayCommand]
        private void ShowSupport()
        {
            CurrentView = "Support";
        }

        [RelayCommand]
        private void Logout()
        {
            _chatViewModel?.Cleanup();
            MessageBox.Show(GetLocalizedString("LogoutSuccessMessage"));
            var loginWindow = new fLogin();
            loginWindow.PerformLogout();
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    window.Close();
                    break;
                }
            }
        }

        [RelayCommand]
        private void Exit()
        {
            Application.Current.Shutdown();
        }

        [RelayCommand]
        private void EditProfile()
        {
            try
            {
                var editProfileViewModel = new EditProfileViewModel();
                editProfileViewModel.ProfileUpdated += OnProfileUpdated;
                var editProfileWindow = new Views.EditProfileWindow
                {
                    Owner = Application.Current.MainWindow,
                    DataContext = editProfileViewModel
                };
                bool? result = editProfileWindow.ShowDialog();
                if (result == true)
                {
                    MessageBox.Show(GetLocalizedString("ProfileUpdatedMessage") ?? "Profile updated successfully!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening edit profile: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnProfileUpdated(bool success)
        {
            if (success)
            {
                UserName = SharedData.Instance.userdata.Name;
                UserEmail = SharedData.Instance.userdata.Email;
                OnPropertyChanged(nameof(UserName));
                OnPropertyChanged(nameof(UserEmail));
            }
        }

        [RelayCommand]
        private void SaveSettings()
        {
            MessageBox.Show(GetLocalizedString("SettingsSavedMessage") +
                           $" Theme={SelectedTheme}, Notifications={EnableNotifications}, Desktop Notifications={ShowDesktopNotifications}, Notify Offline={NotifyWhenOffline}, Language={SelectedLanguage}");
        }

        [RelayCommand]
        private async void ExportData()
        {
            try
            {
                string userEmail = SharedData.Instance.userdata.Email;
                if (string.IsNullOrEmpty(userEmail))
                {
                    MessageBox.Show(GetLocalizedString("ExportDataError_UserNotFound"),
                                    GetLocalizedString("Error"),
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                    return;
                }

                // Get all contacts to get all chat IDs
                var contacts = await _chatViewModel.GetContactsAsync(userEmail);
                if (contacts == null || contacts.Count == 0)
                {
                    MessageBox.Show(GetLocalizedString("NoContactsFoundForExport"),
                                    GetLocalizedString("Info"),
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                    return;
                }

                var saveFileDialog = new SaveFileDialog
                {
                    FileName = $"ChatExport_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                    Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    Title = GetLocalizedString("SaveChatExportTitle")
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    Mouse.OverrideCursor = Cursors.Wait; // Show busy cursor
                    try
                    {
                        var stringBuilder = new StringBuilder();
                        stringBuilder.AppendLine($"--- Chat Export for {userEmail} ---");
                        stringBuilder.AppendLine($"Export Date: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                        stringBuilder.AppendLine("------------------------------------");
                        stringBuilder.AppendLine();

                        foreach (var contact in contacts)
                        {
                            if (!string.IsNullOrEmpty(contact.chatID))
                            {
                                stringBuilder.AppendLine($"### Chat with {contact.Name} ({contact.Email}) - Chat ID: {contact.chatID} ###");
                                stringBuilder.AppendLine("------------------------------------");

                                // Fetch messages for this chatID
                                var messagesQuery = await _firebaseClient
                                    .Child("messages")
                                    .Child(contact.chatID)
                                    .OrderBy("Timestamp") // Order by timestamp to get chronological order
                                    .OnceAsync<Message>();

                                var chatMessages = messagesQuery
                                    .Select(item => item.Object)
                                    .OrderBy(m => m.Timestamp) // Re-order in case Firebase query doesn't guarantee order
                                    .ToList();

                                if (chatMessages.Any())
                                {
                                    foreach (var message in chatMessages)
                                    {
                                        string senderName = (message.SenderId == userEmail) ? "You" : contact.Name;
                                        string messageContent = message.Content;

                                        if (message.IsImage && !string.IsNullOrEmpty(message.ImageUrl))
                                        {
                                            messageContent = $"[Image: {message.ImageUrl}] {message.Content}";
                                        }
                                        else if (message.IsVideo && !string.IsNullOrEmpty(message.VideoUrl))
                                        {
                                            messageContent = $"[Video: {message.VideoUrl}] {message.Content}";
                                        }
                                        else if (!string.IsNullOrEmpty(message.FileUrl))
                                        {
                                            messageContent = $"[File: {message.FileUrl}] {message.Content}";
                                        }

                                        stringBuilder.AppendLine($"[{message.Timestamp:dd/MM/yyyy HH:mm:ss}] {senderName}: {messageContent}");
                                    }
                                }
                                else
                                {
                                    stringBuilder.AppendLine("No messages found in this chat.");
                                }
                                stringBuilder.AppendLine(); // Add a blank line for separation
                            }
                        }

                        File.WriteAllText(saveFileDialog.FileName, stringBuilder.ToString());
                        MessageBox.Show(GetLocalizedString("ExportDataSuccess") + $" {saveFileDialog.FileName}",
                                        GetLocalizedString("Success"),
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error exporting data: {ex.Message}");
                        MessageBox.Show($"{GetLocalizedString("ExportDataError")}: {ex.Message}",
                                        GetLocalizedString("Error"),
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Error);
                    }
                    finally
                    {
                        Mouse.OverrideCursor = null; // Restore cursor
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting up export: {ex.Message}");
                MessageBox.Show($"{GetLocalizedString("ExportDataError_General")}: {ex.Message}",
                                GetLocalizedString("Error"),
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }


        [RelayCommand]
        private async void DeleteData()
        {
            var result = MessageBox.Show(GetLocalizedString("DeleteDataConfirmation"),
                                         GetLocalizedString("Confirmation"),
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                Mouse.OverrideCursor = Cursors.Wait; // Show busy cursor
                try
                {
                    string userEmail = SharedData.Instance.userdata.Email;
                    if (string.IsNullOrEmpty(userEmail))
                    {
                        MessageBox.Show(GetLocalizedString("DataDeletionError_UserNotFound"),
                                        GetLocalizedString("Error"),
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Error);
                        return;
                    }

                    if (_chatViewModel == null)
                    {
                        MessageBox.Show("ChatViewModel is not initialized.",
                                        "Error",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Error);
                        return;
                    }

                    // Get all contacts to identify chat IDs for deletion
                    var contacts = await _chatViewModel.GetContactsAsync(userEmail);
                    if (contacts == null || contacts.Count == 0)
                    {
                        MessageBox.Show(GetLocalizedString("NoContactsFoundToDelete"),
                                        GetLocalizedString("Info"),
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Information);
                        return;
                    }

                    foreach (var contact in contacts)
                    {
                        if (!string.IsNullOrEmpty(contact.chatID))
                        {
                            // Delete messages associated with this chat ID
                            await _firebaseClient
                                .Child($"messages/{contact.chatID}")
                                .DeleteAsync();
                            Debug.WriteLine($"Deleted messages for chatID: {contact.chatID}");

                            // Optionally, if contacts are also stored in Firebase Realtime Database
                            // and you want to remove the chat ID from the user's contacts list,
                            // you might need additional logic here depending on your Firebase structure.
                            // For Firestore contacts, you would remove the document:
                            // await FirestoreHelper.database.Collection("users").Document(userEmail).Collection("contacts").Document(contact.Email).DeleteAsync();
                        }
                    }

                    // Clear messages from the UI
                    if (_chatViewModel?.Messages != null)
                    {
                        _chatViewModel.Messages.Clear();
                    }
                    if (_chatViewModel?.Files != null)
                    {
                        _chatViewModel.Files.Clear();
                    }

                    MessageBox.Show(GetLocalizedString("DataDeletedMessage"),
                                    GetLocalizedString("Success"),
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error deleting data: {ex.Message}");
                    MessageBox.Show($"{GetLocalizedString("DataDeletionError")}: {ex.Message}",
                                    GetLocalizedString("Error"),
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                }
                finally
                {
                    Mouse.OverrideCursor = null; // Restore cursor
                }
            }
        }

        [RelayCommand]
        private void SendFeedback()
        {
            try
            {
                var feedbackViewModel = new FeedbackViewModel();
                var feedbackUserControl = new Views.FeedbackUserControl
                {
                    DataContext = feedbackViewModel
                };

                // Tạo Window để chứa UserControl
                var feedbackWindow = new Window
                {
                    Title = GetLocalizedString("FeedbackFormTitle"),
                    Content = feedbackUserControl,
                    Owner = Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    ResizeMode = ResizeMode.NoResize,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStyle = WindowStyle.SingleBorderWindow
                };

                // Xử lý sự kiện đóng window từ ViewModel
                feedbackViewModel.CloseRequested += () => feedbackWindow.Close();

                feedbackWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{GetLocalizedString("FeedbackErrorMessage")}: {ex.Message}",
                              GetLocalizedString("Error"),
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ShowUserGuide()
        {
            try
            {
                var userGuideViewModel = new UserGuideViewModel(this);
                var userGuideWindow = new Views.UserGuideContainerWindow
                {
                    Owner = Application.Current.MainWindow,
                    DataContext = userGuideViewModel
                };
                userGuideWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{GetLocalizedString("UserGuideErrorMessage")}: {ex.Message}",
                              GetLocalizedString("Error"),
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ApplyLanguage()
        {
            try
            {
                ApplyLanguageGlobally(SelectedLanguage);
                MessageBox.Show($"Language applied: {SelectedLanguage}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying language: {ex.Message}");
            }
        }

        [RelayCommand]
        private void ChangeLanguage(string languageCode)
        {
            try
            {
                ApplyLanguageGlobally(languageCode);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error changing language: {ex.Message}");
            }
        }

        private void ApplyLanguageGlobally(string language)
        {
            try
            {
                string resourcePath = language switch
                {
                    "vi-VN" => "Resources/StringResources.vi.xaml",
                    "en-US" => "Resources/StringResources.en.xaml",
                    "Tiếng Việt" => "Resources/StringResources.vi.xaml",
                    "English" => "Resources/StringResources.en.xaml",
                    _ => "Resources/StringResources.vi.xaml"
                };

                var app = Application.Current;
                if (app == null) return;

                ResourceDictionary newResourceDict;
                try
                {
                    newResourceDict = new ResourceDictionary
                    {
                        Source = new Uri(resourcePath, UriKind.Relative)
                    };
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Could not load resource: {resourcePath}. Error: {ex.Message}");
                    return;
                }

                ResourceDictionary oldDict = null;
                foreach (var dict in app.Resources.MergedDictionaries)
                {
                    if (dict.Source != null && dict.Source.ToString().Contains("StringResources"))
                    {
                        oldDict = dict;
                        break;
                    }
                }

                if (oldDict != null)
                {
                    app.Resources.MergedDictionaries.Remove(oldDict);
                }

                app.Resources.MergedDictionaries.Add(newResourceDict);
                Properties.Settings.Default.SelectedLanguage = language;
                Properties.Settings.Default.Save();
                NotifyLanguageChanged();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ApplyLanguageGlobally: {ex.Message}");
                throw;
            }
        }

        private void NotifyLanguageChanged()
        {
            try
            {
                Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                {
                    OnPropertyChanged(nameof(SelectedLanguage));
                    foreach (Window window in Application.Current.Windows)
                    {
                        window?.InvalidateVisual();
                    }
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error notifying language change: {ex.Message}");
            }
        }

        public void OnLanguageChanged(string language)
        {
            if (SelectedLanguage != language)
            {
                SelectedLanguage = language;
                ApplyLanguageGlobally(language);
            }
        }

        public bool HasLanguageChanges => true;
        public bool HasChanges => true;
    }
}