using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using WpfApp1.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;

namespace WpfApp1.ViewModels
{
    public partial class ChatViewModel : ObservableObject
    {
        // Collections for data binding
        public ObservableCollection<Message> Messages { get; set; }
        public ObservableCollection<Contact> Contacts { get; set; }
        public ObservableCollection<FileItem> Files { get; set; }

        [ObservableProperty]
        private string _newMessageText; // Đặt attribute trên field private


        [ObservableProperty]
        private Contact _selectedContact; 

        partial void OnSelectedContactChanged(Contact value)
        {
           
            // Gọi phương thức tải tin nhắn tương ứng với contact mới ('value')
            LoadMessagesForContact(value);
            LoadFilesForContact(value);
        }
        // Constructor
        public ChatViewModel()
        {
            Messages = new ObservableCollection<Message>();
            Contacts = new ObservableCollection<Contact>();
            Files = new ObservableCollection<FileItem>();

            SelectedContact = null;
            NewMessageText = string.Empty; // Initialize input property

            // Load sample data
            LoadInitialData();
        }

        // Executes when SendCommand is triggered
        [RelayCommand]
        private void SendMessage()
        {
            if (!string.IsNullOrWhiteSpace(NewMessageText))
            {
                var newMessage = new Message
                {
                    Content = this.NewMessageText,
                    IsMine = true,
                    // Timestamp = DateTime.Now // Uncomment if Message.cs has Timestamp
                };
                Messages.Add(newMessage);
                NewMessageText = string.Empty; // Clear input property
            }
        }

        private void LoadMessagesForContact(Contact contact)
        {
            Messages.Clear(); // Xóa tin nhắn cũ
            if (contact == null) return;

            // Logic tải tin nhắn
            if (contact.Name == "Peter Griffin")
            {
                Messages.Add(new Message { Content = "Hey Lois", IsMine = false });
                Messages.Add(new Message { Content = "Look what I found", IsMine = false });
                Messages.Add(new Message { Content = "Yes, Peter", IsMine = true });
            }
            else if (contact.Name == "Lois Griffin")
            {
                Messages.Add(new Message { Content = "What is it Peter?", IsMine = false });
                Messages.Add(new Message { Content = "Oh nothing.", IsMine = true });
            }
            else if (contact.Name == "Stewie Griffin")
            {
                Messages.Add(new Message { Content = "Silence vile woman!", IsMine = false });
            }
            else if (contact.Name == "Brian Griffin")
            {
                Messages.Add(new Message { Content = "Just reading. You?", IsMine = false });
                Messages.Add(new Message { Content = "I am plotting.", IsMine = true });
            }
        }

        private void LoadFilesForContact(Contact contact)
        {
            Files.Clear(); // Xóa danh sách file cũ

            if (contact == null) return; // Không có contact nào được chọn thì thôi

            if (contact.Name == "Peter Griffin")
            {
                Files.Add(new FileItem { FileName = "XSTK.pdf", IconPathOrType = "pdf", FileInfo = "PDF - 2.1MB", FilePathOrUrl = "dummy" });
                Files.Add(new FileItem { FileName = "baitap.png", IconPathOrType = "png", FileInfo = "PNG - 850KB", FilePathOrUrl = "dummy" });
            }
            else if (contact.Name == "Lois Griffin")
            {
                Files.Add(new FileItem { FileName = "tiendien.xls", IconPathOrType = "xls", FileInfo = "TXT - 5KB", FilePathOrUrl = "dummy" });
            }
            else if (contact.Name == "Stewie Griffin")
            {
                Files.Add(new FileItem { FileName = "LAB01-LTMCB.docx", IconPathOrType = "docx", FileInfo = "DOCX - 128KB", FilePathOrUrl = "dummy" });
                Files.Add(new FileItem { FileName = "multithread.ppt", IconPathOrType = "ppt", FileInfo = "PDF - 5.5MB", FilePathOrUrl = "dummy" });
            }
        }

        // Loads initial sample data
        private void LoadInitialData()
        {
            Contacts.Clear();
            // Sample Contacts
            Contacts.Add(new Contact { Name = "Peter Griffin", AvatarUrl = "placeholder", LastMessage = "Hey Lois", IsOnline = true, LastMessageTime = DateTime.Now.AddMinutes(-5) });
            Contacts.Add(new Contact { Name = "Lois Griffin", AvatarUrl = "placeholder", LastMessage = "What is it Peter?", IsOnline = false, LastMessageTime = DateTime.Now.AddMinutes(-6) });
            Contacts.Add(new Contact { Name = "Stewie Griffin", AvatarUrl = "placeholder", LastMessage = "Victory is mine!", IsOnline = true, LastMessageTime = DateTime.Now.AddHours(-1) });
            if (Contacts.Any())
            {
                SelectedContact= Contacts[0];
            }
            else
            {
                SelectedContact = null;
                Messages.Clear();
                Files.Clear();
            }

        }
    }
}