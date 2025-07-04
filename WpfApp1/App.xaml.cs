using System.Configuration;
using System.Data;
using System.Windows;
using Firebase.Database;
using WpfApp1.Models;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using System;
using System.Threading.Tasks;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static FirebaseClient AppFirebaseClient { get; private set; }
        public static Notifier AppNotifier { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Starting application...");
            AppFirebaseClient = new FirebaseClient("https://chatapp-177-default-rtdb.asia-southeast1.firebasedatabase.app/");

            AppNotifier = new Notifier(cfg =>
            {
                // SỬA LỖI: Thay thế WindowPositionProvider bằng PrimaryScreenPositionProvider
                // Điều này sẽ đảm bảo thông báo luôn xuất hiện ở góc màn hình chính.
                cfg.PositionProvider = new PrimaryScreenPositionProvider(
                    corner: Corner.BottomRight,
                    offsetX: 20, // Tăng khoảng cách một chút cho đẹp hơn
                    offsetY: 20);

                // Cấu hình thời gian hiển thị và số lượng thông báo tối đa
                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(3),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(5));

                cfg.Dispatcher = Application.Current.Dispatcher;

                // (Tùy chọn) Cấu hình giao diện chung
                cfg.DisplayOptions.TopMost = true; // Luôn hiển thị trên các cửa sổ khác
                cfg.DisplayOptions.Width = 350;
            });

            base.OnStartup(e);

            try
            {
                LocalizationManager.Initialize();
                LocalizationManager.CheckResourceDictionaries(); // Debug
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing localization: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            try
            {
                FirestoreHelper.Initialize();
                if (FirestoreHelper.database == null)
                {
                    string errorMessage = LocalizationManager.GetString("DatabaseConnectionError") ??
                        "Không thể kết nối tới cơ sở dữ liệu Firestore. Vui lòng kiểm tra thông tin lỗi trong Output window (Debug) hoặc liên hệ quản trị viên. Ứng dụng có thể không hoạt động đúng.";
                    string errorTitle = LocalizationManager.GetString("DatabaseConnectionErrorTitle") ??
                        "Lỗi Kết Nối Cơ Sở Dữ Liệu";
                    MessageBox.Show(errorMessage, errorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing Firestore: {ex.Message}");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Dispose the notifier when the application exits
            AppNotifier?.Dispose();
            base.OnExit(e);
        }

        // Các phương thức khác của bạn giữ nguyên
        public static void UpdateFirebaseClientAuth(string idToken)
        {
            AppFirebaseClient = new FirebaseClient(
                "https://chatapp-177-default-rtdb.asia-southeast1.firebasedatabase.app/",
                new FirebaseOptions { AuthTokenAsyncFactory = () => Task.FromResult(idToken) }
            );
            System.Diagnostics.Debug.WriteLine("AppFirebaseClient updated with new auth token.");
        }

        public static void ClearFirebaseClientAuth()
        {
            AppFirebaseClient = new FirebaseClient("https://chatapp-177-default-rtdb.asia-southeast1.firebasedatabase.app/");
            System.Diagnostics.Debug.WriteLine("AppFirebaseClient reverted to unauthenticated state.");
        }
    }
}
