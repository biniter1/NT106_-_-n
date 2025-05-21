using System.ComponentModel; // Cần cho INotifyPropertyChanged nếu bạn muốn dùng
using System.Runtime.CompilerServices; // Cần cho INotifyPropertyChanged nếu bạn muốn dùng

namespace WpfApp1.Models
{
    // Enum để biểu thị trạng thái mối quan hệ (tùy chọn)
    public enum FriendStatus
    {
        Unknown,
        NotFriend,
        RequestSent,
        RequestReceived,
        Friend
    }

    public class UserSearchResult : INotifyPropertyChanged // Implement INotifyPropertyChanged nếu trạng thái có thể thay đổi sau khi hiển thị
    {
        private string _userId;
        private string _displayName;
        private string _avatarUrl;
        private FriendStatus _status;

        public string UserId
        {
            get => _userId;
            set => SetProperty(ref _userId, value);
        }

        // Thuộc tính này được Binding trong ListBox ItemTemplate
        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        // (Tùy chọn) URL ảnh đại diện
        public string AvatarUrl
        {
            get => _avatarUrl;
            set => SetProperty(ref _avatarUrl, value);
        }

        // (Tùy chọn) Trạng thái quan hệ với người dùng hiện tại
        // ViewModel có thể dùng trạng thái này để quyết định hiển thị nút "Kết bạn", "Hủy yêu cầu", "Đã là bạn"...
        public FriendStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }


        // --- Triển khai INotifyPropertyChanged (chỉ cần nếu dữ liệu có thể thay đổi sau khi binding) ---
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        // --- Kết thúc phần INotifyPropertyChanged ---

        // (Tùy chọn) Override ToString để debug dễ hơn
        public override string ToString()
        {
            return $"{DisplayName} ({UserId}) - {Status}";
        }
    }
}