using CommunityToolkit.Mvvm.ComponentModel;
using WpfApp1.Models; // Namespace chứa Group và IChatContact

namespace WpfApp1.ViewModels
{
    public partial class GroupViewModel : ObservableObject, IChatContact
    {
        private readonly Group _groupModel; // Giữ tham chiếu đến Group Model

        public string Id => _groupModel.Id;
        public string Name => _groupModel.Name;
        public string AvatarUrl => _groupModel.AvatarUrl;
        public int MemberCount => _groupModel.MemberCount;

        public GroupViewModel(Group groupModel)
        {
            _groupModel = groupModel ?? throw new ArgumentNullException(nameof(groupModel));
        }

        string IChatContact.Email { get => Id; set { } }
        string IChatContact.Name { get => Name; set { } }
        string IChatContact.AvatarUrl { get => AvatarUrl; set { } }

        public Group GetModel() => _groupModel;

        public override string ToString()
        {
            return $"GroupVM: {Name} ({Id})";
        }
    }
}