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
using WpfApp1.Services;

namespace WpfApp1.ViewModels
{
    public partial class FriendListViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(UnfriendCommand))]
        [NotifyCanExecuteChangedFor(nameof(LeaveGroupCommand))]
        [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
        private IChatContact _selectedContact; // Vẫn là IChatContact
        public User userdata = SharedData.Instance.userdata;

        [ObservableProperty]
        private ObservableObject _currentViewModel;

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
        private async void Unfriend(FriendViewModel friendVM) // Tham số là FriendViewModel
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

                MessageBox.Show("Đã hủy kết bạn thành công.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Có lỗi khi hủy kết bạn: {ex.Message}");
            }

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
        public async Task AddContactAsync(string email, Contact contact)
        {
            var db = FirestoreHelper.database;
            var userDocRef = db.Collection("users").Document(email);

            // Kiểm tra xem người dùng có tồn tại không
            var userDocSnapshot = await userDocRef.GetSnapshotAsync();
            if (!userDocSnapshot.Exists)
            {
                MessageBox.Show($"User {email} does not exist.");
                return;
            }

            // Thêm contact vào subcollection 'contacts' của người dùng
            var contactDocRef = userDocRef.Collection("contacts").Document(contact.chatID); // Dùng chat id làm document ID

            try
            {
                await contactDocRef.SetAsync(contact); // Lưu thông tin contact
                //MessageBox.Show($"Contact with {contact.Name} ({contact.Email}) has been added successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding contact: {ex.Message}");
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
        private async void SendMessage(IChatContact contactVM)
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

            // Kiểm tra và thêm contact cho cả hai người nếu chưa tồn tại
            // (Logic này của bạn đã đúng)
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

            // --- THAY ĐỔI TẠI ĐÂY ---
            // Thay vì hiển thị MessageBox, hãy phát sự kiện để yêu cầu mở chat

            // Phát sự kiện và gửi đi đối tượng contact mà ChatView cần để làm việc
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
                MessageBox.Show($"Lỗi khi xóa friend request: {ex.Message}");
            }
        }
        [RelayCommand]
        private async void AcceptRequest(FriendRequestViewModel requestVM)
        {
            if (requestVM == null) return;
            FriendRequest requestModel = requestVM.GetModel();

            MessageBox.Show($"Đã chấp nhận lời mời từ: {requestModel.RequesterName} ({requestModel.EmailRequesterId})");

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
                MessageBox.Show($"Có lỗi khi cập nhật dữ liệu: {ex.Message}");
            }
        }



        [RelayCommand]
        private async void DeclineRequest(FriendRequestViewModel requestVM)
        {
            if (requestVM == null) return;
            FriendRequest requestModel = requestVM.GetModel();
            MessageBox.Show($"Đã từ chối lời mời từ: {requestModel.RequesterName} ({requestModel.EmailRequesterId})");

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
                                    Email = friendDict.ContainsKey("Email") ? friendDict["Email"].ToString() : "",
                                    Name = friendDict.ContainsKey("Name") ? friendDict["Name"].ToString() : "",
                                    AvatarUrl = friendDict.ContainsKey("AvatarUrl") ? friendDict["AvatarUrl"].ToString() : "",
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
        // --- Helper Methods ---
        private async void LoadInitialData()
        {
            List<FriendData> friends = await LoadFriendsAsync(SharedData.Instance.userdata.Email);

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
                            EmailRequestId = request["EmailRequestId"]?.ToString(),
                            RequesterAvatarUrl = request.ContainsKey("RequesterAvatarUrl") ? request["RequesterAvatarUrl"]?.ToString() : null,
                            RequesterName = request["RequesterName"]?.ToString(),
                            EmailRequesterId = request["EmailRequesterId"]?.ToString(),
                            RequestTime = request.ContainsKey("RequestTime") ? ((Google.Cloud.Firestore.Timestamp)request["RequestTime"]).ToDateTime() : DateTime.UtcNow,

                        };
                        requests.Add(newRequest);
                    }
                }
            }
            // *** Chuyển Models thành ViewModels ***
            CombinedContacts.Clear();
            foreach (var friend in friends)
            {
                CombinedContacts.Add(new FriendViewModel(friend));
            }
            FriendRequests.Clear();
            foreach (var request in requests)
            {
                FriendRequests.Add(new FriendRequestViewModel(request));
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
