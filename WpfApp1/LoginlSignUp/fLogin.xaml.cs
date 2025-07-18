﻿using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Newtonsoft.Json;
using WpfApp1.Models;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using static System.Net.WebRequestMethods;
using WpfApp1.Views;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WpfApp1.LoginlSignUp
{
    public partial class fLogin : Window
    {
        private const string CREDENTIAL_KEY = "WpfApp1_RefreshToken";

        public fLogin()
        {
            InitializeComponent();
            LocalizationManager.LanguageChanged += OnLanguageChanged;
            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                    this.DragMove();
            };
            txtUsername.Focus();

            Loaded += async (s, e) =>
            {
                if (!IsNetworkAvailable())
                {
  
                    MessageBoxResult result = CustomMessageBox.Show("Không có kết nối mạng. Vui lòng kiểm tra kết nối và thử lại.", "Thông báo", CustomMessageBoxWindow.MessageButtons.OK);
                    return;
                }

                // Kiểm tra xem có Refresh Token không
                string refreshToken = RetrieveRefreshToken();
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    // Ẩn form đăng nhập ngay lập tức
                    this.Opacity = 0;

                    // Hiển thị form "Đang đăng nhập"
                    LoadingForm loadingForm = new LoadingForm();
                    loadingForm.Show();

                    try
                    {
                        await AutoLoginAsync();
                        loadingForm.CloseWithFadeOut();
                    }
                    catch (Exception ex)
                    {
                        loadingForm.CloseWithFadeOut();
                        this.Opacity = 1; // Hiển thị lại form đăng nhập
                        MessageBoxResult result = CustomMessageBox.Show("Không thể tự động đăng nhập.Vui lòng đăng nhập thủ công.\nChi tiết lỗi: " + ex.Message, "Thông báo", CustomMessageBoxWindow.MessageButtons.OK);

                    }
                }
            };
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
        // Phương thức tĩnh để xử lý đăng xuất
        public void PerformLogout()
        {
            // Xóa refresh token
            ClearRefreshToken();

            // Đóng MainWindow
            foreach (Window window in Application.Current.Windows)
            {
                if (window is MainWindow)
                {
                    window.Close();
                    break;
                }
            }

            // Hiển thị form đăng nhập
            fLogin loginWindow = null;
            foreach (Window window in Application.Current.Windows)
            {
                if (window is fLogin)
                {
                    loginWindow = (fLogin)window;
                    break;
                }
            }
            if (loginWindow == null)
            {
                loginWindow = new fLogin();
            }
            if (loginWindow.Opacity == 0)
            {
                loginWindow.Opacity = 1;
            }
            if (!loginWindow.IsVisible)
            {
                loginWindow.Show();
            }

            // Đặt focus vào txtUsername
            loginWindow.txtUsername.Text = "";
            loginWindow.txtPassword.Password = "";
            loginWindow.txtUsername.Focus();
        }

        private async Task AutoLoginAsync()
        {
            try
            {
                string refreshToken = RetrieveRefreshToken();
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    string idToken = await RefreshIdToken(refreshToken);
                    if (!string.IsNullOrEmpty(idToken))
                    {
                        // Get email from Firebase using idToken
                        string email = await GetEmailFromIdToken(idToken);
                        if (!string.IsNullOrEmpty(email))
                        {
                            this.Hide();
                            SuccessPopup successPopup = new SuccessPopup();
                            successPopup.Closed += (s, args) =>
                            {
                                MainWindow main = new MainWindow(email);
                                main.Show();
                            };
                            successPopup.Show();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
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

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string email = txtUsername.Text.Trim();
            string password = txtPassword.Password.Trim();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                CustomMessageBox.Show("Vui lòng nhập đầy đủ thông tin đăng nhập!", "Thông báo", CustomMessageBoxWindow.MessageButtons.OK);
                return;
            }

            try
            {
                // BƯỚC 1: XÁC THỰC DUY NHẤT VỚI FIREBASE AUTHENTICATION
                // Hàm này sẽ tự động kiểm tra xem email có tồn tại và mật khẩu có đúng không.
                (string idToken, string refreshToken) = await SignInWithFirebase(email, password);

                // Nếu idToken rỗng, nghĩa là đăng nhập thất bại.
                // Hàm SignInWithFirebase đã hiển thị lỗi rồi nên ta chỉ cần thoát.
                if (string.IsNullOrEmpty(idToken))
                {
                    return;
                }

                // ---- ĐÃ XÓA TOÀN BỘ ĐOẠN CODE KIỂM TRA VỚI FIRESTORE Ở ĐÂY ----
                // Lý do: Việc kiểm tra này là thừa và gây ra lỗi không đồng bộ khi đổi mật khẩu.
                // Chỉ cần SignInWithFirebase thành công là đủ để xác nhận người dùng hợp lệ.

                // BƯỚC 2: KIỂM TRA TRẠNG THÁI XÁC MINH EMAIL (GIỮ NGUYÊN)
                using (HttpClient client = new HttpClient())
                {
                    string apiKey = "AIzaSyCDIXwx-Zcv3Qcxt9e_y8eUNiNlnEXFDbw";
                    string url = $"https://identitytoolkit.googleapis.com/v1/accounts:lookup?key={apiKey}";

                    var lookupPayload = new { idToken = idToken };
                    var content = new StringContent(JsonConvert.SerializeObject(lookupPayload), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, content);
                    string result = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        dynamic resJson = JsonConvert.DeserializeObject(result);
                        bool emailVerified = resJson.users[0].emailVerified;

                        if (!emailVerified)
                        {
                            CustomMessageBox.Show("Email của bạn chưa được xác minh. Vui lòng kiểm tra hộp thư!", "Thông báo", CustomMessageBoxWindow.MessageButtons.OK);
                            return;
                        }
                    }
                    else
                    {
                        CustomMessageBox.Show($"Không thể kiểm tra trạng thái email xác thực. Chi tiết lỗi: {result}", "Thông báo", CustomMessageBoxWindow.MessageButtons.OK);
                        return;
                    }
                }

                // BƯỚC 3: LƯU TOKEN VÀ CHUYỂN TRANG (GIỮ NGUYÊN)
                if (chkRemember.IsChecked == true)
                {
                    StoreRefreshToken(refreshToken);
                }
                else
                {
                    ClearRefreshToken();
                }

                this.Hide();
                SuccessPopup successPopup = new SuccessPopup();
                App.UpdateFirebaseClientAuth(idToken);
                successPopup.Closed += (s, args) =>
                {
                    MainWindow main = new MainWindow(email);
                    main.Show();
                };
                successPopup.Show();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Lỗi: " + ex.Message, "Thông báo", CustomMessageBoxWindow.MessageButtons.OK);
            }
        }

        private async Task<(string idToken, string refreshToken)> SignInWithFirebase(string email, string password)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string apiKey = "AIzaSyCDIXwx-Zcv3Qcxt9e_y8eUNiNlnEXFDbw";
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
                        string refreshToken = resJson.refreshToken;
                        return (idToken, refreshToken);
                    }
                    else
                    {
                        MessageBoxResult resultt = CustomMessageBox.Show($"Đăng nhập Firebase thất bại. Chi tiết lỗi: {result}", "Thông báo", CustomMessageBoxWindow.MessageButtons.OK);

                        return (null, null);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxResult resultt = CustomMessageBox.Show($"Lỗi khi đăng nhập Firebase: {ex.Message}", "Thông báo", CustomMessageBoxWindow.MessageButtons.OK);

                return (null, null);
            }
        }

        private async Task<string> RefreshIdToken(string refreshToken)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string apiKey = "AIzaSyCDIXwx-Zcv3Qcxt9e_y8eUNiNlnEXFDbw";
                    string url = $"https://securetoken.googleapis.com/v1/token?key={apiKey}";

                    var refreshPayload = new
                    {
                        grant_type = "refresh_token",
                        refresh_token = refreshToken
                    };

                    var content = new StringContent(JsonConvert.SerializeObject(refreshPayload), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, content);
                    string result = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        dynamic resJson = JsonConvert.DeserializeObject(result);
                        string newRefreshToken = resJson.refresh_token;

                        // Cập nhật refresh token mới vào storage nếu có
                        if (!string.IsNullOrEmpty(newRefreshToken))
                        {
                            StoreRefreshToken(newRefreshToken);
                        }

                        return resJson.id_token;
                    }
                }
            }
            catch (Exception ex)
            {
                ClearRefreshToken();
            }
            return null;
        }

        private async Task<string> GetEmailFromIdToken(string idToken)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string apiKey = "AIzaSyCDIXwx-Zcv3Qcxt9e_y8eUNiNlnEXFDbw";
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
                        return resJson.users[0].email;
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return null;
        }

        private void StoreRefreshToken(string refreshToken)
        {
            try
            {
                // Use Windows Credential Locker or secure storage
                var bytes = Encoding.UTF8.GetBytes(refreshToken);
                var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
                Properties.Settings.Default.RefreshToken = Convert.ToBase64String(protectedBytes);
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
            }
        }

        private string RetrieveRefreshToken()
        {
            try
            {
                string base64 = Properties.Settings.Default.RefreshToken;
                if (string.IsNullOrEmpty(base64))
                    return null;
                var protectedBytes = Convert.FromBase64String(base64);
                var bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (Exception ex)
            {
                ClearRefreshToken();
                return null;
            }
        }

        public void ClearRefreshToken()
        {
            Properties.Settings.Default.RefreshToken = null;
            Properties.Settings.Default.Save();
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

        private bool IsNetworkAvailable()
        {
            try
            {
                return NetworkInterface.GetIsNetworkAvailable();
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private void txtUsername_TextChanged(object sender, TextChangedEventArgs e)
        {
        }
    }
}