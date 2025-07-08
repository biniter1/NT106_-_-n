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
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Linq;
using ToastNotifications.Messages;
using System.Collections.Generic; // Added for List<IDisposable> and Dictionary<string, IDisposable>
using System.Threading.Tasks;
using WpfApp1.Services;
using WpfApp1.Views; // Added for Task and CustomMessageBox
using NAudio.Wave;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

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
        private List<string> _viewedImageHistory = new List<string>();
        private string _imageHistoryFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WpfApp1", "ImageHistory.txt");
        public event EventHandler<NewMessageEventArgs> NewMessageNotificationRequested;

        // --- Các biến phục vụ việc ghi âm ---
        private WebRTCService _currentCallService;
        private CallWindow _callWindow;
        private IDisposable _callStateListener;
        public event EventHandler ScrollToBottomRequested;

        private WaveInEvent waveIn;
        private WaveFileWriter writer;
        private string tempAudioFilePath;

        private bool _isRecording = false;

        // --- Biến phục vụ việc hiển thị trạng thái đang gõ ---
        private DispatcherTimer typingTimer;
        private IDisposable typingStatusSubscription;

        [ObservableProperty]
        private string _typingStatusText;

        //--- Biến phục vụ việc trả lời tin nhắn ---
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsReplying))]
        private Message _messageToReplyTo;

        private HashSet<string> BlockedUsers { get; set; } = new HashSet<string>();
        public bool IsReplying => MessageToReplyTo != null;

        [RelayCommand]
        private void ReplyToMessage(Message message)
        {
            if (message == null) return;
            MessageToReplyTo = message;
        }
        [RelayCommand]
        private void CancelReply()
        {
            MessageToReplyTo = null;
        }

        public bool IsRecording
        {
            get => _isRecording;
            set
            {
                
                if (SetProperty(ref _isRecording, value))
                {
                    OnPropertyChanged(nameof(IsNotRecording));
                }
            }
        }
        public bool IsNotRecording => !IsRecording;

        public void InitiateChatWith(Contact contactToChat)
        {
            if (contactToChat == null) return;
            var existingContact = Contacts.FirstOrDefault(c => c.chatID == contactToChat.chatID);

            if (existingContact != null)
            {
                SelectedContact = existingContact;
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Contacts.Insert(0, contactToChat);
                });
                SelectedContact = contactToChat;
            }
        }
        partial void OnSelectedContactChanged(Contact value)
        {
            SendMessageCommand.NotifyCanExecuteChanged();
            StartVideoCallCommand.NotifyCanExecuteChanged();
            StartVoiceCallCommand.NotifyCanExecuteChanged();

            typingStatusSubscription?.Dispose();
            TypingStatusText = string.Empty; 

            if (value != null)
            {
                value.HasUnreadMessages = false;
                LoadMessagesForContact(value);

                var roomId = value.chatID;
                typingStatusSubscription = firebaseClient
                    .Child("typing_status")
                    .Child(roomId)
                    .AsObservable<Dictionary<string, bool>>()
                    .Subscribe(typingUsersDict =>
                    {
                        if (typingUsersDict.Object == null || typingUsersDict.Object.Count == 0)
                        {
                            TypingStatusText = string.Empty;
                            return;
                        }

                        var escapedCurrentUserEmail = EscapeEmail(userdata.Email);
                        var otherTypingUsersNames = new List<string>();
                        foreach (var emailKey in typingUsersDict.Object.Keys)
                        {
                            if (emailKey == escapedCurrentUserEmail) continue;
                            var typingContact = Contacts.FirstOrDefault(c => EscapeEmail(c.Email) == emailKey);

                            if (typingContact != null)
                            {
                                otherTypingUsersNames.Add(typingContact.Name);
                            }
                        }
                        if (otherTypingUsersNames.Count == 0)
                        {
                            TypingStatusText = string.Empty;
                        }
                        else if (otherTypingUsersNames.Count == 1)
                        {
                            TypingStatusText = $"{otherTypingUsersNames[0]} đang nhập...";
                        }
                        else
                        {
                            TypingStatusText = $"{otherTypingUsersNames.Count} người đang nhập...";
                        }

                        Debug.WriteLine($"--> [UI UPDATE] TypingStatusText set to: '{TypingStatusText}'");
                    });
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

            if (typingTimer != null)
            {
                typingTimer.Stop();
                typingTimer.Start();
            }

            UpdateTypingStatus(true);
        }
        private async void UpdateTypingStatus(bool isTyping)
        {
            Debug.WriteLine("--- Checking conditions for UpdateTypingStatus ---");

            if (SelectedContact == null)
            {
                Debug.WriteLine("-> FAILED: SelectedContact is null. Cannot send typing status.");
                return;
            }
            if (string.IsNullOrEmpty(SelectedContact.chatID))
            {
                Debug.WriteLine("-> FAILED: SelectedContact.chatID is null or empty.");
                return;
            }
            if (userdata == null)
            {
                Debug.WriteLine("-> FAILED: userdata is null.");
                return;
            }
            if (string.IsNullOrEmpty(userdata.Email))
            {
                Debug.WriteLine("-> FAILED: userdata.Email is null or empty.");
                return;
            }
            Debug.WriteLine("-> PASSED: All conditions met. Proceeding to send status.");
            Debug.WriteLine($"-> Firebase Client State: {(firebaseClient == null ? "IS NULL" : "Exists")}");

            var escapedUserEmail = EscapeEmail(userdata.Email);
            if (string.IsNullOrEmpty(escapedUserEmail)) return;

            var typingRef = firebaseClient
                .Child("typing_status")
                .Child(SelectedContact.chatID)
                .Child(escapedUserEmail);

            try
            {
                if (isTyping)
                {
                    Debug.WriteLine("-> Executing PutAsync(true)...");
                    await typingRef.PutAsync(true);
                    Debug.WriteLine("-> SUCCESS: PutAsync(true) completed without error.");
                }
                else
                {
                    Debug.WriteLine("-> Executing DeleteAsync()...");
                    await typingRef.DeleteAsync();
                    Debug.WriteLine("-> SUCCESS: DeleteAsync() completed without error.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                Debug.WriteLine("!!!! FIREBASE EXCEPTION CAUGHT IN UpdateTypingStatus !!!!");
                Debug.WriteLine($"!!!! Exception Type: {ex.GetType().Name}");
                Debug.WriteLine($"!!!! Exception Message: {ex.Message}");
                Debug.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            }
        }
        public ChatViewModel(FirebaseClient fbClient)
        {
            if (fbClient == null)
            {
                Debug.WriteLine("FATAL: FirebaseClient is null in ChatViewModel constructor.");
                throw new ArgumentNullException(nameof(fbClient), "FirebaseClient cannot be null.");
            }
            // Khởi tạo collections
            Messages = new ObservableCollection<Message>();
            Contacts = new ObservableCollection<Contact>();
            Files = new ObservableCollection<FileItem>();

            // Khởi tạo các giá trị ban đầu
            firebaseClient = fbClient;
            SelectedContact = null;
            NewMessageText = string.Empty;

            // Khởi tạo timer cho trạng thái "đang gõ"
            typingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            typingTimer.Tick += TypingTimer_Tick;

            // Đăng ký sự kiện
            EditProfileViewModel.AvatarUpdated += OnAvatarUpdated;

            // Tải dữ liệu ban đầu
            LoadInitialData();

            // Tạo thư mục tạm
            Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "WpfApp1"));
            userdata = SharedData.Instance.userdata;
        }

        [RelayCommand]
        private async Task BlockContact(Contact contact)
        {
            
            if (contact == null)
            {
                CustomMessageBox.Show("Không có người liên hệ được chọn để chặn.", "Thông báo", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Information);
                return;
            }
            var currentUser = SharedData.Instance.userdata;
            
            if (currentUser == null)
            {
                CustomMessageBox.Show("Không có người dùng hiện tại để thực hiện hành động này.", "Thông báo", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Information);
                return;
            }

            var result = CustomMessageBox.Show($"Bạn có chắc chắn muốn chặn '{contact.Name}' không?", "Xác nhận chặn", CustomMessageBoxWindow.MessageButtons.YesNo, CustomMessageBoxWindow.MessageIcon.Warning);
            if (result == MessageBoxResult.No) return;

            try
            {
                var db = FirestoreHelper.database;
                var blockedUserRef = db.Collection("users").Document(currentUser.Email).Collection("blockedUsers").Document(contact.Email);
                await blockedUserRef.SetAsync(new { email = contact.Email, blockedAt = Timestamp.GetCurrentTimestamp() });

                BlockedUsers.Add(contact.Email);
                contact.IsBlockedByMe = true;

                SendMessageCommand.NotifyCanExecuteChanged();
                StartVideoCallCommand.NotifyCanExecuteChanged();
                StartVoiceCallCommand.NotifyCanExecuteChanged();

                OnSelectedContactChanged(contact);
                CustomMessageBox.Show($"Đã chặn {contact.Name}.", "Thành công", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi khi chặn người dùng: {ex.Message}");
                MessageBox.Show("Đã xảy ra lỗi. Vui lòng thử lại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task UnblockContactAsync(Contact contact)
        {
            if (contact == null) return;
            var currentUser = SharedData.Instance.userdata;
            if (currentUser == null) return;

            try
            {
                var db = FirestoreHelper.database;
                var blockedUserRef = db.Collection("users").Document(currentUser.Email).Collection("blockedUsers").Document(contact.Email);
                await blockedUserRef.DeleteAsync();

                BlockedUsers.Remove(contact.Email);
                contact.IsBlockedByMe = false;

                SendMessageCommand.NotifyCanExecuteChanged();
                StartVideoCallCommand.NotifyCanExecuteChanged();
                StartVoiceCallCommand.NotifyCanExecuteChanged();

                OnSelectedContactChanged(contact);
                CustomMessageBox.Show($"Đã bỏ chặn {contact.Name}.", "Thành công", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi khi bỏ chặn người dùng: {ex.Message}");
                MessageBox.Show("Đã xảy ra lỗi. Vui lòng thử lại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void TypingTimer_Tick(object sender, EventArgs e)
        {
            typingTimer.Stop();
            UpdateTypingStatus(false); // Gửi tín hiệu "dừng gõ"
        }

        [RelayCommand]
        private async Task RecordVoiceMessageAsync()
        {
            if (SelectedContact == null)
            {
                CustomMessageBox.Show("Vui lòng chọn một cuộc trò chuyện để gửi tin nhắn thoại.", "Thông báo", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Information);
                return;
            }

            if (!IsRecording)
            {
                // --- BẮT ĐẦU GHI ÂM ---
                try
                {
                    IsRecording = true;
                    tempAudioFilePath = Path.Combine(Path.GetTempPath(), "WpfApp1", $"voice_{DateTime.Now:yyyyMMddHHmmss}.wav");

                    waveIn = new WaveInEvent();
                    waveIn.WaveFormat = new WaveFormat(16000, 1); // 16kHz, Mono

                    waveIn.DataAvailable += (s, a) =>
                    {
                        if (writer != null)
                        {
                            writer.Write(a.Buffer, 0, a.BytesRecorded);
                        }
                    };

                    waveIn.RecordingStopped += async (s, a) =>
                    {
                        // Dọn dẹp
                        waveIn?.Dispose();
                        waveIn = null;
                        writer?.Close();
                        writer = null;

                        // Tải file lên và gửi tin nhắn
                        await UploadAndSendVoiceMessageAsync(tempAudioFilePath);
                        IsRecording = false;
                    };

                    writer = new WaveFileWriter(tempAudioFilePath, waveIn.WaveFormat);
                    waveIn.StartRecording();
                    Debug.WriteLine("Bắt đầu ghi âm...");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Lỗi khi bắt đầu ghi âm: {ex.Message}");
                    IsRecording = false;
                }
            }
            else
            {
                // --- DỪNG GHI ÂM ---
                try
                {
                    waveIn?.StopRecording();
                    Debug.WriteLine("Đang dừng ghi âm...");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Lỗi khi dừng ghi âm: {ex.Message}");
                    IsRecording = false;
                }
            }
        }

        private async Task UploadAndSendVoiceMessageAsync(string filePath)
        {
            if (!File.Exists(filePath)) return;

            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                // Lấy độ dài file âm thanh
                var audioDuration = new WaveFileReader(filePath).TotalTime.TotalSeconds;

                // Tải file lên Firebase Storage
                string uniqueFileName = $"voice_{Path.GetFileName(filePath)}";
                string downloadUrl = await FirebaseStorageHelper.UploadFileAsync(filePath, uniqueFileName);

                if (!string.IsNullOrEmpty(downloadUrl))
                {
                    var newMessage = new Message
                    {
                        SenderId = userdata.Email,
                        Timestamp = DateTime.UtcNow,
                        IsMine = true,
                        IsVoiceMessage = true,
                        VoiceMessageUrl = downloadUrl,
                        VoiceMessageDuration = audioDuration,
                        Alignment = "Right"
                    };

                    await firebaseClient
                        .Child("messages")
                        .Child(SelectedContact.chatID)
                        .PostAsync(newMessage);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi khi gửi tin nhắn thoại: {ex.Message}");
            }
            finally
            {
                Mouse.OverrideCursor = null;

                // Cho hệ thống một chút thời gian để giải phóng file
                await Task.Delay(200);

                if (File.Exists(filePath))
                {
                    // Thử xóa file với logic retry, tối đa 5 lần
                    for (int i = 0; i < 5; i++)
                    {
                        try
                        {
                            File.Delete(filePath);
                            Debug.WriteLine("Xóa file tạm thành công!");
                            break; // Nếu xóa thành công, thoát khỏi vòng lặp
                        }
                        catch (IOException ex)
                        {
                            Debug.WriteLine($"Lần {i + 1}: Không thể xóa file. Đang chờ... Lỗi: {ex.Message}");
                            if (i < 4) 
                            {
                                await Task.Delay(300);
                            }
                        }
                    }
                }
            }
        }
        private string EscapeEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return string.Empty;
            return email.Replace('.', '_')
                        .Replace('@', '_') // Thêm xử lý cho ký tự @
                        .Replace('#', '_')
                        .Replace('$', '_')
                        .Replace('[', '_')
                        .Replace(']', '_')
                        .Replace('/', '_');
        }

        [RelayCommand(CanExecute = nameof(CanStartCall))]
        private async Task StartVideoCallAsync(Contact recipient)
        {
            await StartCallAsync(recipient, "Video");
        }
        [RelayCommand(CanExecute = nameof(CanStartCall))]
        private async Task StartVoiceCallAsync(Contact recipient)
        {
            await StartCallAsync(recipient, "Voice");
        }
        private async Task StartCallAsync(Contact recipient, string callType)
        {
            if (recipient == null) return;
            var currentUser = SharedData.Instance.userdata;
            if (currentUser == null) return;

            var callSignal = new CallSignal
            {
                CallId = Guid.NewGuid().ToString(),
                CallerId = currentUser.Email,
                CallerName = currentUser.Name,
                CallerAvatarUrl = currentUser.AvatarUrl,
                CalleeId = recipient.Email,
                CallType = callType,
                Status = "Ringing",
                Timestamp = DateTime.UtcNow
            };

            _currentCallService = new WebRTCService(firebaseClient, callSignal);
            var callViewModel = new CallViewModel(_currentCallService);
            _callWindow = new CallWindow(callViewModel);

            await _currentCallService.InitializeAsync();
            await _currentCallService.AddAudioTrackAsync();
            if (callType == "Video")
            {
                await _currentCallService.AddVideoTrackAsync();
            }
            _currentCallService.OnIceCandidateReady += async (candidate) =>
            {
                try
                {
                    string recipientSafeEmail = EscapeEmail(recipient.Email);
                    var iceSignal = IceCandidateSignal.FromWebRtcCandidate(candidate);
                    await firebaseClient
                        .Child("ice_candidates")
                        .Child(recipientSafeEmail) // Gửi đến người nhận
                        .Child(callSignal.CallId)
                        .PostAsync(iceSignal);
                }
                catch (Exception ex) { Debug.WriteLine($"Error sending ICE candidate: {ex.Message}"); }
            };

            ListenForCallUpdates(callSignal);

            _currentCallService.OnSdpOfferReady += async (sdpOffer) =>
            {
                callSignal.SdpOffer = sdpOffer;
                try
                {
                    string recipientSafeEmail = EscapeEmail(recipient.Email);
                    string firebasePath = $"calls/{recipientSafeEmail}/{callSignal.CallId}";

                    // === LOG GỠ LỖI 1 ===
                    Debug.WriteLine($"[CALLER] Attempting to send call signal to path: {firebasePath}");
                    await firebaseClient
                        .Child("calls")
                        .Child(recipientSafeEmail)
                        .Child(callSignal.CallId)
                        .PutAsync(callSignal);

                    Debug.WriteLine($"Call signal with SDP Offer sent to {recipient.Email}.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error sending call signal: {ex.Message}");
                    CustomMessageBox.Show("Không thể thực hiện cuộc gọi. Vui lòng thử lại.", "Lỗi", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
                    _callWindow.Close();
                }
            };

            await _currentCallService.CreateOfferAsync();
            _callWindow.Show();
        }
        private void ListenForCallUpdates(CallSignal call)
        {
            string currentUserSafeEmail = EscapeEmail(SharedData.Instance.userdata.Email);

            // Lắng nghe thay đổi trạng thái chính (Answer)
            _callStateListener = firebaseClient
                .Child("calls")
                .Child(currentUserSafeEmail) // Lắng nghe trên kênh của chính mình
                .Child(call.CallId)
                .AsObservable<CallSignal>()
                .Subscribe(async callEvent =>
                {
                    if (callEvent.EventType == FirebaseEventType.InsertOrUpdate && callEvent.Object != null)
                    {
                        var updatedCall = callEvent.Object;
                        if (updatedCall.Status == "Accepted" && !string.IsNullOrEmpty(updatedCall.SdpAnswer))
                        {
                            Debug.WriteLine("Call accepted and SDP Answer received.");
                            await _currentCallService.HandleAnswerAsync(updatedCall.SdpAnswer);

                            // Sau khi nhận Answer, bắt đầu lắng nghe ICE của người nhận
                            ListenForRemoteIceCandidates(call.CalleeId, call.CallId);
                        }
                        else if (updatedCall.Status == "Declined" || updatedCall.Status == "Ended")
                        {
                            Debug.WriteLine("Call was declined or has ended.");
                            _currentCallService?.HangUp();
                            _callWindow?.Close();
                            _callStateListener?.Dispose();
                        }
                    }
                });
        }
        private void ListenForRemoteIceCandidates(string remoteUserEmail, string callId)
        {
            string currentUserSafeEmail = EscapeEmail(SharedData.Instance.userdata.Email);
            firebaseClient
                .Child("ice_candidates")
                .Child(currentUserSafeEmail) // Lắng nghe trên kênh của mình
                .Child(callId)
                .AsObservable<IceCandidateSignal>()
                .Subscribe(iceEvent =>
                {
                    if (iceEvent.EventType == FirebaseEventType.InsertOrUpdate && iceEvent.Object != null)
                    {
                        Debug.WriteLine("Received remote ICE candidate.");
                        var remoteCandidate = iceEvent.Object.ToWebRtcCandidate();
                        _currentCallService.AddIceCandidateAsync(remoteCandidate);
                    }
                });
        }

        private bool CanStartCall(Contact recipient)
        {
            return recipient != null && !recipient.InteractionIsBlocked;
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
            if (contact.InteractionIsBlocked)
            {
                string message = contact.IsBlockedByMe
                    ? $"Bạn đã chặn {contact.Name}. Hãy bỏ chặn để tiếp tục trò chuyện."
                    : $"Bạn không thể nhắn tin cho {contact.Name} vì người này đã chặn bạn.";
                Messages.Add(new Message { Content = message, IsSystemMessage = true });
                return;
            }
            if (contact.IsBlockedByMe)
            {
                Messages.Add(new Message { Content = $"Bạn đã chặn {contact.Name}. Hãy bỏ chặn để tiếp tục trò chuyện.", IsSystemMessage = true });
                return;
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
                if (firebaseClient == null)
                {
                    Debug.WriteLine("FirebaseClient is null in LoadMessagesForContact. Cannot load messages.");
                    CustomMessageBox.Show("Lỗi: Không thể kết nối đến máy chủ chat. Vui lòng thử lại.", "Lỗi kết nối", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
                    return;
                }
                bool isGroupChat = roomId.StartsWith("group_");

                var messagesQuery = await firebaseClient
                    .Child("messages")
                    .Child(roomId)
                    .OrderBy("Timestamp")
                    .LimitToLast(100)
                    .OnceAsync<Message>();

                if (messagesQuery != null)
                {
                    var oldMessages = messagesQuery
                        .Where(item => item.Object != null)
                        .Select(item =>
                        {
                            var msg = item.Object;
                            msg.Id = item.Key;
                            msg.IsMine = msg.SenderId == SharedData.Instance.userdata?.Email;
                            msg.Alignment = msg.IsMine ? "Right" : "Left";

                            if (isGroupChat && !msg.IsMine)
                            {
                                var senderName = GetSenderDisplayName(msg.SenderId);
                                if (!string.IsNullOrEmpty(senderName))
                                {
                                    msg.SenderDisplayName = senderName;
                                }
                            }

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
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (messageEvent.EventType == FirebaseEventType.InsertOrUpdate && messageEvent.Object != null)
                            {
                                var message = messageEvent.Object;
                                bool isSenderBlocked = BlockedUsers.Contains(message.SenderId);
                                Debug.WriteLine($"[BLOCK CHECK] Received message from: {message.SenderId}. Is this user in block list? {isSenderBlocked}");
                                if (!message.IsMine && BlockedUsers.Contains(message.SenderId))
                                {
                                    Debug.WriteLine($"Ignoring message from blocked user: {message.SenderId}");
                                    return;
                                }

                                message.Id = messageEvent.Key;
                                message.IsMine = message.SenderId == SharedData.Instance.userdata?.Email;
                                message.Alignment = message.IsMine ? "Right" : "Left";

                                if (isGroupChat && !message.IsMine)
                                {
                                    var senderName = GetSenderDisplayName(message.SenderId);
                                    if (!string.IsNullOrEmpty(senderName))
                                    {
                                        message.SenderDisplayName = senderName;
                                    }
                                }
                                int existingIndex = Messages.IndexOf(Messages.FirstOrDefault(m => m.Id == message.Id));
                                if (existingIndex >= 0)
                                {
                                    Messages[existingIndex] = message;
                                    Debug.WriteLine($"Đã cập nhật tin nhắn: {message.Id}");
                                }
                                else
                                {
                                    int insertIndex = Messages.ToList().FindIndex(m => m.Timestamp > message.Timestamp);
                                    if (insertIndex < 0) insertIndex = Messages.Count;
                                    Messages.Insert(insertIndex, message);
                                    Debug.WriteLine($"Đã thêm tin nhắn mới: {message.Id} tại vị trí {insertIndex}");
                                    ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
                                    if (!message.IsMine)
                                    {
                                        if (SelectedContact == null || SelectedContact.chatID != roomId)
                                        {
                                            var contactToUpdate = Contacts.FirstOrDefault(c => c.chatID == roomId);
                                            if (contactToUpdate != null)
                                            {
                                                contactToUpdate.HasUnreadMessages = true;
                                            }
                                        }
                                        bool isCurrentChatSelected = (SelectedContact?.chatID == roomId);
                                        bool isAppActive = Application.Current.MainWindow?.IsActive ?? false;
                                        bool shouldNotify = !isAppActive || !isCurrentChatSelected;

                                        if (shouldNotify)
                                        {
                                            string titleForNotification = GetSenderDisplayName(message.SenderId) ?? message.SenderId;
                                            string messageForNotification = message.Content;

                                            if (isGroupChat)
                                            {
                                                var contactInList = Contacts.FirstOrDefault(c => c.chatID == roomId);
                                                titleForNotification = contactInList?.Name ?? "Nhóm chat";
                                                messageForNotification = $"{GetSenderDisplayName(message.SenderId)}: {message.Content}";
                                            }

                                            if (message.IsImage)
                                            {
                                                messageForNotification = isGroupChat
                                                    ? $"{GetSenderDisplayName(message.SenderId)}: {GetLocalizedString("Notification_NewImage")}"
                                                    : GetLocalizedString("Notification_NewImage");
                                            }
                                            else if (message.IsVideo)
                                            {
                                                messageForNotification = isGroupChat
                                                    ? $"{GetSenderDisplayName(message.SenderId)}: {GetLocalizedString("Notification_NewVideo")}"
                                                    : GetLocalizedString("Notification_NewVideo");
                                            }
                                            else if (!string.IsNullOrEmpty(message.FileUrl))
                                            {
                                                messageForNotification = isGroupChat
                                                    ? $"{GetSenderDisplayName(message.SenderId)}: {GetLocalizedString("Notification_NewFile")} \"{message.Content}\""
                                                    : $"{GetLocalizedString("Notification_NewFile")} \"{message.Content}\"";
                                            }

                                            NewMessageNotificationRequested?.Invoke(
                                                this,
                                                new NewMessageEventArgs(titleForNotification, messageForNotification, roomId)
                                            );
                                            Debug.WriteLine($"ĐÃ KÍCH HOẠT EVENT THÔNG BÁO: Title='{titleForNotification}', Message='{messageForNotification}', ChatID='{roomId}'");
                                        }
                                    }
                                }
                            }
                            else if (messageEvent.EventType == FirebaseEventType.Delete)
                            {
                                var messageToRemove = Messages.FirstOrDefault(m => m.Id == messageEvent.Key);
                                if (messageToRemove != null)
                                {
                                    Messages.Remove(messageToRemove);
                                    Debug.WriteLine($"Đã xóa tin nhắn: {messageEvent.Key}");
                                }
                            }
                        });
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
                CustomMessageBox.Show($"Không thể tải tin nhắn: {ex.Message}", "Lỗi", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
            }
        }

        private string GetSenderDisplayName(string senderEmail)
        {
            var contact = Contacts.FirstOrDefault(c => c.Email == senderEmail);
            if (contact != null)
            {
                return contact.Name;
            }

            if (senderEmail.Contains("@"))
            {
                return senderEmail.Split('@')[0];
            }

            return senderEmail;
        }

        private async void LoadFilesForContact(Contact contact)
        {
            Files.Clear();

            if (contact == null || string.IsNullOrEmpty(contact.chatID)) return;

            try
            {
                var messagesQuery = await firebaseClient
                    .Child("messages")
                    .Child(contact.chatID)
                    .OrderByKey()
                    .OnceAsync<Message>();

                if (messagesQuery != null)
                {
                    foreach (var messageItem in messagesQuery)
                    {
                        var message = messageItem.Object;

                        if ((message.IsImage && !string.IsNullOrEmpty(message.ImageUrl)) ||
                            (message.IsVideo && !string.IsNullOrEmpty(message.VideoUrl)) ||
                            (!string.IsNullOrEmpty(message.FileUrl)))
                        {
                            string fileUrl = message.IsImage ? message.ImageUrl :
                                             (message.IsVideo ? message.VideoUrl : message.FileUrl);
                            if (string.IsNullOrEmpty(fileUrl)) continue;

                            string fileName = message.Content;
                            if (message.IsImage && string.IsNullOrEmpty(fileName))
                                fileName = $"Image_{DateTime.Now.ToString("yyyyMMddHHmmss")}.jpg";
                            else if (message.IsVideo && string.IsNullOrEmpty(fileName))
                                fileName = $"Video_{DateTime.Now.ToString("yyyyMMddHHmmss")}.mp4";

                            string extension = Path.GetExtension(fileName);
                            if (string.IsNullOrEmpty(extension))
                            {
                                if (message.IsImage)
                                    extension = ".jpg";
                                else if (message.IsVideo)
                                    extension = ".mp4";
                            }

                            bool isVideo = message.IsVideo || IsVideoFile(extension);

                            var fileItem = new FileItem
                            {
                                IconPathOrType = string.IsNullOrEmpty(extension) ?
                                                 (message.IsImage ? "jpg" : (isVideo ? "mp4" : "txt")) :
                                                 extension.TrimStart('.'),
                                FileName = fileName,
                                FileInfo = $"{(message.IsImage ? "Image" : (isVideo ? "Video" : "File"))} • {message.Timestamp:dd/MM/yyyy}",
                                FilePathOrUrl = fileUrl,
                                DownloadUrl = fileUrl,
                                FileExtension = extension,
                                IsVideo = isVideo
                            };

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                if (!Files.Any(f => f.DownloadUrl == fileUrl))
                                    Files.Add(fileItem);
                            });
                        }
                    }

                    Debug.WriteLine($"Loaded {Files.Count} files for contact {contact.Name}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading files for sidebar: {ex.Message}");
            }
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
                if (contact.IsBlockedByMe)
                {
                    CustomMessageBox.Show("Bạn đã chặn người này, không thể gửi tin nhắn.", "Chặn", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Warning);
                    return;
                }
                if (IsReplying)
                {
                    newMessage.IsReply = true;
                    newMessage.ReplyToMessageId = MessageToReplyTo.Id;
                    newMessage.ReplyToMessageContent = MessageToReplyTo.Content;
                    // Lấy tên người gửi của tin nhắn gốc
                    newMessage.ReplyToSenderName = MessageToReplyTo.IsMine
                        ? "Bạn"
                        : GetSenderDisplayName(MessageToReplyTo.SenderId);
                }

                await firebaseClient
                    .Child("messages")
                    .Child(contact.chatID)
                    .PostAsync(newMessage);

                NewMessageText = string.Empty;
                CancelReply();

                typingTimer.Stop();
                UpdateTypingStatus(false);
            }
        }

        private bool CanSendMessage()
        {
            return !string.IsNullOrWhiteSpace(NewMessageText) && SelectedContact != null && !SelectedContact.InteractionIsBlocked;
        }

        [RelayCommand]
        private async Task SelectFileAsync()
        {
            if (SelectedContact == null)
            {
                CustomMessageBox.Show("Vui lòng chọn một liên hệ trước khi gửi tệp tin.", "Thông báo", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Information);
                return;
            }

            var openFileDialog = new OpenFileDialog
            {
                Title = "Chọn tệp để gửi",
                Filter = "Tất cả tệp|*.*|Hình ảnh|*.jpg;*.jpeg;*.png;*.gif;*.bmp|Video|*.mp4;*.avi;*.mov;*.wmv;*.mkv",
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
                        string uniqueFileName = $"{DateTime.Now:yyyyMMddHHmmssfff}_{fileName}";

                        string downloadUrl = await FirebaseStorageHelper.UploadFileAsync(selectedFilePath, uniqueFileName);

                        bool isImage = IsImageFile(fileExtension);
                        bool isVideo = IsVideoFile(fileExtension);

                        Debug.WriteLine($"File detected: {(isImage ? "image" : (isVideo ? "video" : "regular file"))} based on extension: {fileExtension}");

                        var newMessage = new Message
                        {
                            SenderId = SharedData.Instance.userdata.Email,
                            Content = fileName,
                            Timestamp = DateTime.UtcNow,
                            IsMine = true,
                            IsImage = isImage,
                            IsVideo = isVideo,
                            ImageUrl = isImage ? downloadUrl : null,
                            VideoUrl = isVideo ? downloadUrl : null,
                            FileUrl = (!isImage && !isVideo) ? downloadUrl : null
                        };

                        var result = await firebaseClient
                            .Child("messages")
                            .Child(SelectedContact.chatID)
                            .PostAsync(newMessage);

                        newMessage.Id = result.Key;

                        var fileItem = new FileItem
                        {
                            IconPathOrType = string.IsNullOrEmpty(fileExtension) ? "txt" : fileExtension.TrimStart('.'),
                            FileName = fileName,
                            FileInfo = $"{(isImage ? "Image" : (isVideo ? "Video" : "File"))} • {FormatFileSize(new FileInfo(selectedFilePath).Length)} • {DateTime.Now:dd/MM/yyyy}",
                            FilePathOrUrl = downloadUrl,
                            DownloadUrl = downloadUrl,
                            FileExtension = fileExtension,
                            IsVideo = isVideo
                        };

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Files.Add(fileItem);
                        });

                        Debug.WriteLine($"File uploaded successfully: {fileName} with URL: {downloadUrl}");
                        Debug.WriteLine($"File type: {fileItem.IconPathOrType}, IsImage: {isImage}, IsVideo: {isVideo}");
                    }
                    finally
                    {
                        Mouse.OverrideCursor = null;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error uploading file: {ex.Message}");
                    CustomMessageBox.Show($"Không thể tải lên tệp tin: {ex.Message}", "Lỗi", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
                }
            }
        }

        private bool IsImageFile(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return false;

            extension = extension.ToLower().Trim();
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp" };
            return Array.IndexOf(imageExtensions, extension) >= 0;
        }

        private bool IsVideoFile(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return false;

            extension = extension.ToLower().Trim();
            string[] videoExtensions = { ".mp4", ".avi", ".mov", ".wmv", ".mkv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg", ".3gp" };
            return Array.IndexOf(videoExtensions, extension) >= 0;
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

        public async Task<List<Contact>> GetContactsAsync(string currentUserEmail)
        {
            if (string.IsNullOrEmpty(currentUserEmail)) return new List<Contact>();

            try
            {
                var db = FirestoreHelper.database;
                var contactsSnapshot = await db.Collection("users").Document(currentUserEmail).Collection("contacts").GetSnapshotAsync();
                var contacts = new List<Contact>();

                foreach (var contactDoc in contactsSnapshot.Documents)
                {
                    var contact = contactDoc.ConvertTo<Contact>();
                    // Gán trạng thái chặn từ danh sách đã được tải ở LoadInitialData
                    contact.IsBlockedByMe = BlockedUsers.Contains(contact.Email);

                    var theirBlockedListRef = db.Collection("users").Document(contact.Email).Collection("blockedUsers").Document(currentUserEmail);
                    var theirBlockSnapshot = await theirBlockedListRef.GetSnapshotAsync();
                    contact.IsBlockingMe = theirBlockSnapshot.Exists;

                    if (contact.IsBlockingMe)
                    {
                        Debug.WriteLine($"[BLOCK CHECK] User '{contact.Name}' is blocking you.");
                    }

                    contact.IsLoadingAvatar = true;
                    contact.AvatarUrl = await FirebaseStorageHelper.GetAvatarUrlAsync(contact.Email) ?? "/Assets/DefaultAvatar.png";
                    contact.IsLoadingAvatar = false;

                    contacts.Add(contact);
                }
                Debug.WriteLine($"Loaded {contacts.Count} contacts for user {currentUserEmail}");
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
            var currentUser = SharedData.Instance.userdata;
            if (currentUser == null || string.IsNullOrEmpty(currentUser.Email))
            {
                Debug.WriteLine("[FATAL] LoadInitialData: Current user data is not available. Aborting data load.");
                return;
            }

            Debug.WriteLine($"Loading all data for user: {currentUser.Email}");
            await LoadBlockedUsersAsync(currentUser.Email);
            List<Contact> contacts = await GetContactsAsync(currentUser.Email);

            if (contacts.Any())
            {
                foreach (var contact in contacts)
                {
                    Contacts.Add(contact);
                }
                SelectedContact = Contacts[0];
            }
            else
            {
                Debug.WriteLine("No contacts found for the user.");
            }

            SetCurrentUserGlobalStatus(true);
            StartListeningToContactsPresence();
        }
        private async Task LoadBlockedUsersAsync(string currentUserEmail)
        {
            if (string.IsNullOrEmpty(currentUserEmail)) return;
            try
            {
                var db = FirestoreHelper.database;
                var blockedUsersSnapshot = await db.Collection("users").Document(currentUserEmail).Collection("blockedUsers").GetSnapshotAsync();

                var blockedEmailList = blockedUsersSnapshot.Documents.Select(d => d.Id).ToList();
                BlockedUsers = new HashSet<string>(blockedEmailList);
                Debug.WriteLine($"[BLOCK] Loaded {BlockedUsers.Count} blocked user(s): [{string.Join(", ", BlockedUsers)}]");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BLOCK] Error loading blocked users list: {ex.Message}");
            }
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
                last_active = new Dictionary<string, object> { { ".sv", "timestamp" } }
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

        private async void StartListeningToContactsPresence()
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

                Debug.WriteLine($"Checking and initializing status for: {contactEmailCanLangNghe} (escaped: {escapedContactEmail})");

                try
                {
                    var existingStatus = await firebaseClient
                        .Child("user_status")
                        .Child(escapedContactEmail)
                        .OnceSingleAsync<UserStatusData>();

                    if (existingStatus == null)
                    {
                        Debug.WriteLine($"No status found for {escapedContactEmail}. Initializing default status...");
                        var defaultStatus = new UserStatusData
                        {
                            isOnline = false,
                            last_active = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        };

                        await firebaseClient
                            .Child("user_status")
                            .Child(escapedContactEmail)
                            .PutAsync(defaultStatus);
                    }
                }
                catch (Exception initEx)
                {
                    Debug.WriteLine($"Error initializing status for {escapedContactEmail}: {initEx.Message}");
                    continue;
                }

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

            if (messageSubscription != null)
            {
                Debug.WriteLine("Disposing messageSubscription.");
                messageSubscription.Dispose();
                messageSubscription = null;
            }

            foreach (var listener in _contactPresenceListeners.Values)
            {
                listener?.Dispose();
            }
            _contactPresenceListeners.Clear();

            EditProfileViewModel.AvatarUpdated -= OnAvatarUpdated;

            if (firebaseClient == null)
            {
                Debug.WriteLine("firebaseClient is null during Cleanup.");
            }
            else
            {
                Debug.WriteLine("firebaseClient exists during Cleanup.");
            }

            Debug.WriteLine("ChatViewModel cleanup complete.");
        }

        [RelayCommand]
        private void ViewFullImage(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                Debug.WriteLine("Empty image URL provided");
                return;
            }
            try
            {
                var viewer = new Window
                {
                    Title = "Image Viewer",
                    Width = 800,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                var grid = new Grid();

                var viewbox = new System.Windows.Controls.Viewbox
                {
                    Stretch = Stretch.Uniform
                };

                var image = new System.Windows.Controls.Image
                {
                    Source = new BitmapImage(new Uri(imageUrl)),
                    Stretch = Stretch.Uniform
                };

                viewbox.Child = image;
                grid.Children.Add(viewbox);
                viewer.Content = grid;

                viewer.Show();

                LogViewedImage(imageUrl);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening image viewer: {ex.Message}");
                CustomMessageBox.Show($"Không thể mở ảnh: {ex.Message}", "Lỗi", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
            }
        }

        private void LogViewedImage(string imageUrl)
        {
            try
            {
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {imageUrl}";

                _viewedImageHistory.Add(logEntry);

                string directory = Path.GetDirectoryName(_imageHistoryFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.AppendAllText(_imageHistoryFilePath, logEntry + Environment.NewLine);

                Debug.WriteLine($"Image view logged: {logEntry}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error logging viewed image: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task DownloadFileAsync(FileItem fileItem)
        {
            if (fileItem == null || string.IsNullOrEmpty(fileItem.DownloadUrl))
            {
                CustomMessageBox.Show("Không có liên kết tải xuống cho tệp này.", "Thông báo", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Information);
                return;
            }

            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                string fileName = fileItem.FileName;

                if (fileItem.IsVideo || IsVideoFile(fileItem.FileExtension))
                {
                    if (string.IsNullOrEmpty(Path.GetExtension(fileName)))
                    {
                        fileName = Path.ChangeExtension(fileName, fileItem.FileExtension ?? ".mp4");
                    }
                }
                else if (IsImageFile(fileItem.FileExtension))
                {
                    if (string.IsNullOrEmpty(Path.GetExtension(fileName)))
                    {
                        fileName = Path.ChangeExtension(fileName, fileItem.FileExtension ?? ".jpg");
                    }
                }
                else if (!string.IsNullOrEmpty(fileItem.FileExtension) &&
                         !fileName.EndsWith(fileItem.FileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    fileName = Path.ChangeExtension(fileName, fileItem.FileExtension.TrimStart('.'));
                }

                bool success = await FirebaseStorageHelper.DownloadFileToLocationAsync(
                    fileItem.DownloadUrl,
                    fileName);

                if (success)
                {
                    Debug.WriteLine($"File downloaded successfully: {fileName}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error downloading file: {ex.Message}");
                CustomMessageBox.Show($"Không thể tải xuống tệp tin: {ex.Message}", "Lỗi", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        [RelayCommand]
        private async Task DownloadMessageFileAsync(Message message)
        {
            if (message == null || (string.IsNullOrEmpty(message.Content) && !message.IsImage && !message.IsVideo))
            {
                CustomMessageBox.Show("Không có tệp tin để tải xuống.", "Thông báo", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Information);
                return;
            }

            string url = null;
            string fileName = null;
            string extension = null;

            if (message.IsImage)
            {
                url = message.ImageUrl;
                extension = Path.GetExtension(message.Content);
                if (string.IsNullOrEmpty(extension)) extension = ".jpg";
                fileName = $"image_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
            }
            else if (message.IsVideo)
            {
                url = message.VideoUrl;
                extension = Path.GetExtension(message.Content);
                if (string.IsNullOrEmpty(extension)) extension = ".mp4";
                fileName = $"video_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
            }
            else
            {
                url = message.FileUrl;
                fileName = message.Content;

                extension = Path.GetExtension(fileName);
                if (string.IsNullOrEmpty(extension))
                {
                    var fileItem = Files.FirstOrDefault(f => f.FileName == message.Content);
                    if (fileItem != null && !string.IsNullOrEmpty(fileItem.FileExtension))
                    {
                        extension = fileItem.FileExtension;
                        fileName = Path.ChangeExtension(fileName, extension.TrimStart('.'));
                    }
                }
            }

            if (string.IsNullOrEmpty(url))
            {
                var fileItem = Files.FirstOrDefault(f => f.FileName == message.Content);
                if (fileItem != null && !string.IsNullOrEmpty(fileItem.DownloadUrl))
                {
                    url = fileItem.DownloadUrl;
                    if (!string.IsNullOrEmpty(fileItem.FileExtension))
                    {
                        extension = fileItem.FileExtension;
                    }
                }
            }

            if (string.IsNullOrEmpty(url))
            {
                CustomMessageBox.Show("Không tìm thấy đường dẫn tải xuống cho tệp này.", "Thông báo", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Information);
                return;
            }

            if (!string.IsNullOrEmpty(extension) && !fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                fileName = Path.ChangeExtension(fileName, extension.TrimStart('.'));
            }

            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                bool success = await FirebaseStorageHelper.DownloadFileToLocationAsync(url, fileName);

                if (success)
                {
                    Debug.WriteLine($"File downloaded successfully: {fileName}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error downloading file: {ex.Message}");
                CustomMessageBox.Show($"Không thể tải xuống tệp tin: {ex.Message}", "Lỗi", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        [RelayCommand]
        private void ViewVideo(string videoUrl)
        {
            if (string.IsNullOrEmpty(videoUrl))
            {
                Debug.WriteLine("Empty video URL provided");
                return;
            }

            try
            {
                string tempPath = Path.Combine(
                    Path.GetTempPath(),
                    $"TempVideo_{DateTime.Now:yyyyMMddHHmmssfff}.mp4");

                Mouse.OverrideCursor = Cursors.Wait;

                try
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            byte[] videoData = await FirebaseStorageHelper.DownloadFileAsync(videoUrl);
                            await File.WriteAllBytesAsync(tempPath, videoData);

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = tempPath,
                                    UseShellExecute = true
                                });

                                LogViewedVideo(videoUrl);

                                Mouse.OverrideCursor = null;
                            });
                        }
                        catch (Exception ex)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Debug.WriteLine($"Error playing video: {ex.Message}");
                                CustomMessageBox.Show($"Không thể phát video: {ex.Message}", "Lỗi", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
                                Mouse.OverrideCursor = null;
                            });
                        }
                    });
                }
                catch (Exception ex)
                {
                    Mouse.OverrideCursor = null;
                    Debug.WriteLine($"Error setting up video playback: {ex.Message}");
                    CustomMessageBox.Show($"Không thể chuẩn bị phát video: {ex.Message}", "Lỗi", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                Debug.WriteLine($"Error preparing video playback: {ex.Message}");
                CustomMessageBox.Show($"Không thể chuẩn bị phát video: {ex.Message}", "Lỗi", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
            }
        }

        private string GetLocalizedString(string key)
        {
            if (Application.Current.Resources["LocalizationDictionary"] is ResourceDictionary localizationDict)
            {
                return localizationDict.Contains(key) ? localizationDict[key].ToString() : key;
            }
            return key;
        }

        private void LogViewedVideo(string videoUrl)
        {
            try
            {
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - VIDEO: {videoUrl}";

                _viewedImageHistory.Add(logEntry);

                string directory = Path.GetDirectoryName(_imageHistoryFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.AppendAllText(_imageHistoryFilePath, logEntry + Environment.NewLine);

                Debug.WriteLine($"Video view logged: {logEntry}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error logging viewed video: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task DeleteContactAsync(Contact contactToDelete)
        {
            if (contactToDelete == null) return;
            var result = CustomMessageBox.Show(
                $"Bạn có chắc chắn muốn xóa liên hệ '{contactToDelete.Name}' không? Toàn bộ lịch sử trò chuyện sẽ bị xóa vĩnh viễn và không thể khôi phục.",
                "Xác nhận xóa",
                CustomMessageBoxWindow.MessageButtons.YesNo,
                CustomMessageBoxWindow.MessageIcon.Warning);

            if (result == MessageBoxResult.No) return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                string currentUserEmail = SharedData.Instance.userdata.Email;
                string contactEmail = contactToDelete.Email;
                string chatID = contactToDelete.chatID;
                await DeleteChatHistoryAsync(chatID);
                await DeleteContactEntryAsync(currentUserEmail, contactEmail);
                await DeleteContactEntryAsync(contactEmail, currentUserEmail);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Contacts.Remove(contactToDelete);
                    if (SelectedContact == contactToDelete)
                    {
                        SelectedContact = null;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi khi xóa liên hệ: {ex.Message}");
                CustomMessageBox.Show("Đã xảy ra lỗi trong quá trình xóa liên hệ.", "Lỗi", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
        private async Task DeleteChatHistoryAsync(string chatID)
        {
            if (string.IsNullOrEmpty(chatID)) return;
            Debug.WriteLine($"Đang xóa lịch sử chat: messages/{chatID}");
            await firebaseClient.Child("messages").Child(chatID).DeleteAsync();
        }

        private async Task DeleteContactEntryAsync(string ownerEmail, string contactEmailToDelete)
        {
            if (string.IsNullOrEmpty(ownerEmail) || string.IsNullOrEmpty(contactEmailToDelete)) return;

            var db = FirestoreHelper.database;
            Debug.WriteLine($"Đang xóa entry: users/{ownerEmail}/contacts/{contactEmailToDelete}");
            await db.Collection("users").Document(ownerEmail).Collection("contacts").Document(contactEmailToDelete).DeleteAsync();
        }            

        [RelayCommand]
        private async Task DeleteMessageAsync(Message messageToDelete)
        {
            if (messageToDelete == null) return;
                       
            if (!messageToDelete.IsMine)
            {
                CustomMessageBox.Show("Bạn không thể xóa tin nhắn của người khác.", "Thông báo", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Information);
                return;
            }
                       
            var result = CustomMessageBox.Show(
                "Bạn có chắc chắn muốn xóa tin nhắn này không? Hành động này không thể hoàn tác.",
                "Xác nhận xóa",
                CustomMessageBoxWindow.MessageButtons.YesNo,
                CustomMessageBoxWindow.MessageIcon.Warning);

            if (result == MessageBoxResult.No) return;

            try
            {
                await firebaseClient
                    .Child("messages")
                    .Child(SelectedContact.chatID)
                    .Child(messageToDelete.Id)
                    .DeleteAsync();

                Debug.WriteLine($"Yêu cầu xóa tin nhắn {messageToDelete.Id} đã được gửi thành công.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi khi xóa tin nhắn trên server: {ex.Message}");
                CustomMessageBox.Show("Đã có lỗi xảy ra trong quá trình xóa tin nhắn.", "Lỗi", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
            }
        }
    }
}