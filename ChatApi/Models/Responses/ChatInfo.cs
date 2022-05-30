using ChatApi.EF.Models;
using System.Collections.Generic;

namespace ChatApi.Models
{
    public class ChatInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int OwnerId { get; set; }
        public bool IsPrivate { get; set; }
        public int UnreadMessages { get; set; }

        public ChatApi.Models.Responses.ChatMessage LastMessage { get; set; }
        public List<UserInfo> ChatUsers { get; set; }
        public List<UserInfo> HistoryUsers { get; set; }
    }
}