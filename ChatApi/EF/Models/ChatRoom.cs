using System.Collections.Generic;

namespace ChatApi.EntityFramework.Models
{
    public class ChatRoom
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int OwnerId { get; set; }
    }
}