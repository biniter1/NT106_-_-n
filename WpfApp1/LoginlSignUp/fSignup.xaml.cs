using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
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
using static System.Net.WebRequestMethods;
using WpfApp1.Models;

namespace WpfApp1.LoginlSignUp
{
    public partial class fSignup : Window
    {
        public fSignup()
        {
            InitializeComponent();
            LocalizationManager.LanguageChanged += OnLanguageChanged;
            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                    this.DragMove();
            };

            cboGender.SelectedIndex = 0;
            dpBirthDate.SelectedDate = System.DateTime.Now.AddYears(-18);
        }
        private void OnLanguageChanged(object sender, EventArgs e)
        {
            // Force the UI to refresh bindings
            InvalidateVisual();
            // Optionally, update specific bindings
            UpdateBindings();
        }
        private void UpdateBindings()
        {
            // Update bindings for controls that use localized strings
            foreach (var element in LogicalTreeHelper.GetChildren(this))
            {
                if (element is FrameworkElement fe)
                {
                    fe.GetBindingExpression(FrameworkElement.DataContextProperty)?.UpdateTarget();
                    // Update other bindings as needed
                }
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DoubleAnimation fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3)
            };

            fadeOut.Completed += (s, _) => this.Close();
            this.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            fLogin loginWindow = new fLogin();

            DoubleAnimation fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3)
            };

            fadeOut.Completed += (s, _) =>
            {
                loginWindow.Show();
                this.Close();
            };

            this.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var data = GetWriteData(); // hàm lấy email + password từ form
            string email = data.Email.Trim();
            string password = data.Password.Trim();
            string apiKey = "AIzaSyCDIXwx-Zcv3Qcxt9e_y8eUNiNlnEXFDbw";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // === BƯỚC 1: Gửi yêu cầu ĐĂNG KÝ tài khoản mới ===
                    var signUpPayload = new
                    {
                        email = email,
                        password = password,
                        returnSecureToken = true
                    };

                    string signUpUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={apiKey}";
                    var signUpContent = new StringContent(JsonConvert.SerializeObject(signUpPayload), Encoding.UTF8, "application/json");
                    HttpResponseMessage signUpResponse = await client.PostAsync(signUpUrl, signUpContent);
                    string signUpResult = await signUpResponse.Content.ReadAsStringAsync();
                    dynamic signUpJson = JsonConvert.DeserializeObject(signUpResult);

                    if (!signUpResponse.IsSuccessStatusCode)
                    {
                        string error = signUpJson.error.message;
                        MessageBox.Show("Lỗi đăng ký: " + error, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    string idToken = signUpJson.idToken;
                    data.IdToken = idToken;
                    // === BƯỚC 2: Gửi email xác thực ===
                    var verifyPayload = new
                    {
                        requestType = "VERIFY_EMAIL",
                        idToken = idToken
                    };

                    string verifyUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={apiKey}";
                    var verifyContent = new StringContent(JsonConvert.SerializeObject(verifyPayload), Encoding.UTF8, "application/json");
                    HttpResponseMessage verifyResponse = await client.PostAsync(verifyUrl, verifyContent);
                    string verifyResult = await verifyResponse.Content.ReadAsStringAsync();
                    dynamic verifyJson = JsonConvert.DeserializeObject(verifyResult);

                    if (verifyResponse.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Email xác thực đã được gửi thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Mở form xác thực tiếp theo nếu muốn
                        fOtp emailForm = new fOtp(idToken, data.Email, data);
                        this.Hide();
                        emailForm.Show();
                    }
                    else
                    {
                        string error = verifyJson.error.message;
                        MessageBox.Show("Lỗi gửi email xác thực: " + error, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Đã xảy ra lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public User GetWriteData()
        {
            string phonenumber = txtPhone.Text;
            string username = txtUsername.Text.Trim();
            return new User
            {
                Ho = txtFirstName.Text,
                Name = txtLastName.Text,
                Email = txtEmail.Text,
                Password = txtPassword.Password,
                Username = username,
                Phone = txtPhone.Text,
                gender = cboGender.SelectedItem.ToString(),
                DateTime = System.DateTime.SpecifyKind(dpBirthDate.SelectedDate.Value, DateTimeKind.Utc),
                Address = txtAddress.Text
            };
        }
        public bool CheckExist(string phone)
        {
            var db = FirestoreHelper.database;

            if (db == null)
                throw new Exception("Firestore database is not initialized.");

            var docRef = db.Collection("users").Document(phone);
            var snapshot = docRef.GetSnapshotAsync().Result;

            if (snapshot.Exists)
            {
                var data = snapshot.ConvertTo<User>();
                return data != null;
            }

            return false;
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(txtFirstName.Text) || string.IsNullOrWhiteSpace(txtLastName.Text))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ họ và tên!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtEmail.Text) || !IsValidEmail(txtEmail.Text))
            {
                MessageBox.Show("Vui lòng nhập địa chỉ email hợp lệ!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtUsername.Text) || txtUsername.Text.Length < 6)
            {
                MessageBox.Show("Tên đăng nhập phải có ít nhất 6 ký tự!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Password) || txtPassword.Password.Length < 6)
            {
                MessageBox.Show("Mật khẩu phải có ít nhất 6 ký tự!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (txtPassword.Password != txtConfirmPassword.Password)
            {
                MessageBox.Show("Xác nhận mật khẩu không khớp!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtPhone.Text) || !IsValidPhoneNumber(txtPhone.Text))
            {
                MessageBox.Show("Vui lòng nhập số điện thoại hợp lệ!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (dpBirthDate.SelectedDate == null || dpBirthDate.SelectedDate > System.DateTime.Now.AddYears(-10))
            {
                MessageBox.Show("Vui lòng chọn ngày sinh hợp lệ (trên 10 tuổi)!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtAddress.Text))
            {
                MessageBox.Show("Vui lòng nhập địa chỉ!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (chkTerms.IsChecked != true)
            {
                MessageBox.Show("Vui lòng đồng ý với điều khoản và điều kiện!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            return Regex.IsMatch(email, pattern);
        }

        private bool IsValidPhoneNumber(string phoneNumber)
        {
            string pattern = @"^(0|\+84)(\d{9,10})$";
            return Regex.IsMatch(phoneNumber, pattern);
        }
        private void cboGender_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
