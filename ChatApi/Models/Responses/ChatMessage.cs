using System;
using Microsoft.AspNetCore.Http;

namespace ChatApi.Models.Responses
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int ChatId { get; set; }
        public int SenderId { get; set; }
        public DateTime Date { get; set; }
        public string Message { get; set; }
        public string Image { get; set; }

        public static ChatMessage FromDB(ChatApi.EF.Models.ChatMessage m, HttpContext context) => new ChatMessage()
        {
            Id = m.Id,
            ChatId = m.ChatId,
            Date = m.Date,
            Message = m.Message,
            SenderId = m.SenderId,
            Image = m.Image == null ? null : context.Request.Scheme + "://"
                    + context.Request.Host + "/api/attachments/" + m.Image.UUID
        };
    }
}
