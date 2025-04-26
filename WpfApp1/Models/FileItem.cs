namespace WpfApp1.Models
{
    public class FileItem
    {
       
        public string IconPathOrType { get; set; }

        public string FileName { get; set; }

        public string FileInfo { get; set; }

        public string FilePathOrUrl { get; set; }

        // Có thể thêm ID
        // public int Id { get; set; }

        public FileItem()
        {
            // Gán giá trị mặc định nếu cần
        }
    }
}