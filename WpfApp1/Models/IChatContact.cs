namespace WpfApp1.Models // Hoặc WpfApp1.Common
{
    public interface IChatContact
    {
        string Id { get; set; }
        string Name { get; set; }
        string AvatarUrl { get; set; }
    }
}