// Trong thư mục ViewModels
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WpfApp1.Models; // Sử dụng lại model Message
using WpfApp1.Services; // Nơi sẽ chứa AI Service

namespace WpfApp1.ViewModels
{
    public partial class AIChatViewModel : ObservableObject
    {
        public ObservableCollection<Message> Messages { get; } = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
        private string _userInput;

        [ObservableProperty]
        private bool _isAITyping; // Để hiển thị trạng thái AI đang "suy nghĩ"

        private readonly GeminiService _aiService;

        public AIChatViewModel()
        {
            _aiService = new GeminiService();
            // Thêm một tin nhắn chào mừng từ AI
            Messages.Add(new Message
            {
                Content = "Xin chào! Tôi có thể giúp gì cho bạn?",
                IsMine = false,
                Timestamp = DateTime.UtcNow
            });
        }

        [RelayCommand(CanExecute = nameof(CanSendMessage))]
        private async Task SendMessageAsync()
        {
            var userMessageContent = UserInput;
            if (string.IsNullOrWhiteSpace(userMessageContent)) return;

            // Thêm tin nhắn của người dùng vào giao diện
            var userMessage = new Message { Content = userMessageContent, IsMine = true, Timestamp = DateTime.UtcNow };
            Messages.Add(userMessage);
            UserInput = string.Empty; // Xóa ô nhập liệu

            IsAITyping = true; // Bật trạng thái chờ

            // Gọi AI Service để lấy câu trả lời
            var aiResponseContent = await _aiService.GetResponseAsync(userMessageContent);

            // Thêm tin nhắn của AI vào giao diện
            var aiMessage = new Message { Content = aiResponseContent, IsMine = false, Timestamp = DateTime.UtcNow };
            Messages.Add(aiMessage);

            IsAITyping = false; // Tắt trạng thái chờ
        }

        private bool CanSendMessage() => !string.IsNullOrWhiteSpace(UserInput) && !IsAITyping;
    }
}