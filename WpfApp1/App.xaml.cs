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
            base.OnStartup(e);

            // Khởi tạo FirestoreHelper
            FirestoreHelper.Initialize();

            // Kiểm tra xem kết nối có thành công không trước khi mở cửa sổ chính
            if (FirestoreHelper.database == null)
            {
                MessageBox.Show("Không thể kết nối tới cơ sở dữ liệu Firestore. Vui lòng kiểm tra thông tin lỗi trong Output window (Debug) hoặc liên hệ quản trị viên. Ứng dụng có thể không hoạt động đúng.",
                                "Lỗi Kết Nối Cơ Sở Dữ Liệu", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

}
