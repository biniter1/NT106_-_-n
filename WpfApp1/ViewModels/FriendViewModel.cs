using CommunityToolkit.Mvvm.ComponentModel;
using WpfApp1.Models; // Namespace chứa Contact và IChatContact

namespace WpfApp1.ViewModels
{
    public partial class FriendViewModel : ObservableObject, IChatContact
    {
        private readonly FriendData _FriendModel;

        public string Email => _FriendModel.Email;
        public string Name => _FriendModel.Name;
        public string AvatarUrl => _FriendModel.AvatarUrl;

        [ObservableProperty]
        private bool _isOnline;

        public FriendViewModel(FriendData friendModel)
        {
            _FriendModel = friendModel ?? throw new ArgumentNullException(nameof(friendModel));
            _isOnline = friendModel.IsOnline;
        }

        string IChatContact.Email { get => Email; set { } }
        string IChatContact.Name { get => Name; set { } }
        string IChatContact.AvatarUrl { get => AvatarUrl; set { } }

        public FriendData GetModel() => _FriendModel;

        public override string ToString()
        {
            return $"FriendVM: {Name} ({Email})";
        }
    }
}
