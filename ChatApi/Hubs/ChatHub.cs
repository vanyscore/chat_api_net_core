using System;
using System.Linq;
using System.Threading.Tasks;
using ChatApi.EF.Models;
using ChatApi.EntityFramework;
using ChatApi.EntityFramework.Models;
using ChatApi.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ChatApi.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {

        private ChatContext _context;
        private MessagesHelper _unreadMessagesHelper;
        
        public ChatHub(ChatContext context, MessagesHelper helper)
        {
            _context = context;
            _unreadMessagesHelper = helper;
        }

        public async Task ConnectToChat(int chatId)
        {
            var userInfo = Context.GetHttpContext().Items["User"] as User;
            
            await Groups.AddToGroupAsync(
                Context.ConnectionId, "chat_" + chatId.ToString()
            );
            
            Console.WriteLine($"User: {Context.ConnectionId} connected");

            if (userInfo != null)
            {
                await Clients.Group("chat_" + chatId.ToString())
                    .SendAsync("Connect", $"{userInfo.Name} Вошёл в чат");
                await Clients.All.SendAsync(
                    "UserConnection", userInfo.Id, true
                );
            }
        }

        public async Task Send(int chatId, string msg)
        {
            if (Context.GetHttpContext().Items["User"] is User userInfo)
            {
                var message = new ChatMessage()
                {
                    Date = DateTime.Now,
                    ChatId = chatId,
                    Message = msg,
                    SenderId = userInfo.Id,     
                };

                await _context.ChatMessages.AddAsync(message);
                await _context.SaveChangesAsync();

                await Clients.Group("chat_" + chatId)
                    .SendAsync("OnMessage", new JsonResult(message));


                Console.WriteLine("Send message from: " + Context.ConnectionId);

                var usersOfChat = _context.ChatUsers
                    .Where((c) => c.ChatRoomId == chatId && !c.IsRemoved)
                    .ToList();

                foreach (var user in usersOfChat)
                {
                    try
                    {
                        var unreadMessages = _unreadMessagesHelper
                            .GetUnreadMessages(user.UserId);
                        await Clients.Group("user_" + user.UserId)
                            .SendAsync("OnUpdateUnreadMessages", unreadMessages, user.Id);

                        Console.WriteLine($"Invoke OnUpdateUnreadMessages for {user.UserId} from {Context.ConnectionId} with {unreadMessages}");
                    } catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
        }

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"User: {Context.ConnectionId} connected");
            
            var userInfo = Context.GetHttpContext()
                .Items["User"] as User;
            
            await Clients.All.SendAsync(
                "UserConnection", userInfo.Id, true
            );

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                "user_" + userInfo.Id);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Console.WriteLine($"User: {Context.ConnectionId} disconnected");
            
            var userInfo = Context.GetHttpContext()
                .Items["User"] as User;
            
            await Clients.All.SendAsync(
                "UserConnection", userInfo.Id, false
            );
            
            await base.OnDisconnectedAsync(exception);
        }
    }
}