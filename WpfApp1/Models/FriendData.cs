using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Firestore;

namespace WpfApp1.Models
{
    [FirestoreData]
    public  class FriendData
    {
        [FirestoreProperty]
        public string Email {  get; set; }

        [FirestoreProperty]
        public string Name { get; set; }

        [FirestoreProperty]
        public  string AvatarUrl { get; set; }

        [FirestoreProperty]
        public  bool IsOnline {  get; set; }

        public FriendData()
        {

        }
    }

}
