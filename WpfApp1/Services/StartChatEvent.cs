using WpfApp1.Models;

namespace WpfApp1.Services
{
    public class StartChatEvent
    {
        // Payload của sự kiện là một đối tượng Contact, vì ChatViewModel làm việc với Contact
        public Contact FriendToChat { get; }

        public StartChatEvent(Contact friendToChat)
        {
            FriendToChat = friendToChat;
        }
    }
}