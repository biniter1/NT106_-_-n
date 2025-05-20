using Google.Cloud.Firestore;
using System;
using System.Diagnostics; 
using System.IO;        
using System.Windows; 

namespace WpfApp1.Models
{
    internal static class FirestoreHelper
    {
        public static FirestoreDb database { get; private set; }
        private static readonly string ProjectId = "fir-5b855";

        // THAY TÊN TỆP JSON CỦA BẠN VÀO ĐÂY
        private static readonly string CredentialFileName = "firebase-credentials.json";

        private static bool isInitialized = false;

        public static void Initialize()
        {
            if (isInitialized)
            {
                Debug.WriteLine("FirestoreHelper đã được khởi tạo trước đó.");
                return;
            }

            try
            {
                // Tạo đường dẫn đầy đủ đến tệp credentials trong thư mục build
                string credentialPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CredentialFileName);

                if (!File.Exists(credentialPath))
                {
                    string errorMessage = $"LỖI NGHIÊM TRỌNG: Không tìm thấy tệp khóa tài khoản dịch vụ tại: {credentialPath}\n" +
                                          $"Hãy đảm bảo bạn đã thêm tệp '{CredentialFileName}' vào dự án và đặt thuộc tính 'Copy to Output Directory' của nó thành 'Copy if newer' hoặc 'Copy always'.";
                    Debug.WriteLine(errorMessage);
                    // Sử dụng Dispatcher để hiển thị MessageBox từ một luồng không phải UI 
                    Application.Current?.Dispatcher.Invoke(() =>
                        MessageBox.Show(errorMessage, "Lỗi Khởi Tạo Firestore", MessageBoxButton.OK, MessageBoxImage.Error)
                    );
                    return; // Không thể tiếp tục nếu không có file credentials
                }

                // Thiết lập biến môi trường GOOGLE_APPLICATION_CREDENTIALS để SDK tự động nhận diện
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialPath);

                // Khởi tạo FirestoreDb
                database = FirestoreDb.Create(ProjectId); // SDK sẽ sử dụng biến môi trường ở trên

                isInitialized = true;
                Debug.WriteLine($"FirestoreHelper đã khởi tạo thành công cho dự án: {ProjectId} với tệp credentials: {credentialPath}");
                
            }
            catch (Exception ex)
            {              
                string detailedErrorMessage = $"Lỗi khi khởi tạo FirestoreHelper:\n" +
                                              $"Message: {ex.Message}\n" +
                                              $"Stack Trace: {ex.StackTrace}\n";

                if (ex.InnerException != null)
                {
                    detailedErrorMessage += $"\nInner Exception Message: {ex.InnerException.Message}\n" +
                                            $"Inner Exception Stack Trace: {ex.InnerException.StackTrace}";
                }

                Debug.WriteLine("------------------- LỖI KHỞI TẠO FIREBASE -------------------");
                Debug.WriteLine(detailedErrorMessage);
                Debug.WriteLine("--------------------------------------------------------------");

                // Hiển thị lỗi cho người dùng
                Application.Current?.Dispatcher.Invoke(() =>
                    MessageBox.Show($"Không thể khởi tạo kết nối đến Firestore.\nChi tiết lỗi đã được ghi vào Output window (Debug).\n\nLỗi chính: {ex.Message}",
                                    "Lỗi Khởi Tạo Firestore", MessageBoxButton.OK, MessageBoxImage.Error)
                );

                database = null; // Đảm bảo database là null nếu khởi tạo thất bại
                isInitialized = false;
            }
        }
    }
}