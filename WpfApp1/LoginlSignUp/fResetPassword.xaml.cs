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
        private async void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            string code = GenerateOtpCode();
            await FirestoreHelper.database.Collection("otp_codes").Document(txtEmail.Text).SetAsync(new
            {
                otp = code,
                createdAt = Google.Cloud.Firestore.Timestamp.GetCurrentTimestamp()
            });
            if (String.IsNullOrEmpty(txtEmail.Text))
            {
                MessageBox.Show("Vui loi nhap email", "Loi");
                return;
            }
            try
            {
                SendOtpEmail(txtEmail.Text, code);
                this.Hide();
                LoginlSignUp.fCode f = new fCode(code, txtEmail.Text);
                f.Show();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Đã xảy ra lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
