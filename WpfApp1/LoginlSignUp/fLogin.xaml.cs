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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Newtonsoft.Json;
using WpfApp1.Models;

namespace WpfApp1.LoginlSignUp
{
    /// <summary>
    /// Interaction logic for fLogin.xaml
    /// </summary>
    public partial class fLogin : Window
    {
        public fLogin()
        {
            InitializeComponent();
            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                    this.DragMove();
            };
            txtUsername.Focus();
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Hiệu ứng fade out khi đóng
            DoubleAnimation fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3)
            };

            fadeOut.Completed += (s, _) => this.Close();
            this.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string email = txtUsername.Text.Trim();
            string password = txtPassword.Password.Trim();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin đăng nhập!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var db = FirestoreHelper.database;
                if (db == null)
                {
                    MessageBox.Show("Không thể kết nối đến cơ sở dữ liệu!", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var docRef = db.Collection("users").Document(email);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    MessageBox.Show("Tài khoản không tồn tại!", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var data = snapshot.ConvertTo<User>();
                string decryptedPassword = data.Password;
                if (password != decryptedPassword)
                {
                    MessageBox.Show("Mật khẩu không chính xác!", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Step 1: Sign in with Firebase Authentication to get a fresh idToken
                string idToken = await SignInWithFirebase(email, password);
                if (string.IsNullOrEmpty(idToken))
                {
                    return; // Error message already shown in SignInWithFirebase
                }

                // Step 2: Check email verification status with the fresh idToken
                using (HttpClient client = new HttpClient())
                {
                    string apiKey = "AIzaSyDbQedJtWK-vnQAbS_BpgQHCTBqyK8RPMg";
                    string url = $"https://identitytoolkit.googleapis.com/v1/accounts:lookup?key={apiKey}";

                    var lookupPayload = new
                    {
                        idToken = idToken
                    };

                    var content = new StringContent(JsonConvert.SerializeObject(lookupPayload), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, content);
                    string result = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        dynamic resJson = JsonConvert.DeserializeObject(result);
                        bool emailVerified = resJson.users[0].emailVerified;

                        if (!emailVerified)
                        {
                            MessageBox.Show("Email của bạn chưa được xác minh. Vui lòng kiểm tra hộp thư!", "Xác minh Email",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Không thể kiểm tra trạng thái email xác thực. Chi tiết lỗi: {result}", "Lỗi",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                // Đăng nhập thành công
                MessageBox.Show("Đăng nhập thành công!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                this.Hide();
                MainWindow main = new MainWindow(email);
                main.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<string> SignInWithFirebase(string email, string password)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string apiKey = "AIzaSyDbQedJtWK-vnQAbS_BpgQHCTBqyK8RPMg";
                    string url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}";

                    var signInPayload = new
                    {
                        email = email,
                        password = password,
                        returnSecureToken = true
                    };

                    var content = new StringContent(JsonConvert.SerializeObject(signInPayload), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, content);
                    string result = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        dynamic resJson = JsonConvert.DeserializeObject(result);
                        string idToken = resJson.idToken;
                        return idToken;
                    }
                    else
                    {
                        MessageBox.Show($"Đăng nhập Firebase thất bại. Chi tiết lỗi: {result}", "Lỗi",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi đăng nhập Firebase: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private void ForgotPassword_Click(object sender, MouseButtonEventArgs e)
        {
            fResetPassword fResetPassword = new fResetPassword();
            this.Close();
            fResetPassword.Show();
        }

        private void Register_Click(object sender, MouseButtonEventArgs e)
        {



            LoginlSignUp.fSignup register = new fSignup();
            register.Show();
            this.Close();
        }

        private void txtUsername_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
