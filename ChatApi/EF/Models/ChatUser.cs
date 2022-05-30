namespace ChatApi.EntityFramework.Models
{
    public class ChatUser
    {
        public int Id { get; set; }
        public int ChatRoomId { get; set; }
        public int UserId { get; set; }
        public bool IsRemoved { get; set; }
        public int LastReadMessageId { get; set; }
        
        public ChatRoom ChatRoom { get; set; }
    }
}