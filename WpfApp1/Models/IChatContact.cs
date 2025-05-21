namespace WpfApp1.Models // Hoặc WpfApp1.Common
{
    public interface IChatContact
    {
        string Email { get; set; }
        string Name { get; set; }
        string AvatarUrl { get; set; }
    }
}