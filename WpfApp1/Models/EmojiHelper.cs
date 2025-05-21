using System.Collections.Generic;

namespace WpfApp1.Models
{
    public static class EmojiHelper
    {
        // Danh sách emoji phổ biến
        public static List<string> PopularEmojis = new List<string>
        {
            // Mặt cười
            "😀", "😃", "😄", "😁", "😆", "😅", "🤣", "😂", "🙂", "🙃",
            "😉", "😊", "😇", "😍", "🥰", "😘", "😗", "😚", "😙", "😋",
            
            // Cảm xúc
            "😐", "😑", "😶", "🙄", "😏", "😣", "😥", "😮", "🤐", "😯",
            "😪", "😫", "😴", "😌", "😛", "😜", "😝", "🤤", "😒", "😓",
            
            // Biểu tượng tay
            "👍", "👎", "👌", "✌️", "🤞", "🤟", "🤘", "🤙", "👈", "👉",
            "👆", "👇", "☝️", "👋", "🤚", "🖐️", "✋", "🖖", "👏", "🙌",
            
            // Biểu tượng trái tim
            "❤️", "🧡", "💛", "💚", "💙", "💜", "🖤", "💔", "❣️", "💕",
            
            // Biểu tượng khác
            "🎉", "🎊", "🎈", "🎂", "🎁", "🎄", "🎯", "🧩", "🎮", "🎧"
        };

        // Phân loại emoji theo nhóm
        public static Dictionary<string, List<string>> EmojiCategories = new Dictionary<string, List<string>>
        {
            {
                "Mặt cười",
                new List<string> { "😀", "😃", "😄", "😁", "😆", "😅", "🤣", "😂", "🙂", "🙃", "😉", "😊", "😇" }
            },
            {
                "Trái tim",
                new List<string> { "❤️", "🧡", "💛", "💚", "💙", "💜", "🖤", "💔", "❣️", "💕", "💘", "💓" }
            },
            {
                "Tay",
                new List<string> { "👍", "👎", "👌", "✌️", "🤞", "👈", "👉", "👆", "👇", "👋", "👏", "🙌" }
            },
            {
                "Biểu tượng",
                new List<string> { "🎉", "🎊", "🎈", "🎂", "🎁", "🎄", "🎯", "🎮", "📱", "💻", "⌚", "📷" }
            }
        };
    }
}
