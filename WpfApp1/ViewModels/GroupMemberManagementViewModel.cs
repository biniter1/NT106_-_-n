using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WpfApp1.Models;
using WpfApp1.Views;
using Google.Cloud.Firestore;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Data;

namespace WpfApp1.ViewModels
{
    public partial class GroupMemberManagementViewModel : ObservableObject
    {
        private readonly Group _group;
        private readonly string _currentUserEmail;
        private readonly FriendListViewModel _friendListViewModel;

        [ObservableProperty]
        private string _groupName = string.Empty;

        [ObservableProperty]
        private string _groupAvatarUrl = string.Empty;

        [ObservableProperty]
        private int _memberCount;

        [ObservableProperty]
        private string _searchMemberText = string.Empty;

        [ObservableProperty]
        private string _searchFriendText = string.Empty;

        public ObservableCollection<GroupMember> AllMembers { get; } = new();
        public ObservableCollection<SelectableFriendForGroup> AllFriends { get; } = new();

        public ICollectionView Members { get; private set; }
        public ICollectionView AvailableFriends { get; private set; }

        public GroupMemberManagementViewModel(Group group, FriendListViewModel friendListViewModel)
        {
            _group = group;
            _currentUserEmail = SharedData.Instance.userdata.Email;
            _friendListViewModel = friendListViewModel;

            GroupName = group.Name;
            GroupAvatarUrl = group.AvatarUrl;
            MemberCount = group.MemberCount;

            // Setup collection views
            Members = CollectionViewSource.GetDefaultView(AllMembers);
            Members.Filter = FilterMembers;

            AvailableFriends = CollectionViewSource.GetDefaultView(AllFriends);
            AvailableFriends.Filter = FilterFriends;

            // Load data
            _ = LoadMembersAsync();
            _ = LoadAvailableFriendsAsync();
        }

        partial void OnSearchMemberTextChanged(string value)
        {
            Members.Refresh();
        }

        partial void OnSearchFriendTextChanged(string value)
        {
            AvailableFriends.Refresh();
        }

        private bool FilterMembers(object obj)
        {
            if (string.IsNullOrWhiteSpace(SearchMemberText))
                return true;

            if (obj is GroupMember member)
            {
                return member.Name.Contains(SearchMemberText, StringComparison.OrdinalIgnoreCase) ||
                       member.Email.Contains(SearchMemberText, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private bool FilterFriends(object obj)
        {
            if (string.IsNullOrWhiteSpace(SearchFriendText))
                return true;

            if (obj is SelectableFriendForGroup friend)
            {
                return friend.Name.Contains(SearchFriendText, StringComparison.OrdinalIgnoreCase) ||
                       friend.Email.Contains(SearchFriendText, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private async Task LoadMembersAsync()
        {
            try
            {
                var db = FirestoreHelper.database;
                bool isCurrentUserAdmin = _group.AdminEmails.Contains(_currentUserEmail);
                bool isCurrentUserOwner = _group.CreatedBy == _currentUserEmail;

                AllMembers.Clear();

                foreach (var memberEmail in _group.MemberEmails)
                {
                    try
                    {
                        // Get user info from Firestore
                        var userDoc = await db.Collection("users").Document(memberEmail).GetSnapshotAsync();
                        if (userDoc.Exists)
                        {
                            var userData = userDoc.ConvertTo<User>();
                            var isAdmin = _group.AdminEmails.Contains(memberEmail);
                            var isOwner = _group.CreatedBy == memberEmail;

                            var member = new GroupMember
                            {
                                Email = memberEmail,
                                Name = userData.Name ?? memberEmail,
                                AvatarUrl = userData.AvatarUrl ?? "/Assets/DefaultAvatar.png",
                                Role = isOwner ? "Owner" : (isAdmin ? "Admin" : "Member"),
                                IsOnline = false, // Will be updated by presence system
                            };

                            // Set permissions based on current user's role
                            if (isCurrentUserOwner)
                            {
                                // Owner can kick anyone except themselves and make anyone admin
                                member.CanKick = memberEmail != _currentUserEmail;
                                member.CanMakeAdmin = !isAdmin && memberEmail != _currentUserEmail;
                            }
                            else if (isCurrentUserAdmin)
                            {
                                // Admin can kick non-admin members and make members admin
                                member.CanKick = !isAdmin && !isOwner && memberEmail != _currentUserEmail;
                                member.CanMakeAdmin = !isAdmin && !isOwner;
                            }
                            else
                            {
                                // Regular members can't kick or promote anyone
                                member.CanKick = false;
                                member.CanMakeAdmin = false;
                            }

                            AllMembers.Add(member);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error loading member {memberEmail}: {ex.Message}");
                    }
                }

                Debug.WriteLine($"Loaded {AllMembers.Count} members for group {_group.Name}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading members: {ex.Message}");
                CustomMessageBox.Show($"Lỗi khi tải danh sách thành viên: {ex.Message}", "Lỗi",
                    CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
            }
        }

        private async Task LoadAvailableFriendsAsync()
        {
            try
            {
                var friends = await _friendListViewModel.LoadFriendsAsync(_currentUserEmail);

                AllFriends.Clear();

                foreach (var friend in friends)
                {
                    // Only show friends who are not already members
                    if (!_group.MemberEmails.Contains(friend.Email))
                    {
                        AllFriends.Add(new SelectableFriendForGroup
                        {
                            Email = friend.Email,
                            Name = friend.Name,
                            AvatarUrl = friend.AvatarUrl,
                            IsSelected = false
                        });
                    }
                }

                Debug.WriteLine($"Loaded {AllFriends.Count} available friends to add");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading available friends: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task KickMember(GroupMember member)
        {
            if (member == null) return;

            var result = CustomMessageBox.Show(
                $"Bạn có chắc chắn muốn kick '{member.Name}' khỏi nhóm?",
                "Xác nhận kick thành viên",
                CustomMessageBoxWindow.MessageButtons.YesNo,
                CustomMessageBoxWindow.MessageIcon.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _friendListViewModel.RemoveUserFromGroup(_group, member.Email);

                    // Remove group contact from kicked user's contacts
                    var db = FirestoreHelper.database;
                    await db.Collection("users").Document(member.Email)
                           .Collection("contacts").Document(_group.GroupChatId).DeleteAsync();

                    // Update local data
                    _group.MemberEmails.Remove(member.Email);
                    _group.AdminEmails.Remove(member.Email);
                    _group.MemberCount = _group.MemberEmails.Count;

                    AllMembers.Remove(member);
                    MemberCount = _group.MemberCount;

                    // Refresh available friends list
                    await LoadAvailableFriendsAsync();

                    CustomMessageBox.Show($"Đã kick '{member.Name}' khỏi nhóm.", "Thành công",
                        CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Information);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error kicking member: {ex.Message}");
                    CustomMessageBox.Show($"Lỗi khi kick thành viên: {ex.Message}", "Lỗi",
                        CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
                }
            }
        }

        [RelayCommand]
        private async Task MakeAdmin(GroupMember member)
        {
            if (member == null) return;

            var result = CustomMessageBox.Show(
                $"Bạn có chắc chắn muốn làm '{member.Name}' thành admin?",
                "Xác nhận thăng admin",
                CustomMessageBoxWindow.MessageButtons.YesNo,
                CustomMessageBoxWindow.MessageIcon.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var db = FirestoreHelper.database;
                    var groupDocRef = db.Collection("Groups").Document(_group.Id);

                    // Add to admin list
                    await groupDocRef.UpdateAsync("AdminEmails", FieldValue.ArrayUnion(member.Email));

                    // Update local data
                    _group.AdminEmails.Add(member.Email);
                    member.Role = "Admin";
                    member.CanMakeAdmin = false;

                    // Update permissions for all members
                    await LoadMembersAsync();

                    CustomMessageBox.Show($"'{member.Name}' đã được thăng thành admin.", "Thành công",
                        CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Information);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error making admin: {ex.Message}");
                    CustomMessageBox.Show($"Lỗi khi thăng admin: {ex.Message}", "Lỗi",
                        CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
                }
            }
        }

        [RelayCommand]
        private async Task AddSelectedMembers()
        {
            var selectedFriends = AllFriends.Where(f => f.IsSelected).ToList();

            if (!selectedFriends.Any())
            {
                CustomMessageBox.Show("Vui lòng chọn ít nhất một bạn bè để thêm vào nhóm.", "Thông báo",
                    CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Information);
                return;
            }

            try
            {
                var selectedEmails = selectedFriends.Select(f => f.Email).ToList();

                // Add members to group
                await _friendListViewModel.AddMembersToGroup(_group, selectedEmails);

                // Add group contact for new members
                var groupContact = new Contact
                {
                    Name = _group.Name,
                    Email = _group.GroupChatId,
                    AvatarUrl = _group.AvatarUrl,
                    chatID = _group.GroupChatId,
                    IsOnline = true
                };

                foreach (var email in selectedEmails)
                {
                    await _friendListViewModel.AddContactAsync(email, groupContact);
                }

                // Update local data
                _group.MemberEmails.AddRange(selectedEmails);
                _group.MemberCount = _group.MemberEmails.Count;
                MemberCount = _group.MemberCount;

                // Refresh lists
                await LoadMembersAsync();
                await LoadAvailableFriendsAsync();

                CustomMessageBox.Show($"Đã thêm {selectedFriends.Count} thành viên vào nhóm!", "Thành công",
                    CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding members: {ex.Message}");
                CustomMessageBox.Show($"Lỗi khi thêm thành viên: {ex.Message}", "Lỗi",
                    CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
            }
        }
    }
}