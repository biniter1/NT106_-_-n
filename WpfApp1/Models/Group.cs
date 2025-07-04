using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;

namespace WpfApp1.Models
{
    [FirestoreData]
    public class Group
    {
        [FirestoreProperty]
        public string Id { get; set; }
        
        [FirestoreProperty]
        public string Name { get; set; }
        
        [FirestoreProperty]
        public string AvatarUrl { get; set; }
        
        [FirestoreProperty]
        public int MemberCount { get; set; }
        
        [FirestoreProperty]
        public List<string> MemberEmails { get; set; } = new List<string>();
        
        [FirestoreProperty]
        public string CreatedBy { get; set; }
        
        [FirestoreProperty]
        public DateTime CreatedAt { get; set; }
        
        [FirestoreProperty]
        public string Description { get; set; }
        
        [FirestoreProperty]
        public List<string> AdminEmails { get; set; } = new List<string>();
        
        [FirestoreProperty]
        public string GroupChatId { get; set; }
        
        public Group()
        {
            MemberEmails = new List<string>();
            AdminEmails = new List<string>();
            CreatedAt = DateTime.UtcNow;
        }
    }
}