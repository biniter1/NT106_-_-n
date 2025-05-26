using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Google.Cloud.Firestore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Xml.Linq;
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


        // Original values to check for changes
        private string _originalName;
        private string _originalEmail;
        private string _originalPhone;
        private DateTime _originalDateOfBirth;

        // Events
        public event Action<bool> ProfileUpdated;

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

            // Notify properties changed
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Email));
            OnPropertyChanged(nameof(Phone));
            OnPropertyChanged(nameof(DateTime));
        }

        [RelayCommand]
        private void SaveProfile()
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

                SaveToDatabase(userData);

                MessageBox.Show("Profile updated successfully!", "Success",
                               MessageBoxButton.OK, MessageBoxImage.Information);

                // Update original values
                UpdateOriginalValues();

                // Notify parent that profile was updated
                ProfileUpdated?.Invoke(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving profile: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void SaveToDatabase(User user)
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
                    DateTime = user.DateTime
                };

                // Update or create the document in Firestore
                await docRef.SetAsync(userData, SetOptions.MergeAll);

                // Note: We don't need to fetch the snapshot again since we're updating directly
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
                   DateOfBirth != _originalDateOfBirth;
        }

        private void UpdateOriginalValues()
        {
            _originalName = Name;
            _originalEmail = Email;
            _originalPhone = Phone;
            _originalDateOfBirth = DateOfBirth;
        }
    }
}