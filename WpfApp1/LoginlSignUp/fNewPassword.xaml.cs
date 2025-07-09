using System;
using System.Collections.Generic;
using System.Linq;
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
using Google.Cloud.Firestore;
using WpfApp1.Models;
using WpfApp1.Views; 

namespace WpfApp1.LoginlSignUp
{
    /// <summary>
    /// Interaction logic for fNewPassword.xaml
    /// </summary>
    public partial class fNewPassword : Window
    {
        private string email;
        public fNewPassword(string Email)
        {
            InitializeComponent();
            // LocalizationManager.LanguageChanged += OnLanguageChanged; // Giữ nguyên code của bạn
            email = Email;
        }

        // Các phương thức OnLanguageChanged và UpdateBindings giữ nguyên
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

        private async void btnUpdatePassword_Click(object sender, RoutedEventArgs e)
        {
            if (!checkVaild())
            {
                // THAY THẾ 1
                CustomMessageBox.Show("loi", "loi", CustomMessageBoxWindow.MessageButtons.OK);
                return;
            }
            var db = FirestoreHelper.database;
            if (db != null)
            {
                CollectionReference docRef = db.Collection("users");
                var snapShot = await docRef.WhereEqualTo("Email", email).GetSnapshotAsync();
                if (snapShot != null)
                {
                    foreach (var doc in snapShot)
                    {
                        await doc.Reference.UpdateAsync("Password", txtNewPassword.Password);

                        // THAY THẾ 2
                        CustomMessageBox.Show("Thay doi mat khau thanh cong ", "Thong bao", CustomMessageBoxWindow.MessageButtons.OK);

                        LoginlSignUp.fLogin f = new fLogin();
                        this.Hide();
                        f.Show();
                    }
                }
                // THAY THẾ 3
                else CustomMessageBox.Show("loi", "Thông báo", CustomMessageBoxWindow.MessageButtons.OK);
            }
            // THAY THẾ 4
            else CustomMessageBox.Show("Loi", "Thông báo", CustomMessageBoxWindow.MessageButtons.OK);
        }

        // Phương thức checkVaild được giữ nguyên 100%
        private bool checkVaild()
        {
            if (String.IsNullOrEmpty(txtNewPassword.Password)) return false;

            if (String.IsNullOrEmpty(txtConfirmPassword.Password)) return false;

            if (txtNewPassword.Password != txtConfirmPassword.Password) return false;

            return true;
        }
    }
}