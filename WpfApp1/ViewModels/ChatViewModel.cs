using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using WpfApp1.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Windows;
using System.Text;
using Firebase.Database;
using Firebase.Database.Query;
using System.Windows.Media;
using Google.Cloud.Firestore;
using Firebase.Database.Streaming;
using System.Security.Cryptography;
using System.Reactive.Linq;
using Microsoft.Win32;
using System.IO;
using WpfApp1; // Ensure FirebaseStorageHelper namespace is included

namespace WpfApp1.ViewModels
{
    public partial class ChatViewModel : ObservableObject
    {
        public ObservableCollection<Message> Messages { get; set; }
        public ObservableCollection<Contact> Contacts { get; set; }
        public ObservableCollection<FileItem> Files { get; set; }

        [ObservableProperty]
        private string _newMessageText;

        private IDisposable messageSubscription;
        private FirebaseClient firebaseClient;
        private string currentUserId;

        [ObservableProperty]
        private Contact _selectedContact;

        public User userdata;
        private List<IDisposable> allMessageSubscriptions = new List<IDisposable>();
        private Dictionary<string, IDisposable> _contactPresenceListeners = new Dictionary<string, IDisposable>();

        partial void OnSelectedContactChanged(Contact value)
        {
            SendMessageCommand.NotifyCanExecuteChanged();

            if (value != null)
            {
                LoadMessagesForContact(value);
            }
            else
            {
                Messages.Clear();
                Files.Clear();

                if (messageSubscription != null)
                {
                    messageSubscription.Dispose();
                    messageSubscription = null;
                }
            }
        }

        partial void OnNewMessageTextChanged(string value)
        {
            SendMessageCommand.NotifyCanExecuteChanged();
        }

        public ChatViewModel()
        {
            Messages = new ObservableCollection<Message>();
            Contacts = new ObservableCollection<Contact>();
            Files = new ObservableCollection<FileItem>();

            SelectedContact = null;
            NewMessageText = string.Empty;

            firebaseClient = new FirebaseClient("https://chatapp-177-default-rtdb.asia-southeast1.firebasedatabase.app/");

            userdata = SharedData.Instance.userdata;

            if (userdata != null && !string.IsNullOrEmpty(userdata.Email))
            {
                SetCurrentUserGlobalStatus(true);
            }
            else
            {
                Debug.WriteLine("ChatViewModel Constructor: Userdata not available yet for initial online status set.");
            }

            EditProfileViewModel.AvatarUpdated += OnAvatarUpdated;

            LoadInitialData();
        }
        [RelayCommand]
        private async void OnAvatarUpdated(string newAvatarUrl)
        {
            if (string.IsNullOrEmpty(newAvatarUrl)) return;

            try
            {
                // Cập nhật avatar cho người dùng hiện tại trong SharedData
                if (SharedData.Instance.userdata != null &&
                    !string.IsNullOrEmpty(userdata?.Email) &&
                    SharedData.Instance.userdata.Email == userdata.Email)
                {
                    SharedData.Instance.userdata.AvatarUrl = newAvatarUrl;
                }

                // Cập nhật avatar cho contact tương ứng trong danh sách Contacts
                var contactToUpdate = Contacts.FirstOrDefault(c => c.Email == userdata?.Email);
                if (contactToUpdate != null)
                {
                    contactToUpdate.IsLoadingAvatar = true;
                    contactToUpdate.AvatarUrl = newAvatarUrl;
                    contactToUpdate.IsLoadingAvatar = false;
                    Debug.WriteLine($"Updated avatar for contact {contactToUpdate.Name}: {newAvatarUrl}");
                }

                // Nếu người dùng hiện tại là contact đang được chọn, cập nhật lại UI
                if (SelectedContact != null && SelectedContact.Email == userdata?.Email)
                {
                    OnPropertyChanged(nameof(SelectedContact));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnAvatarUpdated: {ex.Message}");
            }
        }


        private async void LoadMessagesForContact(Contact contact)
        {
            Messages.Clear();

            if (messageSubscription != null)
            {
                messageSubscription.Dispose();
                messageSubscription = null;
            }

            if (contact == null || string.IsNullOrEmpty(contact.chatID))
            {
                Debug.WriteLine("Contact không hợp lệ hoặc không có chatID");
                return;
            }

            var roomId = contact.chatID;
            Debug.WriteLine($"Đang tải tin nhắn cho chatID: {roomId}");

            try
            {
                var messagesQuery = await firebaseClient
                    .Child("messages")
                    .Child(roomId)
                    .OrderByKey()
                    .LimitToLast(100)
                    .OnceAsync<Message>();

                if (messagesQuery != null)
                {
                    var oldMessages = messagesQuery
                        .Select(item =>
                        {
                            var msg = item.Object;
                            msg.Id = item.Key;
                            msg.IsMine = msg.SenderId == SharedData.Instance.userdata.Email;
                            msg.Alignment = msg.IsMine ? "Right" : "Left";
                            return msg;
                        })
                        .OrderBy(m => m.Timestamp)
                        .ToList();

                    Debug.WriteLine($"Đã tải {oldMessages.Count} tin nhắn cũ");

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var message in oldMessages)
                        {
                            Messages.Add(message);
                        }
                    });
                }

                messageSubscription = firebaseClient
                    .Child("messages")
                    .Child(roomId)
                    .AsObservable<Message>()
                    .Subscribe(messageEvent =>
                    {
                        if (messageEvent.EventType == FirebaseEventType.InsertOrUpdate && messageEvent.Object != null)
                        {
                            var message = messageEvent.Object;
                            message.Id = messageEvent.Key;
                            message.IsMine = message.SenderId == SharedData.Instance.userdata.Email;
                            message.Alignment = message.IsMine ? "Right" : "Left";

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                int existingIndex = Messages.IndexOf(Messages.FirstOrDefault(m => m.Id == message.Id));
                                if (existingIndex >= 0)
                                {
                                    Messages[existingIndex] = message;
                                    Debug.WriteLine($"Đã cập nhật tin nhắn: {message.Id}");
                                }
                                else if (!message.IsMine)
                                {
                                    int insertIndex = Messages.ToList().FindIndex(m => m.Timestamp > message.Timestamp) + 1;
                                    if (insertIndex < 0) insertIndex = 0;
                                    Messages.Insert(insertIndex, message);
                                    Debug.WriteLine($"Đã thêm tin nhắn mới: {message.Id} tại vị trí {insertIndex}");
                                    if (SelectedContact?.chatID != roomId)
                                    {
                                        string notificationMessage = $"Tin nhắn mới từ {SelectedContact?.Name ?? "Người gửi"}: {message.Content}";
                                        var mainWindow = Application.Current.MainWindow as MainWindow;
                                        mainWindow?.ShowNotification(notificationMessage);
                                    }
                                }
                            });
                        }
                        else if (messageEvent.EventType == FirebaseEventType.Delete)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                var messageToRemove = Messages.FirstOrDefault(m => m.Id == messageEvent.Key);
                                if (messageToRemove != null)
                                {
                                    Messages.Remove(messageToRemove);
                                    Debug.WriteLine($"Đã xóa tin nhắn: {messageEvent.Key}");
                                }
                            });
                        }
                    }, ex =>
                    {
                        Debug.WriteLine($"Lỗi khi lắng nghe tin nhắn: {ex.Message}");
                    });

                allMessageSubscriptions.Add(messageSubscription);
                LoadFilesForContact(contact);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi trong LoadMessagesForContact: {ex.Message}");
                MessageBox.Show($"Không thể tải tin nhắn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadFilesForContact(Contact contact)
        {
            Files.Clear();

            if (contact == null) return;

            // TODO: Implement logic to load files from Firebase Storage or Firestore
            // Example: Fetch files associated with the contact's chatID
        }

        [RelayCommand(CanExecute = nameof(CanSendMessage))]
        private async void SendMessage(Contact contact)
        {
            if (!string.IsNullOrWhiteSpace(NewMessageText))
            {
                var newMessage = new Message
                {
                    SenderId = SharedData.Instance.userdata.Email,
                    Content = NewMessageText,
                    Timestamp = DateTime.UtcNow,
                    IsMine = true,
                };
                await firebaseClient
                    .Child("messages")
                    .Child(contact.chatID)
                    .PostAsync(newMessage);

                Messages.Add(newMessage);
                NewMessageText = string.Empty;
            }
        }

        private bool CanSendMessage()
        {
            return !string.IsNullOrWhiteSpace(NewMessageText) && SelectedContact != null;
        }

        [RelayCommand]
        private async Task SelectFileAsync()
        {
            if (SelectedContact == null)
            {
                MessageBox.Show("Vui lòng chọn một liên hệ trước khi gửi tệp tin.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var openFileDialog = new OpenFileDialog
            {
                Title = "Chọn tệp để gửi",
                Filter = "Tất cả tệp|*.*|Hình ảnh|*.jpg;*.jpeg;*.png;*.gif|Tài liệu|*.pdf;*.docx;*.doc;*.xlsx;*.xls;*.ppt;*.pptx",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string selectedFilePath = openFileDialog.FileName;
                    string fileName = Path.GetFileName(selectedFilePath);
                    string fileExtension = Path.GetExtension(selectedFilePath).ToLower();

                    Mouse.OverrideCursor = Cursors.Wait;

                    try
                    {
                        string downloadUrl = await FirebaseStorageHelper.UploadFileAsync(selectedFilePath, fileName);

                        bool isImage = IsImageFile(fileExtension);

                        var newMessage = new Message
                        {
                            SenderId = SharedData.Instance.userdata.Email,
                            Content = isImage ? string.Empty : fileName,
                            Timestamp = DateTime.UtcNow,
                            IsMine = true,
                            IsImage = isImage,
                            ImageUrl = isImage ? downloadUrl : null,
                            Alignment = "Right"
                        };

                        var result = await firebaseClient
                            .Child("messages")
                            .Child(SelectedContact.chatID)
                            .PostAsync(newMessage);

                        newMessage.Id = result.Key;

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Messages.Add(newMessage);
                        });

                        if (!isImage)
                        {
                            var fileItem = new FileItem
                            {
                                IconPathOrType = fileExtension.TrimStart('.'),
                                FileName = fileName,
                                FileInfo = $"{FormatFileSize(new FileInfo(selectedFilePath).Length)} • {DateTime.Now:dd/MM/yyyy}",
                                FilePathOrUrl = downloadUrl,
                                DownloadUrl = downloadUrl
                            };

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Files.Add(fileItem);
                            });
                        }

                        Debug.WriteLine($"File uploaded successfully: {fileName}");
                    }
                    finally
                    {
                        Mouse.OverrideCursor = null;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error uploading file: {ex.Message}");
                    MessageBox.Show($"Không thể tải lên tệp tin: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool IsImageFile(string extension)
        {
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
            return Array.IndexOf(imageExtensions, extension.ToLower()) >= 0;
        }

        private string FormatFileSize(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (byteCount == 0) return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return $"{(Math.Sign(byteCount) * num)}{suf[place]}";
        }

        public async Task<List<Contact>> GetContactsAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                Debug.WriteLine("Email is null or empty!");
                return new List<Contact>();
            }

            try
            {
                var db = FirestoreHelper.database;
                if (db == null)
                {
                    Debug.WriteLine("Firestore database is not initialized!");
                    return new List<Contact>();
                }

                var userDocRef = db.Collection("users").Document(email);
                var contactsCollectionRef = userDocRef.Collection("contacts");
                var contactsSnapshot = await contactsCollectionRef.GetSnapshotAsync();

                var contacts = new List<Contact>();

                foreach (var contactDoc in contactsSnapshot.Documents)
                {
                    var contact = contactDoc.ConvertTo<Contact>();
                    contact.IsLoadingAvatar = true;
                    contact.AvatarUrl = await FirebaseStorageHelper.GetAvatarUrlAsync(contact.Email);
                    if (string.IsNullOrEmpty(contact.AvatarUrl))
                    {
                        contact.AvatarUrl = "/Assets/DefaultAvatar.png"; // Use resource path instead of relative path
                    }
                    contact.IsLoadingAvatar = false;
                    contacts.Add(contact);
                }

                Debug.WriteLine($"Loaded {contacts.Count} contacts for user {email}");
                return contacts;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading contacts: {ex.Message}");
                return new List<Contact>();
            }
        }

        private async void LoadInitialData()
        {
            Contacts.Clear();
            Debug.WriteLine($"Loading contacts for user: {SharedData.Instance.userdata?.Email}");

            List<Contact> contacts = await GetContactsAsync(SharedData.Instance.userdata?.Email);
            if (contacts.Count == 0)
            {
                Debug.WriteLine("No contacts found for the user.");
                SelectedContact = null;
                Messages.Clear();
                Files.Clear();
                return;
            }

            foreach (var contact in contacts)
            {
                Contacts.Add(contact);
            }

            if (Contacts.Any())
            {
                SelectedContact = Contacts[0];
                Debug.WriteLine($"Set SelectedContact to: {SelectedContact?.Name}");
            }
            else
            {
                SelectedContact = null;
                Messages.Clear();
                Files.Clear();
            }

            StartListeningToContactsPresence();
        }

        private string EscapeEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return string.Empty;
            return email.Replace('.', ',')
                        .Replace('#', '_')
                        .Replace('$', '_')
                        .Replace('[', '_')
                        .Replace(']', '_')
                        .Replace('/', '_');
        }

        private async void SetCurrentUserGlobalStatus(bool isOnline)
        {
            if (userdata == null || string.IsNullOrEmpty(userdata.Email))
            {
                Debug.WriteLine("SetCurrentUserGlobalStatus: Userdata or Email is null. Khong the cap nhat trang thai toan cuc");
                return;
            }

            string escapedUserEmail = EscapeEmail(userdata.Email);
            var statusUpdate = new UserStatusData
            {
                isOnline = isOnline,
                last_active = new Dictionary<string, string> { { ".sv", "timestamp" } }
            };

            try
            {
                await firebaseClient
                      .Child("user_status")
                      .Child(escapedUserEmail)
                      .PutAsync(statusUpdate);

                Debug.WriteLine($"Current user global status set to: {(isOnline ? "Online" : "Offline")} at /user_status/{escapedUserEmail}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting current user global status: {ex.Message}");
            }
        }

        private void StartListeningToContactsPresence()
        {
            if (Contacts == null || !Contacts.Any())
            {
                Debug.WriteLine("StartListeningToContactsPresence: Contacts list is null or empty.");
                return;
            }

            foreach (var listener in _contactPresenceListeners.Values)
            {
                listener?.Dispose();
            }
            _contactPresenceListeners.Clear();
            Debug.WriteLine("Cleared old contact presence listeners.");

            foreach (var contact in Contacts)
            {
                if (contact == null || string.IsNullOrEmpty(contact.Email))
                {
                    Debug.WriteLine("StartListeningToContactsPresence: Skipping a contact with null or empty email.");
                    continue;
                }

                string contactEmailCanLangNghe = contact.Email;
                string escapedContactEmail = EscapeEmail(contactEmailCanLangNghe);

                Debug.WriteLine($"Setting up listener for: {contactEmailCanLangNghe} (escaped: {escapedContactEmail}) at /user_status/{escapedContactEmail}");

                try
                {
                    UserStatusData localStatus = new UserStatusData();

                    var presenceListener = firebaseClient
                        .Child("user_status")
                        .Child(escapedContactEmail)
                        .AsObservable<object>()
                        .Subscribe(presenceEvent =>
                        {
                            Debug.WriteLine($"EventType: {presenceEvent.EventType}, Key: {presenceEvent.Key}");

                            if (presenceEvent.Object != null)
                            {
                                switch (presenceEvent.Key)
                                {
                                    case "isOnline":
                                        bool newIsOnline = Convert.ToBoolean(presenceEvent.Object);
                                        localStatus.isOnline = newIsOnline;
                                        Debug.WriteLine($"Updated isOnline: {newIsOnline}");
                                        break;

                                    case "last_active":
                                        if (long.TryParse(presenceEvent.Object.ToString(), out long newLastActive))
                                        {
                                            localStatus.last_active = newLastActive;
                                            Debug.WriteLine($"Updated last_active: {newLastActive}");
                                        }
                                        break;

                                    default:
                                        Debug.WriteLine($"Unknown key: {presenceEvent.Key}");
                                        break;
                                }

                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    var localContactToUpdate = Contacts.FirstOrDefault(c => c.Email == contactEmailCanLangNghe);
                                    if (localContactToUpdate != null)
                                    {
                                        localContactToUpdate.IsOnline = localStatus.isOnline;
                                        // Uncomment if you want to update last_active
                                        // localContactToUpdate.last_active = localStatus.last_active;
                                    }
                                });
                            }
                            else
                            {
                                Debug.WriteLine("Received null object in presenceEvent");
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    var localContactToUpdate = Contacts.FirstOrDefault(c => c.Email == contactEmailCanLangNghe);
                                    if (localContactToUpdate != null)
                                    {
                                        localContactToUpdate.IsOnline = false;
                                    }
                                });
                            }
                        });

                    _contactPresenceListeners[contactEmailCanLangNghe] = presenceListener;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PRESENCE SETUP FAIL] Failed for {contact.Email}: {ex.Message}");
                }
            }
        }

        public void Cleanup()
        {
            SetCurrentUserGlobalStatus(false);
            foreach (var subscription in allMessageSubscriptions)
            {
                subscription?.Dispose();
            }
            allMessageSubscriptions.Clear();

            messageSubscription?.Dispose();
            messageSubscription = null;

            foreach (var listener in _contactPresenceListeners.Values)
            {
                listener?.Dispose();
            }
            _contactPresenceListeners.Clear();

            EditProfileViewModel.AvatarUpdated -= OnAvatarUpdated;

            Debug.WriteLine("ChatViewModel cleanup complete.");
        }
    }
}