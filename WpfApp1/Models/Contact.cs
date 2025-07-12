using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Google.Cloud.Firestore;

namespace WpfApp1.Models
{

    [FirestoreData]
    public partial class Contact : ObservableObject
    {

        private bool _isOnline;
        public Contact()
        {
            IsOnline = false;
        }

        [FirestoreProperty]
        public string AvatarUrl { get; set; }

        [FirestoreProperty]
        public string Name { get; set; }

        [FirestoreProperty]
        public string Email { get; set; }

        [FirestoreProperty]
        public string LastMessage { get; set; }

        [FirestoreProperty]
        public DateTime? LastMessageTime { get; set; }

        [FirestoreProperty]
        public bool IsOnline
        {
            get => _isOnline;
            set => SetProperty(ref _isOnline, value);
        }

        [FirestoreProperty]
        public string chatID { get; set; }

        [FirestoreProperty]
        public bool IsLoadingAvatar { get; set; }

        [ObservableProperty]
        private bool _hasUnreadMessages;

        private bool _isTyping;
        public bool IsTyping // <--- Thêm "public" vào đây
        {
            get => _isTyping;
            set
            {
                if (_isTyping != value)
                {
                    _isTyping = value;
                    OnPropertyChanged(nameof(IsTyping));
                }
            }
        }

        [ObservableProperty]
        private bool _isBlockedByMe;

        [ObservableProperty]
        private bool _isBlockingMe;
        public bool InteractionIsBlocked => IsBlockedByMe || IsBlockingMe;

        [Newtonsoft.Json.JsonIgnore] 
        public bool IsGroupChat => !string.IsNullOrEmpty(chatID) && chatID.StartsWith("group_");
    }
}