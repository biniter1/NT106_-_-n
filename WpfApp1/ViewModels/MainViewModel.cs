using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using WpfApp1.Models;
using WpfApp1.Services;
using WpfApp1.ViewModels;
using WpfApp1.Views;

namespace WpfApp1.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // Thuộc tính quan trọng: Giữ ViewModel của View đang hiển thị trong ContentControl
        [ObservableProperty]
        private ObservableObject _currentViewModel;

        private readonly FirebaseClient _firebaseClient;

        // Giữ các instance của ViewModel con để tái sử dụng
        public ChatViewModel ChatVm { get; private set; }
        public SettingsViewModel SettingsVm { get; private set; }
        public MatchingChatViewModel MatchingChatVm { get; }
        public FriendListViewModel FriendListVm { get; private set; }
        public AIChatViewModel AIChatVm { get; }
        public class IncomingCallEventArgs : EventArgs
        {
            public CallSignal Call { get; }
            public IncomingCallEventArgs(CallSignal call) { Call = call; }
        }
        private IDisposable _callListener;

        private readonly NotificationService _notificationService;
        private IDisposable _notificationListener;

        [ObservableProperty] private bool _isNotificationPopupVisible;
        [ObservableProperty] private string _notificationMessage;
        [ObservableProperty] private int _unreadNotificationCount;
        [ObservableProperty] private bool _isAIChatVisible;
        public ObservableCollection<Notification> AllNotifications { get; } = new();

        // THÊM 1: KHAI BÁO EVENT ĐỂ MAINWINDOW LẮNG NGHE
        public event EventHandler<NewMessageEventArgs> ShowNotificationRequested;
        public event EventHandler<IncomingCallEventArgs> IncomingCallReceived;
        public MainViewModel(FirebaseClient firebaseClient)
        {
            _firebaseClient = firebaseClient;

            ChatVm = new ChatViewModel(_firebaseClient);
            SettingsVm = new SettingsViewModel(ChatVm, _firebaseClient);
            FriendListVm = new FriendListViewModel();

            string currentEmail = SharedData.Instance.userdata.Email;
            string safeKey = EscapeEmail(currentEmail);
            MatchingChatVm = new MatchingChatViewModel(_firebaseClient, safeKey);
            MatchingChatVm.MatchFound += OnMatchFound;

            AIChatVm = new AIChatViewModel();
            IsAIChatVisible = false;

            CurrentViewModel = ChatVm;
            _notificationService = new NotificationService(firebaseClient);
            _notificationListener = _notificationService.ListenForNotifications(safeKey, (notification) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (!notification.IsRead && !AllNotifications.Any(n => n.Id == notification.Id))
                    {
                        AllNotifications.Insert(0, notification);
                        UnreadNotificationCount = AllNotifications.Count(n => !n.IsRead);
                        NotificationMessage = notification.Message;
                        IsNotificationPopupVisible = true;
                    }
                });
            });

            EventAggregator.Instance.Subscribe<StartChatEvent>(OnStartChatReceived);

            if (ChatVm != null)
            {
                ChatVm.NewMessageNotificationRequested += OnNewMessageNotificationFromChat;
            }
            ListenForIncomingCalls();
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

        private void ListenForIncomingCalls()
        {
            var currentUser = SharedData.Instance.userdata;
            if (currentUser == null || string.IsNullOrEmpty(currentUser.Email)) return;

            string safeEmail = EscapeEmail(currentUser.Email);

            _callListener = _firebaseClient
                .Child("calls")
                .Child(safeEmail)
                .AsObservable<CallSignal>()
                .Subscribe(callEvent =>
                {
                    if (callEvent.EventType == FirebaseEventType.InsertOrUpdate && callEvent.Object != null)
                    {
                        var call = callEvent.Object;
                        if (call.Status == "Ringing")
                        {
                            Debug.WriteLine($"Incoming call received from {call.CallerName}. Call ID: {call.CallId}");
                            IncomingCallReceived?.Invoke(this, new IncomingCallEventArgs(call));
                        }
                    }
                }, ex =>
                {
                    Debug.WriteLine($"Error listening for calls: {ex.Message}");
                });
        }

        public void OpenChat(string chatID)
        {
            if (string.IsNullOrEmpty(chatID)) return;

            Debug.WriteLine($"MainViewModel: Received request to open chat for ID: {chatID}");
            var contactToOpen = ChatVm?.Contacts.FirstOrDefault(c => c.chatID == chatID);

            if (contactToOpen != null)
            {
                Debug.WriteLine($"Found contact: {contactToOpen.Name}. Initiating chat.");
                ChatVm.InitiateChatWith(contactToOpen);
                CurrentViewModel = ChatVm;
            }
            else
            {
                Debug.WriteLine($"Contact with ChatID {chatID} not found in the current contact list.");
            }
        }

        private void OnNewMessageNotificationFromChat(object sender, NewMessageEventArgs e)
        {
            ShowNotificationRequested?.Invoke(this, e);
        }

        private void OnStartChatReceived(StartChatEvent e)
        {
            if (e?.FriendToChat == null) return;
            ChatVm.InitiateChatWith(e.FriendToChat);
            CurrentViewModel = ChatVm;
        }

        [RelayCommand]
        private void ToggleAIChat()
        {
            IsAIChatVisible = !IsAIChatVisible;
        }

        [RelayCommand]
        private void ShowChat() => CurrentViewModel = ChatVm;

        [RelayCommand]
        private void ShowFriendList() => CurrentViewModel = FriendListVm;

        [RelayCommand]
        private void ShowAddFriendPopup()
        {
            var addFriendWin = new AddFriendWindow();
            if (Application.Current.MainWindow != null)
            {
                addFriendWin.Owner = Application.Current.MainWindow;
            }
            addFriendWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            addFriendWin.Show();
        }

        private void OnMatchFound(string roomId, string opponentId)
        {
            CustomMessageBox.Show($"Đã ghép cặp thành công với {opponentId}! Vào phòng chat: {roomId}",
                                "Thành công", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Information);
            CurrentViewModel = ChatVm;
        }

        [RelayCommand]
        private void ShowSettingsPopup()
        {
            var settingsVM = new SettingsViewModel(ChatVm, _firebaseClient);
            var settingsWin = new SettingsWindow(ChatVm, _firebaseClient)
            {
                DataContext = settingsVM
            };

            if (Application.Current.MainWindow != null)
            {
                settingsWin.Owner = Application.Current.MainWindow;
            }
            settingsWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            settingsWin.Show();
        }

        [RelayCommand]
        private void ShowSettings()
        {
            CurrentViewModel = SettingsVm;
        }

        [RelayCommand]
        private void ShowCreateGroupPopup()
        {
            var createGroupWindow = new Views.CreateGroupWindow
            {
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            createGroupWindow.ShowDialog();
        }

        public void Cleanup()
        {
            _notificationListener?.Dispose();
            System.Diagnostics.Debug.WriteLine("MainViewModel: Starting Cleanup...");

            // THÊM 4: HỦY ĐĂNG KÝ SỰ KIỆN KHI DỌN DẸP ĐỂ TRÁNH MEMORY LEAK
            if (ChatVm != null)
            {
                ChatVm.NewMessageNotificationRequested -= OnNewMessageNotificationFromChat;
            }
            _callListener?.Dispose();
            ChatVm?.Cleanup();
            System.Diagnostics.Debug.WriteLine("MainViewModel: Cleanup of cached child ViewModels complete.");
        }
    }
}