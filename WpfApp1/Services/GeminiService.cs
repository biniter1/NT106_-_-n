using Google.Cloud.AIPlatform.V1;
using Google.Protobuf.WellKnownTypes;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace WpfApp1.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _projectId = "920846316053"; // THAY THẾ bằng Project ID của bạn
        private readonly string _location = "us-central1"; // THAY THẾ bằng khu vực của bạn
        private readonly string _apiKey = "AIzaSyAi21xB8WEIcd4UR71wk6Rt4-3b4eg5n_0"; // THAY THẾ bằng API key của bạn

        public GeminiService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<string> GetResponseAsync(string prompt)
        {
            try
            {
                // Sử dụng REST API trực tiếp thay vì gRPC
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent?key={_apiKey}";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    }
                };

                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseObj = JsonConvert.DeserializeObject<GeminiResponse>(responseContent);

                    if (responseObj?.Candidates?.Length > 0 &&
                        responseObj.Candidates[0]?.Content?.Parts?.Length > 0)
                    {
                        return responseObj.Candidates[0].Content.Parts[0].Text ?? "Không có phản hồi từ API.";
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return $"Lỗi API: {response.StatusCode} - {errorContent}";
                }

                return "Xin lỗi, tôi không thể xử lý yêu cầu này.";
            }
            catch (System.Exception ex)
            {
                return $"Đã có lỗi xảy ra: {ex.Message}";
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    // Classes để deserialize response
    public class GeminiResponse
    {
        [JsonProperty("candidates")]
        public GeminiCandidate[] Candidates { get; set; }
    }

    public class GeminiCandidate
    {
        [JsonProperty("content")]
        public GeminiContent Content { get; set; }
    }

    public class GeminiContent
    {
        [JsonProperty("parts")]
        public GeminiPart[] Parts { get; set; }
    }

    public class GeminiPart
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }
}