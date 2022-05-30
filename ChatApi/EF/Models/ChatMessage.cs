using System;

namespace ChatApi.EF.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int ChatId { get; set; }
        public int SenderId { get; set; }
        public DateTime Date { get; set; }
        public string Message { get; set; }
        public Image Image { get; set; }
    }
}