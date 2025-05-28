using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Google.Cloud.Firestore;
using Microsoft.Win32;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Firebase.Storage;
using WpfApp1.Models;

namespace WpfApp1.ViewModels
{
    public partial class EditProfileViewModel : ObservableValidator
    {
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Name is required")]
        [MinLength(2, ErrorMessage = "Name must be at least 2 characters")]
        private string _name;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        private string _email;

        [ObservableProperty]
        private string _phone;

        [ObservableProperty]
        private DateTime _dateOfBirth;

        [ObservableProperty]
        private string _avatarUrl;

        [ObservableProperty]
        private string _avatarFilePath;

        // Original values to check for changes
        private string _originalName;
        private string _originalEmail;
        private string _originalPhone;
        private DateTime _originalDateOfBirth;
        private string _originalAvatarUrl;

        // Events
        public event Action<bool> ProfileUpdated;

        // Static event để notify toàn bộ app khi avatar được update
        public static event Action<string> AvatarUpdated;

        public EditProfileViewModel()
        {
            LoadCurrentProfile();
        }

        private void LoadCurrentProfile()
        {
            // Load current user data
            var userData = SharedData.Instance.userdata;

            _name = _originalName = userData.Name ?? "";
            _email = _originalEmail = userData.Email ?? "";
            _phone = _originalPhone = userData.Phone ?? "";
            _dateOfBirth = _originalDateOfBirth = userData.DateTime != default ? userData.DateTime : DateTime.Now.AddYears(-25);
            _avatarUrl = _originalAvatarUrl = userData.AvatarUrl ?? "";
            _avatarFilePath = "";

            // Notify properties changed
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Email));
            OnPropertyChanged(nameof(Phone));
            OnPropertyChanged(nameof(DateOfBirth));
            OnPropertyChanged(nameof(AvatarUrl));
        }

        [RelayCommand]
        private void ChangeAvatar()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All files (*.*)|*.*",
                Title = "Select Profile Avatar"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    AvatarFilePath = openFileDialog.FileName;
                    AvatarUrl = openFileDialog.FileName; // Display local image immediately
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error selecting avatar: {ex.Message}", "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private async void SaveProfile()
        {
            try
            {
                // Validate all properties
                ValidateAllProperties();

                if (HasErrors)
                {
                    MessageBox.Show("Please fix all validation errors before saving.", "Validation Error",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Check if any changes were made
                if (!HasChanges())
                {
                    MessageBox.Show("No changes detected.", "Information",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Update SharedData
                var userData = SharedData.Instance.userdata;
                userData.Name = Name;
                userData.Email = Email;
                userData.Phone = Phone;
                userData.DateTime = DateOfBirth;

                string newAvatarUrl = userData.AvatarUrl;
                bool avatarChanged = false;

                // Handle avatar upload if a new file was selected
                if (!string.IsNullOrEmpty(AvatarFilePath) && File.Exists(AvatarFilePath))
                {
                    string downloadUrl = await UploadAvatarToFirebase(AvatarFilePath, userData.Email);
                    userData.AvatarUrl = downloadUrl;
                    AvatarUrl = downloadUrl; // Update displayed URL
                    newAvatarUrl = downloadUrl;
                    avatarChanged = true;
                }

                await SaveToDatabase(userData);

                MessageBox.Show("Profile updated successfully!", "Success",
                                MessageBoxButton.OK, MessageBoxImage.Information);

                // Update original values
                UpdateOriginalValues();

                // Fire avatar updated event nếu avatar đã thay đổi
                if (avatarChanged)
                {
                    AvatarUpdated?.Invoke(newAvatarUrl);
                }

                // Notify parent that profile was updated
                ProfileUpdated?.Invoke(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving profile: {ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<string> UploadAvatarToFirebase(string filePath, string email)
        {
            try
            {
                // Initialize Firebase Storage
                var storage = new FirebaseStorage("chatapp-177.firebasestorage.app"); // Replace with your Firebase Storage bucket

                // Generate a unique file name (e.g., using email and timestamp)
                string fileName = $"avatars/{email}.jpg";

                // Upload the file
                var uploadTask = storage.Child(fileName).PutAsync(File.OpenRead(filePath));

                // Get the download URL after upload
                string downloadUrl = await uploadTask;

                return downloadUrl;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to upload avatar to Firebase Storage: {ex.Message}", ex);
            }
        }

        private async Task SaveToDatabase(User user)
        {
            try
            {
                var db = FirestoreHelper.database;
                var docRef = db.Collection("users").Document(user.Email);

                // Prepare the data to be saved
                var userData = new
                {
                    Name = user.Name,
                    Email = user.Email,
                    Phone = user.Phone,
                    DateTime = user.DateTime,
                    AvatarUrl = user.AvatarUrl ?? ""
                };

                // Update or create the document in Firestore
                await docRef.SetAsync(userData, SetOptions.MergeAll);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save user data to Firestore: {ex.Message}", ex);
            }
        }

        [RelayCommand]
        private void CancelEdit()
        {
            // Restore original values
            Name = _originalName;
            Email = _originalEmail;
            Phone = _originalPhone;
            DateOfBirth = _originalDateOfBirth;
            AvatarUrl = _originalAvatarUrl;
            AvatarFilePath = "";

            ProfileUpdated?.Invoke(false);
        }

        [RelayCommand]
        private void ResetToDefaults()
        {
            var result = MessageBox.Show("Are you sure you want to reset all fields to their original values?",
                                        "Confirm Reset", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                CancelEdit();
            }
        }

        private bool HasChanges()
        {
            return Name != _originalName ||
                   Email != _originalEmail ||
                   Phone != _originalPhone ||
                   DateOfBirth != _originalDateOfBirth ||
                   AvatarFilePath != "";
        }

        private void UpdateOriginalValues()
        {
            _originalName = Name;
            _originalEmail = Email;
            _originalPhone = Phone;
            _originalDateOfBirth = DateOfBirth;
            _originalAvatarUrl = AvatarUrl;
            AvatarFilePath = "";
        }
    }
}