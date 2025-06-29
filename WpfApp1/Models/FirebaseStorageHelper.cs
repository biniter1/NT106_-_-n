using Firebase.Storage;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Windows;

namespace WpfApp1.Models
{
    public static class FirebaseStorageHelper
    {
        private static readonly FirebaseStorage storage = new FirebaseStorage("chatapp-177.firebasestorage.app");

        public static async Task<string> UploadFileAsync(string localFilePath, string fileName)
        {
            using var stream = File.OpenRead(localFilePath);

            var task = storage
                .Child("files")
                .Child(fileName)
                .PutAsync(stream);

            // Optional: theo dõi tiến trình
            task.Progress.ProgressChanged += (s, e) =>
            {
                Console.WriteLine($"Progress: {e.Percentage} %");
            };

            // Lấy URL file đã upload
            var downloadUrl = await task;
            return downloadUrl;
        }

        public static async Task<byte[]> DownloadFileAsync(string url)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                System.Diagnostics.Debug.WriteLine($"Attempting to download from URL: {url}");
                return await client.GetByteArrayAsync(url);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in DownloadFileAsync: {ex.Message}");
                throw;
            }
        }

        public static async Task<bool> DownloadFileToLocationAsync(string url, string fileName)
        {
            try
            {
                // Show file save dialog
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    FileName = fileName,
                    Filter = GetFilterForFileName(fileName),
                    Title = "Lưu tệp tin"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    System.Diagnostics.Debug.WriteLine($"Downloading file from: {url}");

                    // Download the file bytes
                    byte[] fileData = await DownloadFileAsync(url);

                    System.Diagnostics.Debug.WriteLine($"Downloaded {fileData.Length} bytes, saving to: {saveFileDialog.FileName}");

                    // Write to the selected location
                    await File.WriteAllBytesAsync(saveFileDialog.FileName, fileData);

                    MessageBox.Show($"Tệp tin {fileName} đã được tải xuống thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error downloading file: {ex.Message}");
                MessageBox.Show($"Không thể tải xuống tệp: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private static string GetFilterForFileName(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();

            return extension switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "Image files|*.jpg;*.jpeg;*.png;*.gif;*.bmp|All files|*.*",
                ".pdf" => "PDF files|*.pdf|All files|*.*",
                ".docx" or ".doc" => "Word documents|*.docx;*.doc|All files|*.*",
                ".xlsx" or ".xls" => "Excel files|*.xlsx;*.xls|All files|*.*",
                ".pptx" or ".ppt" => "PowerPoint presentations|*.pptx;*.ppt|All files|*.*",
                ".txt" => "Text files|*.txt|All files|*.*",
                ".zip" or ".rar" => "Archive files|*.zip;*.rar|All files|*.*",
                _ => "All files|*.*"
            };
        }

        public static async Task<string> GetAvatarUrlAsync(string userEmail)
        {
            try
            {
                string filePath = $"avatars/{userEmail}.jpg"; // Đảm bảo định dạng khớp với EditProfileViewModel
                var url = await storage.Child(filePath).GetDownloadUrlAsync();
                return url;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching avatar for {userEmail}: {ex.Message}");
                return null; // Hoặc trả về URL avatar mặc định
            }
        }

        public static string EscapeEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return string.Empty;
            return email.Replace('.', ',')
                        .Replace('#', '_')
                        .Replace('$', '_')
                        .Replace('[', '_')
                        .Replace(']', '_')
                        .Replace('/', '_');
        }
    }
}
