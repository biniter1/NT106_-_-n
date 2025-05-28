using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.Models
{
    public class UserStatusData
    {
        public bool isOnline { get; set; }
        public object last_active { get; set; } // Dùng object để có thể gán ServerValue.Timestamp

    }
}
