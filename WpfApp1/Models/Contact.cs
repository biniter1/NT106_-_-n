using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Google.Cloud.Firestore;

namespace WpfApp1.Models
{

    [FirestoreData]
    public class Contact : ObservableObject
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

    }
}