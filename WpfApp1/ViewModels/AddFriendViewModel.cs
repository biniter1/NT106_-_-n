// File: ViewModels/AddFriendViewModel.cs
using System;
using Google.Cloud.Firestore;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WpfApp1.Models; // Namespace chứa UserSearchResult
using System.Windows;
using System.Threading.Tasks;
using System.Collections.Generic;
namespace WpfApp1.ViewModels 
{
    public partial class AddFriendViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SearchCommand))] 
        private string _searchQuery = ""; 
        public User userdata;
        static List<FriendRequest> friendRequests;
        public ObservableCollection<UserSearchResult> SearchResults { get; } = new();

        [RelayCommand(CanExecute = nameof(CanSearch))] 
        private async Task SearchAsync() 
        {
            SearchResults.Clear();
            try
            {
                var db=FirestoreHelper.database;
                var doc=db.Collection("users").Document(SearchQuery);
                var snapShot = await doc.GetSnapshotAsync();

                var userData = snapShot.ConvertTo<User>();
                if (userData != null)
                {
                    SearchResults.Add(new UserSearchResult { UserId = userData.Email, DisplayName = $"{userData.Username}", Status = FriendStatus.NotFriend });
                    userdata=userData;
                    friendRequests=SharedData.friendRequests;
                }
                else
                {
                    MessageBox.Show("Nguoi dung khong ton tai");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi tìm kiếm: {ex.Message}");
                
                MessageBox.Show($"Đã xảy ra lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                 
                 AddFriendCommand.NotifyCanExecuteChanged();
            }
        }
        private bool CanSearch()
        {
            return !SearchCommand.IsRunning && !string.IsNullOrWhiteSpace(SearchQuery);
        }
        private async Task<bool> AreAlreadyFriends(string myEmail, string otherEmail)
        {
            var db = FirestoreHelper.database;
            var myFriendDocRef = db.Collection("Friend").Document(myEmail);
            var myFriendDocSnapshot = await myFriendDocRef.GetSnapshotAsync();

            if (myFriendDocSnapshot.Exists)
            {
                var friendsList = myFriendDocSnapshot.GetValue<List<Dictionary<string, object>>>("friends");

                if (friendsList != null)
                {
                    return friendsList.Any(friend =>
                        friend.ContainsKey("Email") && friend["Email"].ToString() == otherEmail);
                }
            }

            return false;
        }

        [RelayCommand(CanExecute = nameof(CanAddFriend))] 
        private async Task AddFriendAsync(UserSearchResult user) 
        {
            if (user == null) return;
            bool checkFriend=await AreAlreadyFriends(SharedData.Instance.userdata.Email,userdata.Email);
            if (checkFriend)
            {
                MessageBox.Show("Bạn đã là bạn với người này rồi");
                user.Status=FriendStatus.Friend;
                return;
            }
            var db = FirestoreHelper.database;
            FriendRequest friendRequest = new FriendRequest
            {
                EmailRequestId = user.UserId,
                EmailRequesterId = SharedData.Instance.userdata.Email,
                RequesterName = SharedData.Instance.userdata.Name,
                RequesterAvatarUrl = "null",
                RequestTime = DateTime.UtcNow,
            };
            friendRequests.Add(friendRequest);
            user.Status = FriendStatus.RequestSent;
            try
            {
                var doc = db.Collection("AddFriendQuery").Document(friendRequest.EmailRequestId);
                await doc.UpdateAsync(new Dictionary<string, object>
                {{ "requests", FieldValue.ArrayUnion(friendRequest) }
                });
            }
            catch (Exception ex)
            {
                var initialData = new Dictionary<string, object>
                {
                    { "requests", new List<FriendRequest> { new FriendRequest
                        {
                            EmailRequestId = user.UserId,
                            EmailRequesterId = SharedData.Instance.userdata.Email,
                            RequesterName = SharedData.Instance.userdata.Name,
                            RequesterAvatarUrl = "null",
                            RequestTime = DateTime.UtcNow,
                        }
                    }}
                };
                await db.Collection("AddFriendQuery").Document(friendRequest.EmailRequestId).SetAsync(initialData);
            }   
        }
        private bool CanAddFriend(UserSearchResult user)
        {
            return user != null
                && user.Status == FriendStatus.NotFriend
                && !AddFriendCommand.IsRunning;
        }

    }
}