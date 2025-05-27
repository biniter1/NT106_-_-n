using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Net.Mail;
using System.Windows;
using System.Xml.Linq;
using WpfApp1.Models;

namespace WpfApp1.ViewModels
{
    public partial class FeedbackViewModel : ObservableObject
    {
        public event Action? CloseRequested;
        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _email;

        [ObservableProperty]
        private string _feedbackMessage;

        public FeedbackViewModel()
        {
            // Prefill name and email from SharedData
            Name = SharedData.Instance.userdata.Name;
            Email = SharedData.Instance.userdata.Email;
            FeedbackMessage = string.Empty;
        }

        [RelayCommand]
        private void SendFeedback()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(FeedbackMessage))
                {
                    MessageBox.Show(GetLocalizedString("FeedbackEmptyMessage"),
                                  GetLocalizedString("Error"),
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                // Configure SMTP client (replace with your SMTP settings)
                using (var client = new SmtpClient("smtp.gmail.com", 587))
                {
                    client.EnableSsl = true;
                    client.Credentials = new System.Net.NetworkCredential("hasonbin123@gmail.com", "nlqr guin fttw wney");

                    // Create mail message
                    var mail = new MailMessage
                    {
                        From = new MailAddress(Email, Name),
                        Subject = GetLocalizedString("FeedbackEmailSubject"),
                        Body = $"Feedback from {Name} ({Email}):\n\n{FeedbackMessage}",
                        IsBodyHtml = false
                    };
                    mail.To.Add("23520149@gm.uit.edu.vn"); // Replace with your email address

                    // Send email
                    client.Send(mail);

                    MessageBox.Show(GetLocalizedString("FeedbackSentMessage"),
                                  GetLocalizedString("Success"),
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);

                    // Close the window
                    CloseWindow();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{GetLocalizedString("FeedbackErrorMessage")}: {ex.Message}",
                              GetLocalizedString("Error"),
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            CloseWindow();
        }

        private void CloseWindow()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    window.Close();
                    break;
                }
            }
        }

        private string GetLocalizedString(string key)
        {
            if (Application.Current.Resources["LocalizationDictionary"] is ResourceDictionary localizationDict)
            {
                return localizationDict.Contains(key) ? localizationDict[key].ToString() : key;
            }
            return key;
        }
    }
}