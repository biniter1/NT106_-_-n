using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace WpfApp1.ViewModels
{
    public partial class UserGuideViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _userGuideContent;

        private readonly SettingsViewModel _settingsViewModel;

        public UserGuideViewModel(SettingsViewModel settingsViewModel)
        {
            _settingsViewModel = settingsViewModel;
            // Load user guide content based on the current language
            UserGuideContent = GetLocalizedString("UserGuideContent");
        }

        [RelayCommand]
        private void Close()
        {
            // Return to the previous settings view (e.g., "Settings")
            _settingsViewModel.CurrentView = "Settings";
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