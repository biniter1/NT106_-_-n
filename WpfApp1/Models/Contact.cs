using System;

namespace WpfApp1.Models
{
    public class Contact
    {
        public string Id { get; set; }
        public string AvatarUrl { get; set; }
        public string Name { get; set; }
        public string LastMessage { get; set; }
        public DateTime? LastMessageTime { get; set; }

        // Trạng thái online
        public bool IsOnline { get; set; }    
       
        public Contact()
        {
            IsOnline = false;
        }
    }
}