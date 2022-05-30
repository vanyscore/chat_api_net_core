namespace ChatApi.EntityFramework.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Telephone { get; set; }
        public int AvatarId { get; set; }
        public Avatar Avatar { get; set; }
    }
}