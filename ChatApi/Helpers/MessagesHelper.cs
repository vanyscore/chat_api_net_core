using System;
using System.Collections.Generic;
using System.Linq;
using ChatApi.EF.Models;
using ChatApi.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace ChatApi.Helpers
{
	public class MessagesHelper
	{
        private readonly ChatContext _context;

        public MessagesHelper(ChatContext context)
        {
            _context = context;
        }

		public int GetUnreadMessages(int userId)
        {
            var chatUsers = _context.ChatUsers
                .Include((c) => c.ChatRoom)
                .Where((u) => u.UserId == userId);
            var userChats = chatUsers.Select((c) => c.ChatRoom);

            var chatMessages = new List<ChatMessage>();

            foreach (var chat in userChats)
            {
                chatMessages.AddRange(_context.ChatMessages.Where((m) => m.ChatId == chat.Id));
            }

            var unreadMessages = 0;

            foreach (var userInChat in chatUsers)
            {
                var messages = chatMessages.Where((m) => m.ChatId == userInChat.ChatRoomId);

                unreadMessages += messages.Where((m) => m.Id > userInChat.LastReadMessageId).Count();
            }

            return unreadMessages;
        }
	}
}

