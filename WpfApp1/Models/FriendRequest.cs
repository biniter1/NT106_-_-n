using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Firestore;


namespace WpfApp1.Models // Đảm bảo namespace là thư mục Models của bạn
{
    /// <summary>
    /// Lớp Model đại diện cho dữ liệu của một lời mời kết bạn.
    /// Chỉ chứa các thuộc tính dữ liệu thuần túy.
    /// </summary>
    /// 
    [FirestoreData]
    public class FriendRequest
    {
        [FirestoreProperty]
        public string EmailRequestId { get; set; }

        [FirestoreProperty]
        public string EmailRequesterId { get; set; }

        [FirestoreProperty]
        public string RequesterName { get; set; }

        [FirestoreProperty]
        public string RequesterAvatarUrl { get; set; }

        [FirestoreProperty]
        public DateTime RequestTime { get; set; }

        public FriendRequest()
        {
            RequestTime = DateTime.UtcNow;
        }
    }
}