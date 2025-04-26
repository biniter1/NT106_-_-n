// File: ViewModels/AddFriendViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WpfApp1.Models; // Namespace chứa UserSearchResult
using System.Windows; // Cho MessageBox (tùy chọn)
// using WpfApp1.Services; // Namespace chứa IFriendService (nếu có)

namespace WpfApp1.ViewModels // <-- Thay WpfApp1 bằng namespace gốc của dự án bạn
{
    public partial class AddFriendViewModel : ObservableObject
    {
        // (Tùy chọn) Service để xử lý logic tìm kiếm và kết bạn
        // private readonly IFriendService _friendService;

        // Thuộc tính cho ô tìm kiếm, tự động tạo bởi [ObservableProperty]
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SearchCommand))] // Tự động gọi CanExecute của SearchCommand khi SearchQuery thay đổi
        private string _searchQuery = ""; // Khởi tạo giá trị mặc định

        // Collection chứa kết quả, không cần thay đổi
        public ObservableCollection<UserSearchResult> SearchResults { get; } = new();

        // Lệnh Tìm kiếm - Sử dụng attribute [RelayCommand]
        // Tên phương thức sẽ là tên lệnh + "Async" (nếu là Task) -> SearchCommand
        // Thuộc tính IsRunning sẽ tự động có trên SearchCommand
        [RelayCommand(CanExecute = nameof(CanSearch))] // Chỉ định phương thức CanExecute
        private async Task SearchAsync() // Đặt tên phương thức là SearchAsync
        {
            SearchResults.Clear();
            Console.WriteLine($"Đang tìm kiếm: {SearchQuery}");

            // *** THAY THẾ BẰNG LOGIC GỌI SERVICE/API THỰC TẾ ***
            try
            {
                await Task.Delay(1000); // Giả lập chờ mạng
                if (!string.IsNullOrWhiteSpace(SearchQuery))
                {
                    SearchResults.Add(new UserSearchResult { UserId = "1", DisplayName = $"{SearchQuery} User 1", Status = FriendStatus.NotFriend });
                    SearchResults.Add(new UserSearchResult { UserId = "2", DisplayName = $"{SearchQuery} User 2", Status = FriendStatus.RequestSent });
                    SearchResults.Add(new UserSearchResult { UserId = "3", DisplayName = $"{SearchQuery} User 3", Status = FriendStatus.Friend });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi tìm kiếm: {ex.Message}");
                // Cân nhắc dùng service hiển thị lỗi thay vì MessageBox trực tiếp
                MessageBox.Show($"Đã xảy ra lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                 // Gọi lại CanExecute của AddFriendCommand để cập nhật trạng thái nút Kết bạn
                 AddFriendCommand.NotifyCanExecuteChanged();
            }
        }

        // Phương thức kiểm tra điều kiện thực thi cho SearchAsync
        private bool CanSearch()
        {
            // Nếu lệnh chưa chạy và có nội dung tìm kiếm
            return !SearchCommand.IsRunning && !string.IsNullOrWhiteSpace(SearchQuery);
        }


        // Lệnh Thêm bạn - Sử dụng attribute [RelayCommand]
        // Tên phương thức sẽ là tên lệnh + "Async" (nếu là Task) -> AddFriendCommand
        // Tự động nhận tham số kiểu UserSearchResult từ CommandParameter
        [RelayCommand(CanExecute = nameof(CanAddFriend))] // Chỉ định phương thức CanExecute
        private async Task AddFriendAsync(UserSearchResult user) // Đặt tên phương thức AddFriendAsync
        {
            if (user == null) return; // Tham số null thì bỏ qua

            Console.WriteLine($"Đang gửi yêu cầu kết bạn đến: {user.DisplayName} ({user.UserId})");

            // *** THAY THẾ BẰNG LOGIC GỌI SERVICE/API GỬI YÊU CẦU KẾT BẠN THỰC TẾ ***
            try
            {
                await Task.Delay(800); // Giả lập chờ mạng
                user.Status = FriendStatus.RequestSent; // Cập nhật trạng thái (cần INotifyPropertyChanged trong UserSearchResult)
                Console.WriteLine($"Đã gửi yêu cầu đến {user.DisplayName}");

                // Quan trọng: Thông báo cho UI biết rằng điều kiện CanExecute của lệnh này
                // (cho user này và có thể cả user khác) cần được đánh giá lại.
                AddFriendCommand.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi gửi yêu cầu kết bạn: {ex.Message}");
                MessageBox.Show($"Đã xảy ra lỗi khi gửi yêu cầu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Phương thức kiểm tra điều kiện thực thi cho AddFriendAsync
        private bool CanAddFriend(UserSearchResult user)
        {
            // Cho phép nếu user hợp lệ, chưa phải bạn bè/đã gửi yêu cầu, và lệnh không đang chạy
            return user != null
                && user.Status == FriendStatus.NotFriend
                && !AddFriendCommand.IsRunning; // Kiểm tra trạng thái chạy chung của lệnh
                // Lưu ý: Nếu cần vô hiệu hóa chỉ nút của user đang được xử lý, bạn cần thêm cờ trạng thái trong UserSearchResult
        }

    }
}