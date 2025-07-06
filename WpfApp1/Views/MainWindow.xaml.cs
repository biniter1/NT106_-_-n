using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using ToastNotifications.Core;
using WpfApp1.Models;
using WpfApp1.Services;
using WpfApp1.ViewModels;
using WpfApp1.Views;
using static WpfApp1.ViewModels.MainViewModel;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string CurrentEmail;

        public MainWindow(string email)
        {
            InitializeComponent();
            CurrentEmail = email;
            Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
            LocalizationManager.LanguageChanged += OnLanguageChanged;
        }

        private string EscapeEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return string.Empty;
            return email.Replace('.', '_').Replace('@', '_').Replace('#', '_').Replace('$', '_').Replace('[', '_').Replace(']', '_').Replace('/', '_');
        }

        private async void HandleCallResponse(CallSignal call, bool accepted)
        {
            string calleeSafeEmail = EscapeEmail(call.CalleeId);
            await App.AppFirebaseClient.Child("calls").Child(calleeSafeEmail).Child(call.CallId).DeleteAsync();

            if (accepted)
            {
                var webRtcService = new WebRTCService(App.AppFirebaseClient, call);
                var callViewModel = new CallViewModel(webRtcService);
                var callWindow = new CallWindow(callViewModel);

                await webRtcService.InitializeAsync();
                await webRtcService.AddAudioTrackAsync();
                if (call.CallType == "Video")
                {
                    await webRtcService.AddVideoTrackAsync();
                }
                webRtcService.OnIceCandidateReady += async (candidate) =>
                {
                    try
                    {
                        string callerSafeEmail = EscapeEmail(call.CalleeId);
                        var iceSignal = IceCandidateSignal.FromWebRtcCandidate(candidate);
                        await App.AppFirebaseClient
                            .Child("ice_candidates")
                            .Child(callerSafeEmail) // Gửi về cho người gọi
                            .Child(call.CallId)
                            .PostAsync(iceSignal);
                    }
                    catch (Exception ex) { Debug.WriteLine($"Error sending ICE candidate: {ex.Message}"); }
                };
                ListenForRemoteIceCandidates(webRtcService, call.CallerId, call.CallId);

                webRtcService.OnSdpAnswerReady += async (sdpAnswer) =>
                {
                    call.Status = "Accepted";
                    call.SdpAnswer = sdpAnswer;
                    try
                    {
                        string callerSafeEmail = EscapeEmail(call.CallerId);
                        await App.AppFirebaseClient
                            .Child("calls")
                            .Child(callerSafeEmail)
                            .Child(call.CallId)
                            .PutAsync(call);

                        Debug.WriteLine("Sent SDP Answer back to caller.");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error sending SDP Answer: {ex.Message}");
                        callWindow.Close();
                    }
                };

                await webRtcService.HandleOfferAsync(call.SdpOffer);
                callWindow.Show();
            }
            else
            {
                call.Status = "Declined";
                string callerSafeEmail = call.CallerId.Replace('.', '_');
                await App.AppFirebaseClient.Child("calls").Child(callerSafeEmail).Child(call.CallId).PutAsync(call);
            }
        }

        private void ListenForRemoteIceCandidates(WebRTCService service, string remoteUserEmail, string callId)
        {
            string currentUserSafeEmail = EscapeEmail(SharedData.Instance.userdata.Email);
            App.AppFirebaseClient
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
                        service.AddIceCandidateAsync(remoteCandidate);
                    }
                });
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            InvalidateVisual();
            UpdateBindings();
        }

        private void UpdateBindings()
        {
            foreach (var element in LogicalTreeHelper.GetChildren(this))
            {
                if (element is FrameworkElement fe)
                {
                    fe.GetBindingExpression(FrameworkElement.DataContextProperty)?.UpdateTarget();
                }
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataCurrentUser(CurrentEmail);

            if (App.AppFirebaseClient == null)
            {
                CustomMessageBox.Show("Lỗi khởi tạo Firebase: FirebaseClient chưa được thiết lập. Vui lòng đăng nhập lại.",
                                    "Lỗi Firebase",
                                    CustomMessageBoxWindow.MessageButtons.OK,
                                    CustomMessageBoxWindow.MessageIcon.Error);
                Application.Current.Shutdown();
                return;
            }

            var mainViewModel = new MainViewModel(App.AppFirebaseClient);
            this.DataContext = mainViewModel;

            mainViewModel.ShowNotificationRequested += MainViewModel_ShowNotificationRequested;
            NotificationWindow.NotificationClicked += OnNotificationClicked;

            mainViewModel.IncomingCallReceived += MainViewModel_IncomingCallReceived;
        }

        private void MainViewModel_IncomingCallReceived(object sender, IncomingCallEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var callWindow = new IncomingCallWindow(e.Call);

                // Đăng ký sự kiện chấp nhận/từ chối từ cửa sổ popup
                callWindow.CallAccepted += (acceptedCall) => HandleCallResponse(acceptedCall, true);
                callWindow.CallDeclined += (declinedCall) => HandleCallResponse(declinedCall, false);

                callWindow.Show();
            });
        }

        private void OnNotificationClicked(string chatID)
        {
            // Đảm bảo chạy trên luồng UI
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Lấy MainViewModel từ DataContext
                if (this.DataContext is MainViewModel mainVm)
                {
                    // Gọi phương thức mới để mở chat
                    mainVm.OpenChat(chatID);
                }

                // Đưa cửa sổ chính lên phía trước
                if (this.WindowState == WindowState.Minimized)
                {
                    this.WindowState = WindowState.Normal;
                }
                this.Activate();
                this.Focus();
            });
        }

        // Xử lý khi nhận được yêu cầu hiển thị thông báo
        private void MainViewModel_ShowNotificationRequested(object sender, NewMessageEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ShowDesktopNotification(e.Title, e.Message, e.ChatID);
            });
        }

        public async Task LoadDataCurrentUser(string email)
        {
            var db = FirestoreHelper.database;
            if (db == null)
            {
                CustomMessageBox.Show("Firestore database is not initialized!",
                                    "Error",
                                    CustomMessageBoxWindow.MessageButtons.OK,
                                    CustomMessageBoxWindow.MessageIcon.Error);
                return;
            }

            try
            {
                var doc = db.Collection("users").Document(email);
                var snap = await doc.GetSnapshotAsync();
                if (snap.Exists)
                {
                    var user = snap.ConvertTo<User>();
                    SharedData.Instance.userdata = user;
                    Debug.WriteLine($"Loaded user data for: {user.Email}");
                }
                else
                {
                    Debug.WriteLine($"User document not found for email: {email}");
                    CustomMessageBox.Show($"User not found for email: {email}",
                                        "Error",
                                        CustomMessageBoxWindow.MessageButtons.OK,
                                        CustomMessageBoxWindow.MessageIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading user data: {ex.Message}");
                CustomMessageBox.Show($"Error loading user data: {ex.Message}",
                                    "Error",
                                    CustomMessageBoxWindow.MessageButtons.OK,
                                    CustomMessageBoxWindow.MessageIcon.Error);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AddFriendWindow addFriendWin = new AddFriendWindow();
            addFriendWin.Owner = this;
            addFriendWin.Show();
        }

        private void ShowDesktopNotification(string title, string message, string chatID)
        {
            try
            {
                Debug.WriteLine($"ShowDesktopNotification called: {title} - {message} - ChatID: {chatID}");
                CreateNotificationWindow(title, message, chatID);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing desktop notification: {ex.Message}");
            }
        }

        private void CreateNotificationWindow(string title, string message, string chatID)
        {
            try
            {
                // Truyền cả chatID vào constructor
                var notificationWindow = new NotificationWindow(title, message, chatID);
                notificationWindow.Show();
                Debug.WriteLine("NotificationWindow created and shown");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating notification window: {ex.Message}");
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                NotificationWindow.NotificationClicked -= OnNotificationClicked;

                NotificationWindow.CloseAllNotifications();

                if (this.DataContext is MainViewModel mainVM)
                {
                    mainVM.ShowNotificationRequested -= MainViewModel_ShowNotificationRequested;
                    mainVM.IncomingCallReceived -= MainViewModel_IncomingCallReceived;
                    mainVM.Cleanup();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during window closing: {ex.Message}");
            }
        }

        private void CloseAllNotifications()
        {
            NotificationWindow.CloseAllNotifications();
        }
    }
}