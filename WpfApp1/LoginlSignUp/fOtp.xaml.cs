using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WpfApp1.Models;

namespace WpfApp1.LoginlSignUp
{
    /// <summary>
    /// Interaction logic for fOtp.xaml
    /// </summary>
    public partial class fOtp : Window
    {
        private string verificationId;
        private DispatcherTimer countdownTimer;
        private TimeSpan timeRemaining;
        private string phoneNumber;
        private User userData;
        private string idToken;
        private string userEmail;
        private const string apiKey = "AIzaSyCDIXwx-Zcv3Qcxt9e_y8eUNiNlnEXFDbw";
        public fOtp(string token, string email, User data)
        {
            InitializeComponent();
            idToken = token;
            userEmail = email;
            userData = data;

            EmailTextBlock.Text = $"Chúng tôi đã gửi một email xác thực đến: {userEmail}";

        }
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
                        // ✅ Ghi dữ liệu vào Firestore
                        var db = FirestoreHelper.database;
                        var docRef = db.Collection("users").Document(userData.Email);
                        await docRef.SetAsync(userData);

                        MessageBox.Show("Email đã xác minh và thông tin đã lưu!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Mở MainWindow
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
                MessageBox.Show("Lỗi kiểm tra xác minh: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    if (response != null)
                    {
                        MessageBox.Show("Da gui lai email thanh cong");
                    }
                    else MessageBox.Show("Khong gui duoc");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("loi" + ex);
            }
        }
    }
}
