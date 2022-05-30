using System;
using System.IO;
using System.Linq;
using ChatApi.EF.Models;
using ChatApi.EntityFramework;
using ChatApi.EntityFramework.Models;
using ChatApi.Helpers;
using ChatApi.Hubs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ChatApi.Controllers
{
    [Authorize]
    [Route("api")]
    public class ImageController : ControllerBase
    {
        private ChatContext _context;
        private IHubContext<ChatHub> _hubContext;
        private MessagesHelper _messagesHelper;

        public ImageController(ChatContext context, IHubContext<ChatHub> hubContext, MessagesHelper helper)
        {
            _hubContext = hubContext;
            _context = context;
            _messagesHelper = helper;
        }

        [HttpPost("chat/{chatId:int}/attachments/image")]
        public ActionResult<string> SendImageToChat(int chatId, IFormFile file)
        {
            var user = HttpContext.Items["User"] as User;

            var uuid = Guid.NewGuid().ToString();
            var path = "./Images/Attachments/" + uuid  + ".png";
            using var stream = new FileStream(path, FileMode.Create);

            file.CopyTo(stream);

            var image = new Image()
            {
                UUID = uuid
            };
            _context.Images.Add(image);
            _context.SaveChanges();

            var chat = _context.ChatRooms.SingleOrDefault((ch) => ch.Id == chatId);

            var message = new ChatMessage()
            {
                ChatId = chat.Id,
                SenderId = user.Id,
                Date = DateTime.Now,
                Image = image,
            };
            _context.ChatMessages.Add(message);
            _context.SaveChanges();

            _hubContext.Clients.Group("chat_" + message.ChatId).SendAsync("OnMessage",
                new JsonResult(
                    ChatApi.Models.Responses.ChatMessage.FromDB(message, HttpContext)
            ));
            
            var usersOfChat = _context.ChatUsers
                    .Where((c) => c.ChatRoomId == chatId && !c.IsRemoved)
                    .ToList();

            foreach (var userOfChat in usersOfChat)
            {
                _hubContext.Clients.Group("user_" + userOfChat.UserId)
                    .SendAsync("OnUpdateUnreadMessages", _messagesHelper
                    .GetUnreadMessages(userOfChat.UserId), userOfChat.UserId);
            }

            return uuid;
        }

        [HttpGet("attachments/{id}")]
        public IActionResult Get(string id)
        {
            var image = _context.Images.SingleOrDefault((im) => im.UUID == id);

            if (image != null)
            {
                var path = "./Images/Attachments/" + id + ".png";

                var stream = System.IO.File.OpenRead(path);

                return File(stream, "image/png");
            }
            else
            {
                Response.StatusCode = 404;

                return null;
            }
        }
    }
}
