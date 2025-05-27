using Firebase.Storage;
using System;
using System.IO;
using System.Threading.Tasks;

namespace WpfApp1.Models
{
    public static class FirebaseStorageHelper
    {
        private static readonly string Bucket = "chatapp-177.firebasestorage.app";
        private static readonly FirebaseStorage storage = new FirebaseStorage("chatapp-177.firebasestorage.app");

        public static async Task<string> UploadFileAsync(string localFilePath, string fileName)
        {
            var storage = new FirebaseStorage(Bucket);
            using var stream = File.OpenRead(localFilePath);
            await storage
                .Child(fileName)
                .PutAsync(stream);

            // Return the gs:// URI instead of the download URL
            return $"https://chatapp-177.firebasestorage.app/v0/b/{fileName}?alt=media";
        }

        public static async Task<byte[]> DownloadFileAsync(string url)
        {
            using var client = new System.Net.Http.HttpClient();
            return await client.GetByteArrayAsync(url);
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
