using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ChatApi.EF.Models;
using ChatApi.EntityFramework;
using ChatApi.EntityFramework.Models;
using ChatApi.Helpers;
using ChatApi.Hubs;
using ChatApi.Models;
using ChatApi.Models.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ChatApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api")]
    public class ChatController : ControllerBase
    {

        private ChatContext _context;
        private IHubContext<ChatHub> _hubContext;
        private MessagesHelper _unreadMessagesHelper;

        public ChatController(ChatContext context, IHubContext<ChatHub> hubContext, MessagesHelper helper)
        {
            _context = context;
            _hubContext = hubContext;
            _unreadMessagesHelper = helper;
        }

        [HttpGet("chat/{chatId:int}")]
        public ActionResult<ChatInfo> GetChatRoom(int chatId)
        {

            return GetChatInfo(chatId);
        }

        [HttpGet("chats")]
        public ActionResult<List<ChatInfo>> GetChats()
        {
            var user = HttpContext.Items["User"] as User;
            var result = _context.ChatUsers.ToList()
                .Where(usr => usr.UserId == user.Id && !usr.IsRemoved)
                .Select(usr => _context.ChatRooms.Find(usr.ChatRoomId))
                .Select(ch => GetChatInfo(ch.Id)).ToList();

            var common = result.Where((c) => !c.IsPrivate).ToList();
            var privatChats = result.Where((c) => c.IsPrivate).ToList();

            common.Sort((c1, c2) => c1.UnreadMessages > c2.UnreadMessages ? -1 : 1);
            privatChats.Sort((c1, c2) => c1.UnreadMessages > c2.UnreadMessages ? -1 : 1);

            result.Clear();

            result.AddRange(common);
            result.AddRange(privatChats);
                 
            return result;
        }

        [HttpGet("chat/{chatId:int}/messages")]
        public ActionResult<BaseResponse<List<ChatApi.Models.Responses.ChatMessage>>> GetChatMessages(
            int chatId
        )
        {
            var messages = _context.ChatMessages.Include((m) => m.Image).ToList()
                .Where(
                    (ch, i) => ch.ChatId == chatId
                ).ToList().Select((m) => ChatApi.Models.Responses.ChatMessage.FromDB(m, HttpContext)).ToList();

            return new BaseResponse<List<ChatApi.Models.Responses.ChatMessage>>
            {
                Data = messages
            };
        }

        private ChatInfo GetChatInfo(int chatId)
        {
            var chat = _context.ChatRooms.Find(chatId);
            var chatUsers = _context.ChatUsers.ToList()
                .Where(ch => ch.ChatRoomId == chatId)
                .ToList();
            var chatMessages = _context.ChatMessages.Where((m) => m.ChatId == chatId);

            var users = new List<UserInfo>();

            chatUsers.ForEach(usr =>
            {
                var user = _context.Users.Find(usr.UserId);

                users.Add(new UserInfo()
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Telephone = user.Telephone,
                    AvatarId = user.Id,
                    ImageUrl = AvatarsController.GetAvatarUrl(
                        HttpContext.Request, user.AvatarId
                    )
                });
            });

            var messages = _context.ChatMessages
                .Include((m) => m.Image)
                .Where((m) => m.ChatId == chatId)
                .Select((m) => Models.Responses.ChatMessage.FromDB(m, HttpContext)).ToList();

            var userId = ((User)HttpContext.Items["User"]).Id;
            var chatUserInfo = chatUsers
                .SingleOrDefault((u) => u.ChatRoomId == chatId && u.UserId == userId);

            return new ChatInfo()
            {
                Id = chatId,
                OwnerId = chat.OwnerId,
                Name = chat.Name,
                IsPrivate = chat.IsPrivate,
                LastMessage = messages.LastOrDefault(),
                UnreadMessages = messages.Where((m) => m.Id > chatUserInfo.LastReadMessageId).Count(),
                ChatUsers = users.Where(
                    usr => chatUsers.SingleOrDefault(
                        chUsr => chUsr.UserId == usr.Id && !chUsr.IsRemoved
                    ) != null
                ).OrderBy(
                    (u) => u.Id == 1 ? int.MaxValue : chatMessages
                    .Where((m) => m.SenderId == u.Id)
                    .Count()
                )
                .Reverse().ToList(),
                HistoryUsers = users.Where(
                    usr => chatUsers.SingleOrDefault(
                        chUsr => chUsr.UserId == usr.Id && chUsr.IsRemoved
                    ) != null
                ).ToList()
            };
        }

        [HttpPut("chat/create")]
        public ActionResult<BaseResponse<object>> CreateChatRoom(
            [FromBody] ChangeNameRequest request
        )
        {
            var user = HttpContext.Items["User"] as User;

            if (user == null) return Unauthorized();

            if (request.Name == null)
            {
                return new BaseResponse<object>()
                {
                    Error = "Имя чата не должно быть пустым"
                };
            }

            var chat = new ChatRoom()
            {
                Name = request.Name,
                OwnerId = user.Id
            };

            _context.ChatRooms.Add(chat);
            _context.SaveChanges();

            var createdChat = _context.ChatRooms.ToList().LastOrDefault(
                ch => ch.OwnerId == user.Id
            );

            var admin = _context.Users.Find(1);

            if (createdChat == null) return Unauthorized();

            var chatUsers = new List<ChatUser>
            {
                new ChatUser
                {
                    ChatRoomId = createdChat.Id,
                    UserId = admin.Id
                }
            };

            if (user.Id != 1)
            {
                chatUsers.Add(new ChatUser
                {
                    ChatRoomId = createdChat.Id,
                    UserId = user.Id
                });
            }

            _context.ChatUsers.AddRange(chatUsers);
            _context.SaveChanges();

            return Ok();

        }

        [HttpGet("chat/{chatId:int}/edit/users")]
        public BaseResponse<List<ChatUserEdit>> GetEditableChatUsers(
            int chatId
        )
        {
            var chatUsers = _context.ChatUsers.Where(
                chUsr => chUsr.ChatRoomId == chatId
            ).ToList();
            var users = _context.Users.ToList().Where(
                usr => chatUsers.SingleOrDefault(
                    chUsr => chUsr.UserId == usr.Id
                ) == null
            ).ToList();

            users.AddRange(chatUsers.Select(chUsr =>
                _context.Users.Find(chUsr.UserId)
            ));

            var resultUsers = new List<ChatUserEdit>();

            foreach (var usr in users)
            {
                var chatUser = chatUsers.SingleOrDefault(
                    chUsr => chUsr.UserId == usr.Id
                );
                bool isAttached = chatUser != null && !chatUser.IsRemoved;

                resultUsers.Add(
                    new ChatUserEdit()
                    {
                        UserId = usr.Id,
                        Name = usr.Name,
                        ImageUrl = AvatarsController.GetAvatarUrl(
                            HttpContext.Request, usr.AvatarId
                        ),
                        IsAttached = isAttached,
                        IsRemovable = usr.Id != 1 && isAttached
                    }
                );
            }

            return new BaseResponse<List<ChatUserEdit>>
            {
                Data = resultUsers
            };
        }

        [HttpPatch("chat/{chatId:int}/edit/user")]
        public ActionResult<BaseResponse<object>> UpdateChatUser(
            int chatId, [FromQuery] int userId, [FromQuery] bool isAttach
        )
        {
            if (userId == 1)
            {
                return new BaseResponse<object>()
                {
                    Error = "Вы не можете удалить админа из чата"
                };
            }

            var chat = _context.ChatRooms.Find(chatId);
            var user = HttpContext.Items["User"] as User;

            if (chat.OwnerId != user.Id)
            {
                return new BaseResponse<object>()
                {
                    Error = "У вас нет прав на редактирование данного чата"
                };
            }

            var chatUsers = _context.ChatUsers.ToList()
                .Where(chUsr => chUsr.ChatRoomId == chatId);
            var chatUser = chatUsers.SingleOrDefault(chUsr =>
                chUsr.UserId == userId
                && chUsr.ChatRoomId == chatId);

            if (isAttach)
            {
                if (chatUser != null)
                {
                    if (chatUser.IsRemoved)
                    {
                        chatUser.IsRemoved = false;

                        _context.ChatUsers.Update(chatUser);
                        _context.SaveChanges();

                        return Ok();
                    }

                    return new BaseResponse<object>()
                    {
                        Error = "Пользователь уже является участником чата"
                    };
                }

                _context.ChatUsers.Add(
                    new ChatUser()
                    {
                        ChatRoomId = chatId,
                        UserId = userId,
                        LastReadMessageId = 0
                    }
                );
                _context.SaveChanges();

                return Ok();
            }

            if (chatUser != null)
            {
                if (!chatUser.IsRemoved)
                {
                    chatUser.IsRemoved = true;

                    _context.ChatUsers.Update(chatUser);
                    _context.SaveChanges();

                    return Ok();
                }

                return new BaseResponse<object>()
                {
                    Error = "Пользователь не является участником чата"
                };
            }

            return new BaseResponse<object>()
            {
                Error = "Нельзя удалить пользователя, которого не было в чате"
            };
        }


        [HttpPatch("chat/{chatId:int}/edit")]
        public ActionResult<BaseResponse<object>> UpdateChatName(
            int chatId, [FromBody] ChangeNameRequest request
        )
        {
            var chatName = request.Name;

            if (string.IsNullOrEmpty(chatName))
            {
                return new BaseResponse<object>()
                {
                    Error = "Имя чата не должно быть пустым"
                };
            }

            var chat = _context.ChatRooms.Find(chatId);

            if (chat.Name == chatName)
            {
                return new BaseResponse<object>()
                {
                    Error = "Ошибка: (Имя повторяется)"
                };
            }

            chat.Name = chatName;

            _context.ChatRooms.Update(chat);
            _context.SaveChanges();

            return Ok();
        }

        [HttpPatch("chat/{chatId:int}/read/{messageId:int}")]
        public ActionResult<object> ReadMessage(int chatId, int messageId)
        {
            var userId = ((User)HttpContext.Items["User"]).Id;

            var chatUser = _context.ChatUsers
                .SingleOrDefault((r) => r.ChatRoomId == chatId && r.UserId == userId);

            chatUser.LastReadMessageId = messageId;

            _context.SaveChanges();

            _hubContext.Clients.Group("user_" + userId.ToString())
                .SendAsync("OnUpdateUnreadMessages", _unreadMessagesHelper
                .GetUnreadMessages(userId));

            return null;
        }

        [HttpGet("chat/unreadMessages")]
        public ActionResult<int> UnreadMessages()
        {
            var userId = ((User)HttpContext.Items["User"]).Id;

            return _unreadMessagesHelper.GetUnreadMessages(userId);
        }

        [HttpPost("chat/private/create/{userId:int}")]
        public ActionResult<int> CreatePrivateChat(int userId)
        {
            var userFrom = (User) HttpContext.Items["User"];
            var userTo = _context.Users.SingleOrDefault((u) => u.Id == userId);
            var chats = _context.ChatRooms;

            ChatRoom chatRoom = null;

            foreach (var chat in chats)
            {
                var usersOfChat = _context.ChatUsers.Where((c) => c.ChatRoomId == chat.Id);

                if (usersOfChat.Count() == 2)
                {
                    var userFromContains = usersOfChat.SingleOrDefault((u) => u.UserId == userFrom.Id) != null;
                    var userToContains = usersOfChat.SingleOrDefault((u) => u.UserId == userTo.Id) != null;

                    if (userFromContains && userToContains)
                    {
                        chatRoom = chat;
                        break;
                    }
                }
            }

            if (chatRoom != null)
            {
                return chatRoom.Id;
            }
            else
            {
                chatRoom = new ChatRoom()
                {
                    IsPrivate = true,
                    Name = null,
                    OwnerId = userFrom.Id
                };
                _context.ChatRooms.Add(chatRoom);
                _context.SaveChanges();

                _context.ChatUsers.Add(new ChatUser()
                {
                    ChatRoomId = chatRoom.Id,
                    LastReadMessageId = 0,
                    IsRemoved = false,
                    UserId = userFrom.Id
                });

                _context.ChatUsers.Add(new ChatUser()
                {
                    ChatRoomId = chatRoom.Id,
                    LastReadMessageId = 0,
                    IsRemoved = false,
                    UserId = userTo.Id
                });

                _context.SaveChanges();

                return chatRoom.Id;
            }
        }
    }
}