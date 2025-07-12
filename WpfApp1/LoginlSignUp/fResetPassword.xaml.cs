using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
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
using WpfApp1.Models; 
using WpfApp1.Views;
using System.Net.Http;
using Newtonsoft.Json;

namespace WpfApp1.LoginlSignUp
{
    /// <summary>
    /// Interaction logic for fResetPassword.xaml
    /// </summary>
    public partial class fResetPassword : Window
    {
        public fResetPassword()
        {
            InitializeComponent();
            
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

        private async void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            string email = txtEmail.Text.Trim();
            if (string.IsNullOrEmpty(email))
            {
                CustomMessageBox.Show("Vui lòng nhập địa chỉ email.", "Thông tin trống", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Warning);
                return;
            }

            // API Key của bạn
            string apiKey = "AIzaSyCDIXwx-Zcv3Qcxt9e_y8eUNiNlnEXFDbw";
            string url = $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={apiKey}";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Tạo payload để yêu cầu Firebase gửi email reset password
                    var payload = new
                    {
                        requestType = "PASSWORD_RESET",
                        email = email
                    };

                    var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync(url, content);
                    string result = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        // Thông báo thành công cho người dùng
                        CustomMessageBox.Show($"Một liên kết để đặt lại mật khẩu đã được gửi đến {email}. Vui lòng kiểm tra hộp thư của bạn.", "Thành Công", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Success);

                        // Đóng form này và mở lại form đăng nhập
                        fLogin loginWindow = new fLogin();
                        this.Close();
                        loginWindow.Show();
                    }
                    else
                    {
                        // Phân tích lỗi từ Firebase và hiển thị
                        dynamic errorJson = JsonConvert.DeserializeObject(result);
                        string errorMessage = errorJson.error.message;
                        CustomMessageBox.Show($"Lỗi: {errorMessage}", "Gửi Email Thất Bại", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Đã xảy ra lỗi: " + ex.Message, "Lỗi Hệ Thống", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
            }
        }

        public static string GenerateOtpCode()
        {
            Random rand = new Random();
            return rand.Next(100000, 999999).ToString();
        }
        public static void SendOtpEmail(string toEmail, string otp)
        {
            var fromAddress = new MailAddress("hasonbin123@gmail.com", "My App");
            var toAddress = new MailAddress(toEmail);
            const string fromPassword = "ykaszizhalpibhbu";
            const string subject = "Mã xác thực OTP của bạn";
            string body = $"Mã xác thực của bạn là: {otp}.\nMã sẽ hết hạn sau 5 phút.";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };

            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body
            })
            {
                smtp.Send(message);
            }
        }
        private void linkLogin_Click(object sender, RoutedEventArgs e)
        {
            LoginlSignUp.fLogin flogin = new fLogin();
            this.Hide();
            flogin.Show();
        }
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}