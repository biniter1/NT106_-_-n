using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WpfApp1.Models; // Namespace của Contact, Group, FriendRequest, IChatContact
using System.Collections.Generic;

namespace WpfApp1.ViewModels
{
    public partial class FriendListViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(UnfriendCommand))]
        [NotifyCanExecuteChangedFor(nameof(LeaveGroupCommand))]
        [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
        private IChatContact _selectedContact; // Vẫn là IChatContact

        public ObservableCollection<IChatContact> CombinedContacts { get; } // Chứa FriendViewModel và GroupViewModel
        public ObservableCollection<FriendRequestViewModel> FriendRequests { get; }

        public FriendListViewModel()
        {
            CombinedContacts = new ObservableCollection<IChatContact>();
            FriendRequests = new ObservableCollection<FriendRequestViewModel>();
            LoadInitialData();
        }

        // --- Command Methods ---

        [RelayCommand(CanExecute = nameof(CanUnfriend))]
        private void Unfriend(FriendViewModel friendVM) // Tham số là FriendViewModel
        {
            if (friendVM == null) return;
            Contact friendModel = friendVM.GetModel(); // Lấy Contact Model gốc

            MessageBox.Show($"Đang hủy kết bạn với: {friendModel.Name} ({friendModel.Id})");
            // *** Logic hủy kết bạn thật với friendModel.Id ***

            CombinedContacts.Remove(friendVM);
            if (SelectedContact == friendVM) SelectedContact = null;
        }
        private bool CanUnfriend() => SelectedContact is FriendViewModel;


        [RelayCommand(CanExecute = nameof(CanLeaveGroup))]
        private void LeaveGroup(GroupViewModel groupVM) // Tham số là GroupViewModel
        {
            if (groupVM == null) return;
            Group groupModel = groupVM.GetModel(); // Lấy Group Model gốc

            MessageBox.Show($"Đang rời nhóm: {groupModel.Name} ({groupModel.Id})");
            // *** Logic rời nhóm thật với groupModel.Id ***

            CombinedContacts.Remove(groupVM);
            if (SelectedContact == groupVM) SelectedContact = null;
        }
        private bool CanLeaveGroup() => SelectedContact is GroupViewModel;

        public event Action<IChatContact> RequestOpenChat;
        [RelayCommand(CanExecute = nameof(CanSendMessage))]
        private void SendMessage(IChatContact contactVM)
        {
            if (contactVM == null) return;
            MessageBox.Show($"Đang mở cửa sổ chat với: {contactVM.Name} ({contactVM.Id})");
            // *** Logic mở chat thật với contactVM.Id ***
            // Bạn có thể kiểm tra kiểu contactVM (là FriendViewModel hay GroupViewModel) nếu cần
            // if (contactVM is FriendViewModel friendVM) { ... }
            // else if (contactVM is GroupViewModel groupVM) { ... }
        }
        private bool CanSendMessage() => SelectedContact != null;


        [RelayCommand]
        private void AcceptRequest(FriendRequestViewModel requestVM)
        {
            if (requestVM == null) return;
            FriendRequest requestModel = requestVM.GetModel();

            MessageBox.Show($"Đã chấp nhận lời mời từ: {requestModel.RequesterName} ({requestModel.RequesterId})");

            // *** Logic chấp nhận lời mời thật ***
            // 1. Gọi service chấp nhận -> Server trả về thông tin Contact đầy đủ của người bạn mới
            // 2. Tạo Contact Model từ thông tin trả về
            // 3. Tạo FriendViewModel từ Contact Model mới
            // 4. Thêm FriendViewModel vào CombinedContacts
            // 5. Xóa requestVM khỏi FriendRequests

            // --- Code giả lập ---
            var newFriendModel = new Contact { Id = requestModel.RequesterId, Name = requestModel.RequesterName, AvatarUrl = requestModel.RequesterAvatarUrl ?? "", IsOnline = false }; // Dùng Contact Model
            var newFriendVM = new FriendViewModel(newFriendModel); // Tạo FriendViewModel từ Contact
            CombinedContacts.Add(newFriendVM);
            FriendRequests.Remove(requestVM);
            SortContacts();
        }

        [RelayCommand]
        private void DeclineRequest(FriendRequestViewModel requestVM)
        {
            if (requestVM == null) return;
            FriendRequest requestModel = requestVM.GetModel();
            MessageBox.Show($"Đã từ chối lời mời từ: {requestModel.RequesterName} ({requestModel.RequesterId})");
            // *** Logic từ chối thật ***
            FriendRequests.Remove(requestVM);
        }

        // --- Helper Methods ---
        private void LoadInitialData()
        {
            // *** Tải dữ liệu Model: Contact (Friends), Group, FriendRequest ***
            List<Contact> friends = new List<Contact> { // Dùng Contact Model
                new Contact { Id = "friend1", Name = "Nguyễn Văn A (Contact)", AvatarUrl = "", IsOnline=true },
                new Contact { Id = "friend2", Name = "Trần Thị B (Contact)", AvatarUrl = "", IsOnline=false },
                new Contact { Id = "friend3", Name = "Lê Văn C (Contact)", AvatarUrl = "", IsOnline=true }
            };
            // Chưa xử lý được group
            //List<Group> groups = new List<Group> {
            //    new Group { Id = "group1", Name = "Nhóm Học Tập (Group)", AvatarUrl = "", MemberCount=10 },
            //    new Group { Id = "group2", Name = "Team Dự Án X (Group)", AvatarUrl = "", MemberCount=5 }
            //};
            List<FriendRequest> requests = new List<FriendRequest> {
                 new FriendRequest { RequestId = "req1", RequesterId = "user_xyz", RequesterName = "Người Lạ 1", RequesterAvatarUrl = "", RequestTime=DateTime.Now.AddDays(-1) },
                 new FriendRequest { RequestId = "req2", RequesterId = "user_abc", RequesterName = "Người Lạ 2", RequesterAvatarUrl = "", RequestTime=DateTime.Now }
            };

            // *** Chuyển Models thành ViewModels ***
            CombinedContacts.Clear();
            foreach (var friend in friends)
            {
                CombinedContacts.Add(new FriendViewModel(friend)); // Tạo FriendViewModel từ Contact
            }

            // Chưa xử lý group
            //foreach (var group in groups)
            //{
            //    CombinedContacts.Add(new GroupViewModel(group)); // Tạo GroupViewModel từ Group
            //}

            FriendRequests.Clear();
            foreach (var request in requests)
            {
                FriendRequests.Add(new FriendRequestViewModel(request)); // Tạo FriendRequestViewModel từ FriendRequest
            }

            SortContacts();
        }

        private void SortContacts()
        {
            var sortedContacts = CombinedContacts.OrderBy(c => c.Name).ToList();
            CombinedContacts.Clear();
            foreach (var contact in sortedContacts)
            {
                CombinedContacts.Add(contact);
            }
        }
    }
}