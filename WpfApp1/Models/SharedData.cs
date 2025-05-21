using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using WpfApp1.Models;

namespace WpfApp1.Models
{
    public class SharedData
    {
        public static SharedData Instance { get; } = new SharedData();
        public User userdata {  get; set; }
        public static List<FriendRequest> friendRequests { get; set; }=new List<FriendRequest>();
        
        public SharedData() {
        userdata = new User();

        }
    }
}
