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
            email = Email;
        }
        private async void btnUpdatePassword_Click(object sender, RoutedEventArgs e)
        {
            if (!checkVaild())
            {
                MessageBox.Show("loi", "loi");
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
                        MessageBox.Show("Thay doi mat khau thanh cong ", "Thong bao", MessageBoxButton.OK);
                        LoginlSignUp.fLogin f = new fLogin();
                        this.Hide();
                        f.Show();
                    }

                }
                else MessageBox.Show("loi");
            }
            else MessageBox.Show("Loi");


        }
        private bool checkVaild()
        {
            if (String.IsNullOrEmpty(txtNewPassword.Password)) return false;

            if (String.IsNullOrEmpty(txtConfirmPassword.Password)) return false;

            if (txtNewPassword.Password != txtConfirmPassword.Password) return false;

            return true;
        }
    }
}
