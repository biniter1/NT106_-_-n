using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using WpfApp1.Models;
using WpfApp1.Views;

namespace WpfApp1.LoginlSignUp
{
    /// <summary>
    /// Interaction logic for fOtp.xaml
    /// </summary>
    public partial class fOtp : Window
    {
        // ... Toàn bộ các biến và constructor của bạn giữ nguyên ...
        private string verificationId;
        private System.Windows.Threading.DispatcherTimer countdownTimer;
        private TimeSpan timeRemaining;
        private string phoneNumber;
        private User userData;
        private string idToken;
        private string userEmail;
        private const string apiKey = "AIzaSyCDIXwx-Zcv3Qcxt9e_y8eUNiNlnEXFDbw";
        public fOtp(string token, string email, User data)
        {
            InitializeComponent();
            // LocalizationManager.LanguageChanged += OnLanguageChanged;
            idToken = token;
            userEmail = email;
            userData = data;
            EmailTextBlock.Text = $"Chúng tôi đã gửi một email xác thực đến: {userEmail}";
        }

        // ... OnLanguageChanged và UpdateBindings giữ nguyên ...

        private async void CheckVerifyButton_Click(object sender, RoutedEventArgs e)
        {
            StatusTextBlock.Text = "";
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = $"https://identitytoolkit.googleapis.com/v1/accounts:lookup?key={apiKey}";
                    var payload = new { idToken = idToken };
                    var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync(url, content);
                    string result = await response.Content.ReadAsStringAsync();
                    dynamic json = JsonConvert.DeserializeObject(result);

                    if (json.users != null && json.users[0].emailVerified == true)
                    {
                        var db = FirestoreHelper.database;
                        var docRef = db.Collection("users").Document(userData.Email);
                        await docRef.SetAsync(userData);

                        // THAY THẾ 1
                        CustomMessageBox.Show("Email đã xác minh và thông tin đã lưu!", "Thành công", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Success);

                        fLogin main = new fLogin();
                        this.Close();
                        main.Show();
                    }
                    else
                    {
                        StatusTextBlock.Text = "Email chưa được xác minh. Vui lòng kiểm tra lại hộp thư.";
                    }
                }
            }
            catch (Exception ex)
            {
                // THAY THẾ 2
                CustomMessageBox.Show("Lỗi kiểm tra xác minh: " + ex.Message, "Lỗi", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = $"https://identitytoolkit.googleapis.com/v1/accounts:lookup?key={apiKey}";
                    var payload = new { idToken = idToken };
                    var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync(url, content);
                    if (response != null && response.IsSuccessStatusCode) // Nên kiểm tra IsSuccessStatusCode
                    {
                        // THAY THẾ 3
                        CustomMessageBox.Show("Đã gửi lại email thành công", "Thông báo", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Information);
                    }
                    else
                    {
                        // THAY THẾ 4
                        CustomMessageBox.Show("Không gửi được email. Vui lòng thử lại.", "Lỗi", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                // THAY THẾ 5
                CustomMessageBox.Show("Lỗi: " + ex.Message, "Lỗi hệ thống", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
            }
        }
    }
}