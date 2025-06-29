using System;
using Google.Cloud.Firestore;

namespace WpfApp1.Models
{
    [FirestoreData]
    public class MatchRequest
    {
        [FirestoreProperty]
        public string Id { get; set; }

        [FirestoreProperty]
        public string FromEmail { get; set; }

        [FirestoreProperty]
        public string ToEmail { get; set; }

        [FirestoreProperty]
        public string FromName { get; set; }

        [FirestoreProperty]
        public string ToName { get; set; }

        [FirestoreProperty]
        public string Message { get; set; }

        [FirestoreProperty]
        public DateTime CreatedAt { get; set; }

        [FirestoreProperty]
        public MatchRequestStatus Status { get; set; }

        [FirestoreProperty]
        public string FromAvatarUrl { get; set; }

        public MatchRequest()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
            Status = MatchRequestStatus.Pending;
        }
    }

    public enum MatchRequestStatus
    {
        Pending = 0,
        Accepted = 1,
        Rejected = 2,
        Cancelled = 3
    }
}