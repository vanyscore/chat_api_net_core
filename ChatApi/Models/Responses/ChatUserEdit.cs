namespace ChatApi.Models
{
    public class ChatUserEdit
    {
        public int UserId { get; set; }
        public bool IsRemovable { get; set; }
        public string ImageUrl { get; set; }
        public bool IsAttached { get; set; }
        public string Name { get; set; }
    }
}