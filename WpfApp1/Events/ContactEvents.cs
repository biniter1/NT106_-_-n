namespace WpfApp1.Events
{
    public class ContactRemovedEvent
    {
        public string ChatId { get; }
        public string UserEmail { get; }

        public ContactRemovedEvent(string chatId, string userEmail)
        {
            ChatId = chatId;
            UserEmail = userEmail;
        }
    }

    public class GroupDeletedEvent
    {
        public string GroupChatId { get; }
        public DateTime DeletedAt { get; }

        public GroupDeletedEvent(string groupChatId)
        {
            GroupChatId = groupChatId;
            DeletedAt = DateTime.UtcNow;
        }
    }
}