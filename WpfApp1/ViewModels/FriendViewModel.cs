using CommunityToolkit.Mvvm.ComponentModel;
using WpfApp1.Models; // Namespace chứa Contact và IChatContact

namespace WpfApp1.ViewModels
{
    // FriendViewModel wrap Contact Model và implement IChatContact
    public partial class FriendViewModel : ObservableObject, IChatContact
    {
        private readonly Contact _contactModel; // Giữ tham chiếu đến Contact Model

        // Expose các thuộc tính từ Contact Model
        public string Id => _contactModel.Id;
        public string Name => _contactModel.Name;
        public string AvatarUrl => _contactModel.AvatarUrl;

        // Expose IsOnline và làm nó Observable nếu cần cập nhật động từ VM
        [ObservableProperty]
        private bool _isOnline;

        // Constructor nhận Contact Model
        public FriendViewModel(Contact contactModel)
        {
            _contactModel = contactModel ?? throw new ArgumentNullException(nameof(contactModel));
            _isOnline = contactModel.IsOnline; // Khởi tạo giá trị ban đầu
            // TODO: Cập nhật IsOnline nếu trạng thái thay đổi qua SignalR/Polling,...
        }

        // Triển khai IChatContact properties (nếu cần set thì phải thêm logic)
        string IChatContact.Id { get => Id; set { /* No set needed from interface typically */ } }
        string IChatContact.Name { get => Name; set { } }
        string IChatContact.AvatarUrl { get => AvatarUrl; set { } }

        // Phương thức để lấy Model gốc nếu cần trong Command
        public Contact GetModel() => _contactModel;

        public override string ToString()
        {
            return $"FriendVM: {Name} ({Id})";
        }
    }
}