using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Firebase.Database;

namespace WpfApp1.ViewModels
{
    public static class RealtimeDatabaseHelper
    {
        private static FirebaseClient _firebaseClient;
        private static readonly string FirebaseUrl = "https://chatapp-177-default-rtdb.asia-southeast1.firebasedatabase.app/";

        static RealtimeDatabaseHelper()
        {
            InitializeFirebase();
        }

        private static void InitializeFirebase()
        {
            try
            {
                _firebaseClient = new FirebaseClient(FirebaseUrl,
                    new FirebaseOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult<string>(null)
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing Firebase: {ex.Message}");
                throw;
            }
        }

        public static FirebaseClient GetClient()
        {
            if (_firebaseClient == null)
            {
                InitializeFirebase();
            }
            return _firebaseClient;
        }

        /// <summary>
        /// Sanitizes email for use in Firebase database paths
        /// </summary>
        public static string SanitizeEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return string.Empty;

            return email.Replace(".", ",")
                       .Replace("#", "_")
                       .Replace("$", "_")
                       .Replace("[", "_")
                       .Replace("]", "_");
        }
    }
}
