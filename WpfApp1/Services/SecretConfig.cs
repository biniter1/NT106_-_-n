using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace WpfApp1.Services
{
    public class SecretConfig
    {
        public string ApiKey { get; set; }
    }

    public static class SecretManager
    {
        public static async Task<string> LoadApiKeyAsync()
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, "secret.json");
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Không tìm thấy file key!", filePath);

            var json = await File.ReadAllTextAsync(filePath);
            var config = JsonSerializer.Deserialize<SecretConfig>(json);
            return config.ApiKey;
        }
    }
}
