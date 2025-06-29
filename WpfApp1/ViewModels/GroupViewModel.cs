using System;
using CommunityToolkit.Mvvm.ComponentModel;
using WpfApp1.Models;
using System.Collections.Generic;

namespace WpfApp1.ViewModels
{
    public partial class GroupViewModel : ObservableObject, IChatContact
    {
        private readonly Group _groupModel; // Giữ tham chiếu đến Group Model

        public string Id => _groupModel.Id;
        public string Name => _groupModel.Name;
        public string AvatarUrl => _groupModel.AvatarUrl;
        public int MemberCount => _groupModel.MemberCount;
        public List<string> MemberEmails => _groupModel.MemberEmails;
        public string CreatedBy => _groupModel.CreatedBy;
        public DateTime CreatedAt => _groupModel.CreatedAt;
        public string Description => _groupModel.Description;
        public List<string> AdminEmails => _groupModel.AdminEmails;
        public string GroupChatId => _groupModel.GroupChatId;

        public GroupViewModel(Group groupModel)
        {
            _groupModel = groupModel ?? throw new ArgumentNullException(nameof(groupModel));
        }

        // IChatContact implementation - using GroupChatId as Email for chat functionality
        string IChatContact.Email { get => GroupChatId ?? Id; set { } }
        string IChatContact.Name { get => Name; set { } }
        string IChatContact.AvatarUrl { get => AvatarUrl; set { } }

        public Group GetModel() => _groupModel;

        public bool IsUserAdmin(string userEmail)
        {
            return AdminEmails?.Contains(userEmail) ?? false;
        }

        public bool IsUserMember(string userEmail)
        {
            return MemberEmails?.Contains(userEmail) ?? false;
        }

        public override string ToString()
        {
            return $"GroupVM: {Name} ({Id}) - {MemberCount} members";
        }
    }
}