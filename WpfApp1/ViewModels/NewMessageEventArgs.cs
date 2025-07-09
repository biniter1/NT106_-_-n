using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.ViewModels
{
    public class NewMessageEventArgs : EventArgs
    {
        public string Title { get; }
        public string Message { get; }
        public string ChatID { get; }

        public NewMessageEventArgs(string title, string message, string chatID)
        {
            Title = title;
            Message = message;
            ChatID = chatID;
        }
    }
}
