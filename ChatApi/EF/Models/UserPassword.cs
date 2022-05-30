namespace ChatApi.EntityFramework.Models
{
    public class UserPassword
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string PasswordHash { get; set; }
    }
}