using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using Google.Rpc;
using WpfApp1.Views;
namespace WpfApp1.LoginlSignUp
{
    public partial class fCode : Window
    {
        private DispatcherTimer countdownTimer;
        private TimeSpan countdownTime;
        private List<TextBox> otpBoxes;
        private string code;
        private string email;
        public fCode(string Code,string Email)
        {
            InitializeComponent();
            LocalizationManager.LanguageChanged += OnLanguageChanged;
            otpBoxes = new List<TextBox> { otp1, otp2, otp3, otp4, otp5, otp6 };

            Loaded += (s, e) => { otp1.Focus(); };

            code = Code;

            email = Email;
            InitializeCountdown();
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
        private void InitializeCountdown()
        {
            countdownTime = TimeSpan.FromSeconds(90);
            UpdateCountdownDisplay();

            countdownTimer = new DispatcherTimer();
            countdownTimer.Interval = TimeSpan.FromSeconds(1);
            countdownTimer.Tick += CountdownTimer_Tick;
            countdownTimer.Start();
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            countdownTime = countdownTime.Subtract(TimeSpan.FromSeconds(1));

            UpdateCountdownDisplay();
            if (countdownTime.TotalSeconds <= 0)
            {
                countdownTimer.Stop();
                resendLink.IsEnabled = true;
                resendLink.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4F74FF"));

                btnVerify.IsEnabled = false;

                foreach (var box in otpBoxes)
                {
                    box.IsEnabled = false;
                }

                txtError.Text = "Mã OTP đã hết hạn. Vui lòng yêu cầu mã mới.";
                txtError.Visibility = Visibility.Visible;
            }
        }

        private void UpdateCountdownDisplay()
        {
            countdownText.Text = string.Format("{0:00}:{1:00}",
                countdownTime.Minutes, countdownTime.Seconds);
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }


        private void OTP_TextChanged(object sender, TextChangedEventArgs e)
        {
            var currentTextBox = sender as TextBox;

            if (currentTextBox.Text.Length == 1)
            {
                int currentIndex = otpBoxes.IndexOf(currentTextBox);

                if (currentIndex < otpBoxes.Count - 1 && otpBoxes[currentIndex + 1] != null)
                {
                    otpBoxes[currentIndex + 1].Focus();
                }
                CheckCompleteOTP();
            }
        }



        private void CheckCompleteOTP()
        {
            bool isComplete = true;
            foreach (var box in otpBoxes)
            {
                if (box == null || string.IsNullOrEmpty(box.Text))
                {
                    isComplete = false;
                    break;
                }
            }
            btnVerify.IsEnabled = isComplete;
        }


        private void btnVerify_Click(object sender, RoutedEventArgs e)
        {
            string otp = string.Concat(otp1.Text, otp2.Text, otp3.Text, otp4.Text, otp5.Text, otp6.Text);

            if (countdownTime.TotalSeconds <= 0)
            {
                txtError.Text = "Mã OTP đã hết hạn";
                txtError.Visibility = Visibility.Collapsed;
                return;
            }
            if (otp == code)
            {
                MessageBoxResult result= CustomMessageBox.Show("Xác thực OTP thành công!", "Thông báo", CustomMessageBoxWindow.MessageButtons.OK);
                LoginlSignUp.fNewPassword f = new LoginlSignUp.fNewPassword(email);
                this.Hide();
                f.Show();
            }
            else
            {
                txtError.Text = "Mã OTP không chính xác. Vui lòng thử lại.";
                txtError.Visibility = Visibility.Visible;

                foreach (var box in otpBoxes)
                {
                    box.Text = string.Empty;
                }
                otp1.Focus();
            }

        }

        private void resendLink_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = CustomMessageBox.Show("Đã gửi lại mã OTP mới!", "Thông báo", CustomMessageBoxWindow.MessageButtons.OK);

            countdownTime = TimeSpan.FromSeconds(90);
            UpdateCountdownDisplay();

            countdownTimer.Start();

            btnVerify.IsEnabled = true;

            resendLink.IsEnabled = false;
            resendLink.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC"));

            foreach (var box in otpBoxes)
            {
                box.Text = string.Empty;
                box.IsEnabled = true;
            }
            txtError.Visibility = Visibility.Collapsed;
            otp1.Focus();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (e.Key == Key.Back)
            {
                var focusedElement = FocusManager.GetFocusedElement(this) as TextBox;
                if (focusedElement != null && otpBoxes.Contains(focusedElement))
                {
                    int currentIndex = otpBoxes.IndexOf(focusedElement);
                    if (string.IsNullOrEmpty(focusedElement.Text) && currentIndex > 0)
                    {
                        otpBoxes[currentIndex - 1].Focus();
                        otpBoxes[currentIndex - 1].SelectAll();
                    }
                }
            }
        }
    }
}
