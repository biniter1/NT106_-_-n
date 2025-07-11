using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WpfApp1.Models;
using System.Collections.Generic;
using System.Windows.Documents;
using Google.Cloud.Firestore;
using System.Text;
using WpfApp1.Views;
using System.Security.Cryptography;
using Firebase.Database;
using System.Windows.Controls;
using System.IO;
using WpfApp1.Services;
using System.Diagnostics;

namespace WpfApp1.ViewModels
{
    // Define GroupData class
    public class GroupData
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }

    public partial class FriendListViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(UnfriendCommand))]
        [NotifyCanExecuteChangedFor(nameof(LeaveGroupCommand))]
        [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
        private IChatContact? _selectedContact; // Made nullable

        public User userdata = SharedData.Instance.userdata;

        private readonly FirebaseClient firebaseClient;

        [ObservableProperty]
        private ObservableObject? _currentViewModel; // Made nullable

        public ObservableCollection<IChatContact> CombinedContacts { get; } // Chứa FriendViewModel và GroupViewModel
        public ObservableCollection<FriendRequestViewModel> FriendRequests { get; }

        public event Action<IChatContact>? RequestOpenChat; // Made nullable

        public FriendListViewModel()
        {
            firebaseClient = new FirebaseClient("https://chatapp-177-default-rtdb.asia-southeast1.firebasedatabase.app/");
            CombinedContacts = new ObservableCollection<IChatContact>();
            FriendRequests = new ObservableCollection<FriendRequestViewModel>();
            LoadInitialData();
        }

        // --- Command Methods ---

        [RelayCommand(CanExecute = nameof(CanUnfriend))]
        private async Task Unfriend(FriendViewModel friendVM) // Changed to Task
        {
            if (friendVM == null) return;
            FriendData friendModel = friendVM.GetModel(); // Lấy Contact Model gốc

            var db = FirestoreHelper.database;

            try
            {
                // --- Xóa bạn bè khỏi tài khoản người dùng hiện tại ---
                var currentUserDocRef = db.Collection("Friend").Document(SharedData.Instance.userdata.Email);
                var currentUserDocSnapshot = await currentUserDocRef.GetSnapshotAsync();

                if (currentUserDocSnapshot.Exists)
                {
                    // Lấy danh sách bạn bè hiện tại
                    var friendsList = currentUserDocSnapshot.GetValue<List<Dictionary<string, object>>>("friends");

                    // Tìm và loại bỏ bạn bè có email trùng với friendEmail
                    friendsList?.RemoveAll(friend => friend["Email"].ToString() == friendModel.Email);

                    // Cập nhật lại danh sách bạn bè cho người dùng hiện tại
                    await currentUserDocRef.UpdateAsync("friends", friendsList);
                }

                // --- Xóa bạn bè khỏi tài khoản của người bạn ---
                var friendDocRef = db.Collection("Friend").Document(friendModel.Email);
                var friendDocSnapshot = await friendDocRef.GetSnapshotAsync();

                if (friendDocSnapshot.Exists)
                {
                    // Lấy danh sách bạn bè của người bạn
                    var friendList = friendDocSnapshot.GetValue<List<Dictionary<string, object>>>("friends");

                    // Tìm và loại bỏ người dùng có email trùng với currentUserEmail
                    friendList?.RemoveAll(friend => friend["Email"].ToString() == SharedData.Instance.userdata.Email);

                    // Cập nhật lại danh sách bạn bè cho người bạn
                    await friendDocRef.UpdateAsync("friends", friendList);
                }

                CombinedContacts.Remove(friendVM);
                if (SelectedContact == friendVM) SelectedContact = null;

                // Cập nhật lại giao diện
                SortContacts();

                CustomMessageBox.Show("Đã hủy kết bạn thành công.", "Thành công",
                                    CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Information);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Có lỗi khi hủy kết bạn: {ex.Message}", "Lỗi",
                                    CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
            }
        }

        private bool CanUnfriend() => SelectedContact is FriendViewModel;

        [RelayCommand(CanExecute = nameof(CanLeaveGroup))]
        private async Task LeaveGroup(GroupViewModel groupVM) // Enhanced implementation
        {
            if (groupVM == null) return;

            var result = CustomMessageBox.Show($"Bạn có chắc chắn muốn rời khỏi nhóm '{groupVM.Name}'?",
                                        "Xác nhận rời nhóm",
                                        CustomMessageBoxWindow.MessageButtons.YesNo,
                                        CustomMessageBoxWindow.MessageIcon.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var group = groupVM.GetModel();
                    var currentUserEmail = SharedData.Instance.userdata.Email;
                    
                    // Remove from group
                    await RemoveUserFromGroup(group, currentUserEmail);
                    
                    // Remove group contact from user's contacts
                    var db = FirestoreHelper.database;
                    await db.Collection("users").Document(currentUserEmail)
                           .Collection("contacts").Document(group.GroupChatId).DeleteAsync();
                    
                    // Remove from local collections
                    CombinedContacts.Remove(groupVM);
                    if (SelectedContact == groupVM) SelectedContact = null;

                    CustomMessageBox.Show($"Đã rời khỏi nhóm '{groupVM.Name}' thành công.", "Thành công",
                                        CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Information);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error leaving group: {ex.Message}");
                    CustomMessageBox.Show($"Lỗi khi rời nhóm: {ex.Message}", "Lỗi",
                                        CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
                }
            }
        }

        private bool CanLeaveGroup() => SelectedContact is GroupViewModel;

        public static string GenerateChatId(string email1, string email2)
        {
            var emails = new string[] { email1, email2 };
            Array.Sort(emails);
            var combinedEmails = string.Join(":", emails);

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedEmails));

                // Base64 URL-safe: replace + and /, remove =
                string base64 = Convert.ToBase64String(hashBytes);
                return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
            }
        }

        private string GenerateGroupChatId()
        {
            return $"group_{Guid.NewGuid():N}";
        }

        public static string GenerateGroupChatId(List<string> memberEmails)
        {
            // Sort emails to ensure consistent ID generation
            var sortedEmails = memberEmails.OrderBy(e => e).ToList();
            var combinedEmails = string.Join(":", sortedEmails);

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes($"group:{combinedEmails}"));
                string base64 = Convert.ToBase64String(hashBytes);
                return $"group_{base64.Replace('+', '-').Replace('/', '_').TrimEnd('=')}";
            }
        }

        public async Task AddContactAsync(string email, Contact contact)
        {
            var db = FirestoreHelper.database;
            var userDocRef = db.Collection("users").Document(email);

            // Kiểm tra xem người dùng có tồn tại không
            var userDocSnapshot = await userDocRef.GetSnapshotAsync();
            if (!userDocSnapshot.Exists)
            {
                CustomMessageBox.Show($"User {email} does not exist.", "Lỗi",
                                    CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
                return;
            }

            // Thêm contact vào subcollection 'contacts' của người dùng
            var contactDocRef = userDocRef.Collection("contacts").Document(contact.chatID); // Dùng chat id làm document ID

            try
            {
                await contactDocRef.SetAsync(contact); // Lưu thông tin contact
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error adding contact: {ex.Message}", "Lỗi",
                                    CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
            }
        }

        public async Task<List<Contact>> GetContactsAsync(string email)
        {
            var db = FirestoreHelper.database;
            var userDocRef = db.Collection("users").Document(email);
            var contactsCollectionRef = userDocRef.Collection("contacts");
            var contactsSnapshot = await contactsCollectionRef.GetSnapshotAsync();

            var contacts = new List<Contact>();

            foreach (var contactDoc in contactsSnapshot.Documents)
            {
                var contact = contactDoc.ConvertTo<Contact>();
                contacts.Add(contact);
            }

            return contacts;
        }

        [RelayCommand(CanExecute = nameof(CanSendMessage))]
        private async Task SendMessage(IChatContact contactVM) // Changed to Task
        {
            if (contactVM == null) return;

            var currentUser = SharedData.Instance.userdata;
            // Tạo chatID duy nhất và nhất quán
            var chatID = GenerateChatId(currentUser.Email, contactVM.Email);

            // Tạo đối tượng Contact đại diện cho người bạn mà mình muốn chat
            var contactForCurrentUser = new Contact
            {
                AvatarUrl = contactVM.AvatarUrl,
                Name = contactVM.Name,
                Email = contactVM.Email,
                chatID = chatID, // Sử dụng chatID đã tạo
                IsOnline = true, // Tạm thời, trạng thái online sẽ được cập nhật sau
            };

            // Tạo đối tượng Contact đại diện cho mình trong danh sách của người bạn
            var contactForFriend = new Contact
            {
                AvatarUrl = currentUser.AvatarUrl,
                Name = currentUser.Name,
                Email = currentUser.Email,
                chatID = chatID, // Sử dụng cùng chatID
                IsOnline = true, // Tạm thời
            };
            var contactsOfA = await GetContactsAsync(currentUser.Email);
            if (!contactsOfA.Any(c => c.chatID == chatID))
            {
                await AddContactAsync(currentUser.Email, contactForCurrentUser);
            }

            var contactsOfB = await GetContactsAsync(contactVM.Email);
            if (!contactsOfB.Any(c => c.chatID == chatID))
            {
                await AddContactAsync(contactVM.Email, contactForFriend);
            }
            EventAggregator.Instance.Publish(new StartChatEvent(contactForCurrentUser));
        }

        private bool CanSendMessage() => SelectedContact != null;

        private async Task RemoveFriendRequestAsync(string myEmail, string requesterId)
        {
            try
            {
                var db = FirestoreHelper.database;
                var requestDocRef = db.Collection("AddFriendQuery").Document(myEmail);

                // Lấy snapshot hiện tại
                var snapshot = await requestDocRef.GetSnapshotAsync();
                if (snapshot.Exists)
                {
                    // Lấy danh sách requests
                    var requests = snapshot.GetValue<List<Dictionary<string, object>>>("requests");

                    if (requests != null)
                    {
                        // Tìm request cần xóa
                        var requestToRemove = requests.FirstOrDefault(r =>
                            r.ContainsKey("EmailRequesterId") && r["EmailRequesterId"].ToString() == requesterId);

                        if (requestToRemove != null)
                        {
                            // Xóa bằng ArrayRemove
                            await requestDocRef.UpdateAsync(new Dictionary<string, object>
                    {
                        { "requests", FieldValue.ArrayRemove(requestToRemove) }
                    });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Lỗi khi xóa friend request: {ex.Message}", "Lỗi",
                                    CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
            }
        }

        [RelayCommand]
        private async Task AcceptRequest(FriendRequestViewModel requestVM) // Changed to Task
        {
            if (requestVM == null) return;
            FriendRequest requestModel = requestVM.GetModel();

            CustomMessageBox.Show($"Đã chấp nhận lời mời từ: {requestModel.RequesterName} ({requestModel.EmailRequesterId})",
                                "Thành công", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Information);

            // --- Tạo đối tượng bạn bè mới ---
            var newFriendModel = new FriendData
            {
                Email = requestModel.EmailRequesterId,
                Name = requestModel.RequesterName,
                AvatarUrl = requestModel.RequesterAvatarUrl ?? "",
                IsOnline = false
            };
            var newFriendVM = new FriendViewModel(newFriendModel);

            // --- Lưu vào local ---
            CombinedContacts.Add(newFriendVM);
            FriendRequests.Remove(requestVM);
            SortContacts();

            // --- Lưu vào Firestore ---
            var db = FirestoreHelper.database;
            var myDocRef = db.Collection("Friend").Document(SharedData.Instance.userdata.Email);

            var friendData = new Dictionary<string, object>
            {
                { "Email", newFriendModel.Email },
                { "Name", newFriendModel.Name },
                { "AvatarUrl", newFriendModel.AvatarUrl },
                { "IsOnline", newFriendModel.IsOnline }
            };

            try
            {
                // --- Thêm friend vào tài khoản mình ---
                var mySnapshot = await myDocRef.GetSnapshotAsync();
                if (!mySnapshot.Exists)
                {
                    await myDocRef.SetAsync(new Dictionary<string, object>
                        {
                            { "friends", new List<object> { friendData } }
                        });
                }
                else
                {
                    await myDocRef.UpdateAsync(new Dictionary<string, object>
                    {
                        {"friends", FieldValue.ArrayUnion(friendData) }
                    });
                }

                // --- Thêm mình vào tài khoản friend ---
                var friendDocRef = db.Collection("Friend").Document(newFriendModel.Email);
                var friendDocSnapshot = await friendDocRef.GetSnapshotAsync();

                var myDataForFriend = new Dictionary<string, object>
                {
                    { "Email", SharedData.Instance.userdata.Email },
                    { "Name", SharedData.Instance.userdata.Name },
                    { "AvatarUrl", SharedData.Instance.userdata.AvatarUrl ?? "" },
                    { "IsOnline", false }
                };

                if (!friendDocSnapshot.Exists)
                {
                    await friendDocRef.SetAsync(new Dictionary<string, object>
                        {
                            { "friends", new List<object> { myDataForFriend } }
                        });
                }
                else
                {
                    await friendDocRef.UpdateAsync(new Dictionary<string, object>
                        {
                            { "friends", FieldValue.ArrayUnion(myDataForFriend) }
                         });
                }

                // --- Xóa yêu cầu kết bạn ---
                await RemoveFriendRequestAsync(myEmail: SharedData.Instance.userdata.Email, requesterId: requestModel.EmailRequesterId);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Có lỗi khi cập nhật dữ liệu: {ex.Message}", "Lỗi",
                                    CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
            }
        }

        [RelayCommand]
        private async Task DeclineRequest(FriendRequestViewModel requestVM) // Changed to Task
        {
            if (requestVM == null) return;
            FriendRequest requestModel = requestVM.GetModel();
            CustomMessageBox.Show($"Đã từ chối lời mời từ: {requestModel.RequesterName} ({requestModel.EmailRequesterId})",
                                "Thông báo", CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Information);

            FriendRequests.Remove(requestVM);
            await RemoveFriendRequestAsync(myEmail: SharedData.Instance.userdata.Email, requesterId: requestModel.EmailRequesterId);
        }

        public async Task<List<FriendData>> LoadFriendsAsync(string email)
        {
            var db = FirestoreHelper.database;
            var friendList = new List<FriendData>();

            try
            {
                var docRef = db.Collection("Friend").Document(email);
                var snapshot = await docRef.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    if (snapshot.ContainsField("friends"))
                    {
                        var friendsRaw = snapshot.GetValue<List<object>>("friends");

                        foreach (var friendObj in friendsRaw)
                        {
                            if (friendObj is Dictionary<string, object> friendDict)
                            {
                                var friend = new FriendData
                                {
                                    Email = friendDict.ContainsKey("Email") ? friendDict["Email"]?.ToString() ?? "" : "",
                                    Name = friendDict.ContainsKey("Name") ? friendDict["Name"]?.ToString() ?? "" : "",
                                    AvatarUrl = friendDict.ContainsKey("AvatarUrl") ? friendDict["AvatarUrl"]?.ToString() ?? "" : "",
                                    IsOnline = friendDict.ContainsKey("IsOnline") ? Convert.ToBoolean(friendDict["IsOnline"]) : false
                                };

                                friendList.Add(friend);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading friends: {ex.Message}");
            }
            return friendList;
        }

        public async Task<List<Group>> LoadGroupsAsync(string userEmail)
        {
            var db = FirestoreHelper.database;
            var groupList = new List<Group>();

            try
            {
                // Get user's groups
                var userGroupsRef = db.Collection("UserGroups").Document(userEmail);
                var userGroupsSnapshot = await userGroupsRef.GetSnapshotAsync();

                if (userGroupsSnapshot.Exists && userGroupsSnapshot.ContainsField("groups"))
                {
                    var groupIds = userGroupsSnapshot.GetValue<List<string>>("groups");

                    foreach (var groupId in groupIds)
                    {
                        var groupDocRef = db.Collection("Groups").Document(groupId);
                        var groupSnapshot = await groupDocRef.GetSnapshotAsync();

                        if (groupSnapshot.Exists)
                        {
                            var group = groupSnapshot.ConvertTo<Group>();
                            
                            // SỬA LỖI: Đảm bảo avatar URL được load đúng cách
                            if (string.IsNullOrEmpty(group.AvatarUrl) || group.AvatarUrl == "/Assets/DefaultGroupAvatar.png")
                            {
                                group.AvatarUrl = "/Assets/DefaultGroupAvatar.png";
                            }
                            
                            groupList.Add(group);
                            Debug.WriteLine($"Loaded group: {group.Name} with avatar: {group.AvatarUrl}");
                        }
                    }
                }
                
                Debug.WriteLine($"Loaded {groupList.Count} groups for user {userEmail}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading groups: {ex.Message}");
                Console.WriteLine($"Error loading groups: {ex.Message}");
            }

            return groupList;
        }

        private async Task SaveGroupToFirestore(Group group)
        {
            try
            {
                var db = FirestoreHelper.database;
                var groupDocRef = db.Collection("Groups").Document(group.Id);
                await groupDocRef.SetAsync(group);

                // Add group to user's groups collection
                var userGroupRef = db.Collection("UserGroups").Document(SharedData.Instance.userdata.Email);
                var userGroupsSnapshot = await userGroupRef.GetSnapshotAsync();

                if (!userGroupsSnapshot.Exists)
                {
                    await userGroupRef.SetAsync(new Dictionary<string, object>
                    {
                        { "groups", new List<string> { group.Id } }
                    });
                }
                else
                {
                    await userGroupRef.UpdateAsync("groups", FieldValue.ArrayUnion(group.Id));
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lưu nhóm: {ex.Message}");
            }
        }

        public async Task AddMembersToGroup(Group group, List<string> memberEmails)
        {
            try
            {
                var db = FirestoreHelper.database;
                var groupDocRef = db.Collection("Groups").Document(group.Id);

                // Add members to group
                foreach (var email in memberEmails)
                {
                    if (!group.MemberEmails.Contains(email))
                    {
                        await groupDocRef.UpdateAsync("MemberEmails", FieldValue.ArrayUnion(email));
                        Debug.WriteLine($"Added {email} to group {group.Name}");

                        // Add group to member's groups collection
                        var memberGroupRef = db.Collection("UserGroups").Document(email);
                        var memberGroupsSnapshot = await memberGroupRef.GetSnapshotAsync();

                        if (!memberGroupsSnapshot.Exists)
                        {
                            await memberGroupRef.SetAsync(new Dictionary<string, object>
                            {
                                { "groups", new List<string> { group.Id } }
                            });
                            Debug.WriteLine($"Created new UserGroups document for {email}");
                        }
                        else
                        {
                            await memberGroupRef.UpdateAsync("groups", FieldValue.ArrayUnion(group.Id));
                            Debug.WriteLine($"Added group to existing UserGroups for {email}");
                        }
                    }
                }

                // Update member count
                await groupDocRef.UpdateAsync("MemberCount", group.MemberEmails.Count + memberEmails.Count);
                Debug.WriteLine($"Updated member count to {group.MemberEmails.Count + memberEmails.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AddMembersToGroup: {ex.Message}");
                throw new Exception($"Lỗi khi thêm thành viên: {ex.Message}");
            }
        }

        public async Task RemoveUserFromGroup(Group group, string userEmail)
        {
            try
            {
                var db = FirestoreHelper.database;
                var groupDocRef = db.Collection("Groups").Document(group.Id);

                // Remove user from group members
                await groupDocRef.UpdateAsync("MemberEmails", FieldValue.ArrayRemove(userEmail));
                await groupDocRef.UpdateAsync("AdminEmails", FieldValue.ArrayRemove(userEmail));
                await groupDocRef.UpdateAsync("MemberCount", FieldValue.Increment(-1));

                // Remove group from user's groups
                var userGroupRef = db.Collection("UserGroups").Document(userEmail);
                await userGroupRef.UpdateAsync("groups", FieldValue.ArrayRemove(group.Id));
                
                Debug.WriteLine($"Successfully removed {userEmail} from group {group.Name}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error removing user from group: {ex.Message}");
                throw new Exception($"Lỗi khi xóa thành viên khỏi nhóm: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task CreateGroup() // Updated method
        {
            try
            {
                var createGroupWindow = new Views.CreateGroupWindow
                {
                    Owner = Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (createGroupWindow.ShowDialog() == true)
                {
                    var groupData = new GroupData
                    {
                        Name = createGroupWindow.GroupName,
                        Description = createGroupWindow.GroupDescription,
                        AvatarUrl = createGroupWindow.SelectedAvatarPath
                    };

                    // Get selected friends
                    var selectedFriendEmails = createGroupWindow.GetSelectedFriendEmails();

                    // Upload avatar if selected - SỬA LỖI: Kiểm tra file tồn tại trước
                    string finalAvatarUrl = "/Assets/DefaultGroupAvatar.png"; // Default avatar
                    
                    if (!string.IsNullOrEmpty(groupData.AvatarUrl) && File.Exists(groupData.AvatarUrl))
                    {
                        try
                        {
                            string fileName = $"group_avatar_{DateTime.Now:yyyyMMddHHmmssfff}_{Path.GetFileName(groupData.AvatarUrl)}";
                            string uploadedUrl = await FirebaseStorageHelper.UploadFileAsync(groupData.AvatarUrl, fileName);
                            finalAvatarUrl = uploadedUrl;
                            Debug.WriteLine($"Avatar uploaded successfully: {uploadedUrl}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Avatar upload failed: {ex.Message}");
                            CustomMessageBox.Show($"Không thể tải lên ảnh đại diện: {ex.Message}", "Cảnh báo",
                                CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Warning);
                        }
                    }

                    // Create initial member list with current user
                    var initialMembers = new List<string> { SharedData.Instance.userdata.Email };
                    initialMembers.AddRange(selectedFriendEmails);

                    // Create group with current user as admin and selected friends as members
                    var newGroup = new Group
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = groupData.Name,
                        Description = groupData.Description,
                        AvatarUrl = finalAvatarUrl, // Sử dụng URL đã xử lý
                        CreatedBy = SharedData.Instance.userdata.Email,
                        CreatedAt = DateTime.UtcNow,
                        MemberEmails = initialMembers,
                        AdminEmails = new List<string> { SharedData.Instance.userdata.Email },
                        MemberCount = initialMembers.Count,
                        GroupChatId = GenerateGroupChatId()
                    };

                    await SaveGroupToFirestore(newGroup);

                    // Add group to all members' groups collection
                    if (selectedFriendEmails.Any())
                    {
                        await AddMembersToGroup(newGroup, selectedFriendEmails);
                    }

                    // Automatically create group contact for chat for all members
                    var groupContact = new Contact
                    {
                        Name = newGroup.Name,
                        Email = newGroup.GroupChatId,
                        AvatarUrl = newGroup.AvatarUrl,
                        chatID = newGroup.GroupChatId,
                        IsOnline = true
                    };

                    // Add contact for current user
                    await AddContactAsync(SharedData.Instance.userdata.Email, groupContact);

                    // Add contact for all selected friends - SỬA LỖI: Đảm bảo tất cả thành viên đều có contact
                    foreach (var friendEmail in selectedFriendEmails)
                    {
                        try
                        {
                            await AddContactAsync(friendEmail, groupContact);
                            Debug.WriteLine($"Added group contact for member: {friendEmail}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Failed to add group contact for {friendEmail}: {ex.Message}");
                        }
                    }

                    var groupVM = new GroupViewModel(newGroup);
                    CombinedContacts.Add(groupVM);
                    SortContacts();

                    string memberInfo = selectedFriendEmails.Any()
                        ? $" với {selectedFriendEmails.Count} thành viên"
                        : "";

                    CustomMessageBox.Show($"Nhóm '{newGroup.Name}' đã được tạo thành công{memberInfo}!", "Thành công",
                        CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Information);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating group: {ex.Message}");
                CustomMessageBox.Show($"Lỗi khi tạo nhóm: {ex.Message}", "Lỗi",
                    CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
            }
        }

        [RelayCommand]
        private async Task InviteToGroup(GroupViewModel groupVM) // Updated implementation
        {
            if (groupVM == null) return;

            // Use the new member management window instead
            await ManageGroupMembers(groupVM);
        }

        [RelayCommand]
        private async Task ManageGroupMembers(GroupViewModel groupVM)
        {
            if (groupVM == null) return;

            try
            {
                var group = groupVM.GetModel();
                var currentUserEmail = SharedData.Instance.userdata.Email;

                // Check if user has permission to manage members
                bool canManage = group.AdminEmails.Contains(currentUserEmail) || 
                                group.CreatedBy == currentUserEmail;

                if (!canManage)
                {
                    CustomMessageBox.Show("Bạn không có quyền quản lý thành viên của nhóm này.", "Không có quyền",
                        CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Warning);
                    return;
                }

                var managementViewModel = new GroupMemberManagementViewModel(group, this);
                var managementWindow = new GroupMemberManagementWindow(managementViewModel)
                {
                    Owner = Application.Current.MainWindow
                };

                managementWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening member management: {ex.Message}");
                CustomMessageBox.Show($"Lỗi khi mở quản lý thành viên: {ex.Message}", "Lỗi",
                    CustomMessageBoxWindow.MessageButtons.OK, CustomMessageBoxWindow.MessageIcon.Error);
            }
        }

        // --- Helper Methods ---
        private async void LoadInitialData()
        {
            // Load friends (existing code)
            List<FriendData> friends = await LoadFriendsAsync(SharedData.Instance.userdata.Email);

            // Load groups (new code)
            List<Group> groups = await LoadGroupsAsync(SharedData.Instance.userdata.Email);

            var db = FirestoreHelper.database;
            var doc = db.Collection("AddFriendQuery").Document(SharedData.Instance.userdata.Email);
            var snapshot = await doc.GetSnapshotAsync();
            List<FriendRequest> requests = new List<FriendRequest>();
            if (snapshot.Exists)
            {
                if (snapshot.TryGetValue<List<Dictionary<string, object>>>("requests", out var requestList))
                {
                    foreach (var request in requestList)
                    {
                        FriendRequest newRequest = new FriendRequest
                        {
                            EmailRequestId = request["EmailRequestId"]?.ToString() ?? "",
                            RequesterAvatarUrl = request.ContainsKey("RequesterAvatarUrl") ? request["RequesterAvatarUrl"]?.ToString() : null,
                            RequesterName = request["RequesterName"]?.ToString() ?? "",
                            EmailRequesterId = request["EmailRequesterId"]?.ToString() ?? "",
                            RequestTime = request.ContainsKey("RequestTime") ? ((Google.Cloud.Firestore.Timestamp)request["RequestTime"]).ToDateTime() : DateTime.UtcNow,
                        };
                        requests.Add(newRequest);
                    }
                }
            }

            // *** Chuyển Models thành ViewModels ***
            CombinedContacts.Clear();

            // Add friends
            foreach (var friend in friends)
            {
                CombinedContacts.Add(new FriendViewModel(friend));
            }

            // Add groups
            foreach (var group in groups)
            {
                CombinedContacts.Add(new GroupViewModel(group));
            }

            FriendRequests.Clear();
            foreach (var request in requests)
            {
                FriendRequests.Add(new FriendRequestViewModel(request));
            }

            SortContacts();
            await StartListeningToFriendStatusAsync();
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
        private string EscapeEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return string.Empty;
            return email.Replace('.', '_')
                         .Replace('@', '_')
                         .Replace('#', '_')
                         .Replace('$', '_')
                         .Replace('[', '_')
                         .Replace(']', '_')
                         .Replace('/', '_');
        }

        private readonly Dictionary<string, IDisposable> _friendStatusListeners = new Dictionary<string, IDisposable>();

        private async Task StartListeningToFriendStatusAsync()
        {
            foreach (var listener in _friendStatusListeners.Values)
            {
                listener?.Dispose();
            }
            _friendStatusListeners.Clear();
            Debug.WriteLine("Cleared old friend status listeners.");
            Debug.WriteLine($"Kiểm tra... Số lượng trong CombinedContacts: {CombinedContacts.Count}");

            // Dùng OfType<FriendViewModel>() để chỉ lấy những contact là bạn bè, bỏ qua các nhóm.
            var friends = CombinedContacts.OfType<FriendViewModel>().ToList();

            Debug.WriteLine($"Đã tìm thấy {friends.Count} FriendViewModel để lắng nghe.");

            if (!friends.Any())
            {
                Debug.WriteLine("No friends in the list to listen to.");
                return;
            }

            foreach (var friendVM in friends)
            {
                if (friendVM == null || string.IsNullOrEmpty(friendVM.Email)) continue;

                string friendEmailToListen = friendVM.Email;
                string escapedFriendEmail = EscapeEmail(friendEmailToListen);
                Debug.WriteLine($"---{escapedFriendEmail}---");

                try
                {
                    string pathToListen = $"user_status/{escapedFriendEmail}";
                    Debug.WriteLine($"---> Đang lắng nghe trên đường dẫn: {pathToListen}");

                    var statusListener = firebaseClient
                        .Child(pathToListen)
                        .AsObservable<object>()
                        .Subscribe(
                            rawEvent =>
                            {

                                if (rawEvent.Key == "isOnline")
                                {
                                    Debug.WriteLine($"[isOnline EVENT] cho {friendEmailToListen}");

                                    bool newIsOnline = false;
                                    if (rawEvent.Object != null)
                                    {
                                        try { newIsOnline = Convert.ToBoolean(rawEvent.Object); }
                                        catch { newIsOnline = false; }
                                    }

                                    Debug.WriteLine($"---> Trạng thái IsOnline mới là: {newIsOnline}");

                                    // Cập nhật UI trên luồng chính
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        var friendToUpdate = CombinedContacts
                                            .OfType<FriendViewModel>()
                                            .FirstOrDefault(f => f.Email == friendEmailToListen);

                                        if (friendToUpdate != null)
                                        {
                                            Debug.WriteLine($"---> Đang cập nhật IsOnline cho {friendToUpdate.Name} thành {newIsOnline}");
                                            friendToUpdate.IsOnline = newIsOnline;
                                        }
                                    });
                                }
                            },
                            // Hành động khi có lỗi
                            error =>
                            {
                                Debug.WriteLine($"[LISTENER ERROR] Lỗi khi lắng nghe {friendEmailToListen}: {error.Message}");
                            });

                    _friendStatusListeners[friendEmailToListen] = statusListener;
                    Debug.WriteLine($"---> Đã thiết lập listener thành công cho: {friendEmailToListen}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PRESENCE SETUP FAIL] Không thể thiết lập listener cho {friendEmailToListen}: {ex.Message}");
                }
            }
        }
    }
}