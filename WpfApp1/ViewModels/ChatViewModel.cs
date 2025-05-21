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
using System.Windows.Input;

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

        partial void OnSelectedContactChanged(Contact value)
        {
            // Thông báo cho Command cập nhật trạng thái có thể thực thi
            SendMessageCommand.NotifyCanExecuteChanged();

            // Nếu đã chọn một contact, tải tin nhắn của contact đó
            if (value != null)
            {
                LoadMessagesForContact(value);
            }
            else
            {
                // Nếu không có contact nào được chọn, xóa danh sách tin nhắn
                Messages.Clear();
                Files.Clear();

                // Hủy đăng ký lắng nghe tin nhắn nếu có
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

            firebaseClient = new FirebaseClient("https://fir-5b855-default-rtdb.firebaseio.com");

            userdata = SharedData.Instance.userdata;
            SelectedContact = null;
            NewMessageText = string.Empty;

            LoadInitialData();
        }


        private async void LoadMessagesForContact(Contact contact)
        {
            // Xóa danh sách tin nhắn hiện tại
            Messages.Clear();

            // Dừng subscription cũ nếu có
            if (messageSubscription != null)
            {
                messageSubscription.Dispose();
                messageSubscription = null;
            }

            // Kiểm tra contact có tồn tại không
            if (contact == null || string.IsNullOrEmpty(contact.chatID))
            {
                Debug.WriteLine("Contact không hợp lệ hoặc không có chatID");
                return;
            }

            var roomId = contact.chatID;
            Debug.WriteLine($"Đang tải tin nhắn cho chatID: {roomId}");

            try
            {
                // 1. Tải tin nhắn cũ từ Firebase
                var messagesQuery = await firebaseClient
                    .Child("messages")
                    .Child(roomId)
                    .OrderByKey()
                    .LimitToLast(100)  // Giới hạn 100 tin nhắn gần nhất để tránh quá tải
                    .OnceAsync<Message>();

                if (messagesQuery != null)
                {
                    var oldMessages = messagesQuery
                        .Select(item => {
                            var msg = item.Object;
                            msg.Id = item.Key;  // Đặt Id là khóa của tin nhắn trong Firebase
                            msg.IsMine = msg.SenderId == SharedData.Instance.userdata.Email;  // Đặt IsMine dựa trên SenderId
                            msg.Alignment = msg.IsMine ? "Right" : "Left";  // Đặt Alignment
                            return msg;
                        })
                        .OrderBy(m => m.Timestamp)
                        .ToList();

                    Debug.WriteLine($"Đã tải {oldMessages.Count} tin nhắn cũ");

                    Application.Current.Dispatcher.Invoke(() => {
                        foreach (var message in oldMessages)
                        {
                            Messages.Add(message);
                        }
                    });
                }

                // 2. Đăng ký lắng nghe tin nhắn mới
                messageSubscription = firebaseClient
                    .Child("messages")
                    .Child(roomId)
                    .AsObservable<Message>()
                    .Subscribe(messageEvent => {
                        if (messageEvent.EventType == FirebaseEventType.InsertOrUpdate && messageEvent.Object != null)
                        {
                            var message = messageEvent.Object;
                            message.Id = messageEvent.Key;  // Đặt Id là khóa Firebase
                            message.IsMine = message.SenderId == SharedData.Instance.userdata.Email;  // Đặt IsMine dựa trên SenderId
                            message.Alignment = message.IsMine ? "Right" : "Left";  // Đặt Alignment

                            // Chỉ thêm tin nhắn từ đối phương hoặc cập nhật nếu không phải của mình
                            if (!message.IsMine || Messages.Any(m => m.Id == message.Id))
                            {
                                Application.Current.Dispatcher.Invoke(() => {
                                    int existingIndex = -1;
                                    for (int i = 0; i < Messages.Count; i++)
                                    {
                                        if (Messages[i].Id == message.Id)
                                        {
                                            existingIndex = i;
                                            break;
                                        }
                                    }

                                    if (existingIndex >= 0)
                                    {
                                        Messages[existingIndex] = message;
                                        Debug.WriteLine($"Đã cập nhật tin nhắn: {message.Id}");
                                    }
                                    else if (!message.IsMine) // Chỉ thêm tin nhắn từ đối phương
                                    {
                                        int insertIndex = 0;
                                        while (insertIndex < Messages.Count && Messages[insertIndex].Timestamp < message.Timestamp)
                                        {
                                            insertIndex++;
                                        }
                                        Messages.Insert(insertIndex, message);
                                        Debug.WriteLine($"Đã thêm tin nhắn mới: {message.Id} tại vị trí {insertIndex}");
                                        if (!message.IsMine && SelectedContact?.chatID != roomId)
                                        {
                                            string notificationMessage = $"Tin nhắn mới từ {SelectedContact?.Name ?? "Người gửi"}: {message.Content}";
                                            var mainWindow = Application.Current.MainWindow as MainWindow;
                                            mainWindow?.ShowNotification(notificationMessage);
                                        }
                                    }
                                });
                            }
                        }
                        else if (messageEvent.EventType == FirebaseEventType.Delete)
                        {
                            Application.Current.Dispatcher.Invoke(() => {
                                var messageToRemove = Messages.FirstOrDefault(m => m.Id == messageEvent.Key);
                                if (messageToRemove != null)
                                {
                                    Messages.Remove(messageToRemove);
                                    Debug.WriteLine($"Đã xóa tin nhắn: {messageEvent.Key}");
                                }
                            });
                        }
                    }, ex => {
                        Debug.WriteLine($"Lỗi khi lắng nghe tin nhắn: {ex.Message}");
                    });

                // Lưu subscription để có thể dọn dẹp sau này
                allMessageSubscriptions.Add(messageSubscription);

                // Tải danh sách tệp tin nếu có
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
        }

        [RelayCommand(CanExecute = nameof(CanSendMessage))]
        private async void SendMessage(Contact contact)
        {
            if (!string.IsNullOrWhiteSpace(NewMessageText))
            {
                var newMessage = new Message
                {
                    SenderId = SharedData.Instance.userdata.Email,
                    Content = this.NewMessageText,
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

                    // Hiển thị thông báo đang tải lên
                    Mouse.OverrideCursor = Cursors.Wait;

                    try
                    {
                        // Upload file lên Firebase Storage
                        string downloadUrl = await FirebaseStorageHelper.UploadFileAsync(selectedFilePath, fileName);

                        // Xác định có phải là ảnh không
                        bool isImage = IsImageFile(fileExtension);

                        // Tạo đối tượng tin nhắn
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

                        // Gửi tin nhắn lên Firebase
                        var result = await firebaseClient
                            .Child("messages")
                            .Child(SelectedContact.chatID)
                            .PostAsync(newMessage);

                        // Lấy ID từ kết quả và gán vào tin nhắn
                        newMessage.Id = result.Key;

                        // Thêm vào ObservableCollection
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Messages.Add(newMessage);
                        });

                        // Nếu là tệp (không phải ảnh), thêm vào danh sách Files
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
                        // Khôi phục con trỏ chuột
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
            if (byteCount == 0)
                return "0" + suf[0];
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
                MessageBox.Show("No contacts found.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
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
        }
    }
}
