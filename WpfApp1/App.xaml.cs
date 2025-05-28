using System.Configuration;
using System.Data;
using System.Windows;
using WpfApp1.Models;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Starting application...");

            // Gọi base.OnStartup TRƯỚC để khởi tạo Application.Current
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
    }
}