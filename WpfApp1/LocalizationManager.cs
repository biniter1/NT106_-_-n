using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;

namespace WpfApp1
{
    public static class LocalizationManager
    {
        // Event để thông báo khi ngôn ngữ thay đổi
        public static event EventHandler LanguageChanged;

        // Thuộc tính để lưu ngôn ngữ hiện tại
        public static string CurrentLanguage { get; private set; } = "Vietnamese";

        // Flag để tránh vòng lặp đệ quy
        private static bool _isChangingLanguage = false;

        /// <summary>
        /// Thay đổi ngôn ngữ của ứng dụng
        /// </summary>
        /// <param name="language">Ngôn ngữ cần chuyển đổi ("Vietnamese", "English", etc.)</param>
        public static void ChangeLanguage(string language)
        {
            if (_isChangingLanguage || CurrentLanguage == language)
                return;

            try
            {
                _isChangingLanguage = true;
                CurrentLanguage = language;

                // Thay đổi Culture
                var cultureInfo = language switch
                {
                    "Vietnamese" => new CultureInfo("vi-VN"),
                    "English" => new CultureInfo("en-US"),
                    _ => new CultureInfo("vi-VN")
                };

                Thread.CurrentThread.CurrentCulture = cultureInfo;
                Thread.CurrentThread.CurrentUICulture = cultureInfo;

                // Load ResourceDictionary tương ứng
                LoadResourceDictionary(language);

                // Gửi event
                LanguageChanged?.Invoke(null, EventArgs.Empty);

                System.Diagnostics.Debug.WriteLine($"Language changed to: {language}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ChangeLanguage: {ex.Message}");
                MessageBox.Show($"Lỗi khi thay đổi ngôn ngữ: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isChangingLanguage = false;
            }
        }

        private static void LoadResourceDictionary(string language)
        {
            try
            {
                var app = Application.Current;
                if (app == null)
                {
                    System.Diagnostics.Debug.WriteLine("Application.Current is null");
                    return;
                }

                string resourcePath = language switch
                {
                    "Vietnamese" => "/Resources/StringResources.vi.xaml",
                    "English" => "/Resources/StringResources.en.xaml",
                    _ => "/Resources/StringResources.vi.xaml"
                };

                System.Diagnostics.Debug.WriteLine($"Trying to load resource: {resourcePath}");

                // Xoá dictionary ngôn ngữ cũ trước
                var oldDicts = app.Resources.MergedDictionaries
                    .Where(d => d.Source != null &&
                               (d.Source.OriginalString.Contains("StringResources.vi.xaml") ||
                                d.Source.OriginalString.Contains("StringResources.en.xaml")))
                    .ToList();

                foreach (var oldDict in oldDicts)
                {
                    app.Resources.MergedDictionaries.Remove(oldDict);
                    System.Diagnostics.Debug.WriteLine($"Removed old dictionary: {oldDict.Source}");
                }

                // Tạo ResourceDictionary mới
                var dict = new ResourceDictionary();

                // Thử load với pack URI
                try
                {
                    var packUri = new Uri($"pack://application:,,,{resourcePath}", UriKind.Absolute);
                    dict.Source = packUri;
                    System.Diagnostics.Debug.WriteLine($"Loading with pack URI: {packUri}");
                }
                catch (Exception packEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Pack URI failed: {packEx.Message}");
                    // Fallback với relative URI
                    var relativeUri = new Uri(resourcePath.TrimStart('/'), UriKind.Relative);
                    dict.Source = relativeUri;
                    System.Diagnostics.Debug.WriteLine($"Loading with relative URI: {relativeUri}");
                }

                // Force load dictionary để kiểm tra lỗi
                var testLoad = dict.Keys.Count;

                // Thêm dictionary mới
                app.Resources.MergedDictionaries.Add(dict);
                System.Diagnostics.Debug.WriteLine($"Successfully added dictionary: {resourcePath} with {dict.Keys.Count} keys");
            }
            catch (System.IO.FileNotFoundException fileEx)
            {
                System.Diagnostics.Debug.WriteLine($"Resource file not found: {fileEx.Message}");
                System.Diagnostics.Debug.WriteLine("Please ensure the resource files exist in the Resources folder and are set as 'Resource' build action");
                throw new Exception($"Resource file not found: {language}. Please check if StringResources.{(language == "Vietnamese" ? "vi" : "en")}.xaml exists in Resources folder.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading resource dictionary: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Khởi tạo ngôn ngữ từ cài đặt đã lưu
        /// </summary>
        public static void Initialize()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Initializing LocalizationManager...");

                // Đảm bảo Application.Current đã khởi tạo
                if (Application.Current == null)
                {
                    System.Diagnostics.Debug.WriteLine("Application.Current is null during initialization");
                    return;
                }

                // Load default language first
                LoadResourceDictionary("Vietnamese");

                // Sau đó load saved language nếu có
                string savedLanguage = null;
                try
                {
                    savedLanguage = Properties.Settings.Default.SelectedLanguage;
                    System.Diagnostics.Debug.WriteLine($"Saved language from settings: {savedLanguage}");
                }
                catch (Exception settingsEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error reading settings: {settingsEx.Message}");
                }

                if (!string.IsNullOrEmpty(savedLanguage))
                {
                    var language = savedLanguage switch
                    {
                        "Tiếng Việt" => "Vietnamese",
                        "English" => "English",
                        _ => "Vietnamese"
                    };
                    if (language != "Vietnamese") // Chỉ thay đổi nếu khác default
                    {
                        ChangeLanguage(language);
                    }
                }

                System.Diagnostics.Debug.WriteLine("LocalizationManager initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing language: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Lỗi khởi tạo ngôn ngữ: {ex.Message}\n\nVui lòng kiểm tra:\n1. File StringResources.vi.xaml có tồn tại trong thư mục Resources không\n2. Build Action của file có được set là 'Resource' không", "Lỗi khởi tạo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Lấy chuỗi đã localize
        /// </summary>
        public static string GetString(string key)
        {
            try
            {
                var app = Application.Current;
                if (app?.Resources[key] != null)
                {
                    return app.Resources[key].ToString();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Key '{key}' not found in resources");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting localized string for key '{key}': {ex.Message}");
            }

            return key; // Fallback to key if not found
        }

        /// <summary>
        /// Kiểm tra xem resource dictionary có được load không
        /// </summary>
        public static void CheckResourceDictionaries()
        {
            var app = Application.Current;
            if (app != null)
            {
                System.Diagnostics.Debug.WriteLine($"Total merged dictionaries: {app.Resources.MergedDictionaries.Count}");
                foreach (var dict in app.Resources.MergedDictionaries)
                {
                    System.Diagnostics.Debug.WriteLine($"Dictionary source: {dict.Source}");
                    System.Diagnostics.Debug.WriteLine($"Dictionary keys count: {dict.Keys.Count}");

                    // Debug một vài key đầu tiên
                    var keys = dict.Keys.Cast<object>().Take(5);
                    foreach (var key in keys)
                    {
                        System.Diagnostics.Debug.WriteLine($"  Key: {key} = {dict[key]}");
                    }
                }
            }
        }
    }
}