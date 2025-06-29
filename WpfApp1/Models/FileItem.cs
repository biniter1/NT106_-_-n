namespace WpfApp1.Models
{
    public class FileItem
    {
        public string IconPathOrType { get; set; }
        public string FileName { get; set; }
        public string FileInfo { get; set; }
        public string FilePathOrUrl { get; set; }
        public string DownloadUrl { get; set; } 
        public string FileExtension { get; set; }
        public bool IsVideo { get; set; }
    }
}