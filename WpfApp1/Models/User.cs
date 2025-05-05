using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Firestore;

namespace WpfApp1.Models
{
    [FirestoreData]
    public class User
    {

        [FirestoreProperty]
        public string Ho { get; set; }
        [FirestoreProperty]
        public string Name { get; set; }
        [FirestoreProperty]
        public string Email { get; set; }
        [FirestoreProperty]
        public string Password { get; set; }
        [FirestoreProperty]
        public string Username { get; set; }
        [FirestoreProperty]

        public string Phone { get; set; }
        [FirestoreProperty]

        public string gender { get; set; }
        [FirestoreProperty]
        public DateTime DateTime { get; set; }
        [FirestoreProperty]
        public string Address { get; set; }
        [FirestoreProperty]
        public string IdToken { get; set; }

        [FirestoreProperty]
        public string AvatarUrl { get; set; }

        public User()
        {

        }

    }
}
