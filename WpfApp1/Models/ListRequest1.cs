using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Firestore;

namespace WpfApp1.Models
{
    [FirestoreData]
    public static class ListRequest1
    {
        [FirestoreProperty]
        static List<FriendRequest> listRequest { get; set; } = new List<FriendRequest>();
    }
}
