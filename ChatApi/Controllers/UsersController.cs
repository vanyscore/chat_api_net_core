using System.Collections.Generic;
using System.Linq;
using ChatApi.EntityFramework;
using ChatApi.Helpers;
using ChatApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatApi.Controllers
{
    [ApiController]
    public class UsersController : ControllerBase
    {
        private ChatContext _context;
        
        public UsersController(ChatContext context)
        {
            _context = context;
        }

        [HttpGet("api/users")]
        [Authorize]
        public IEnumerable<UserInfo> Get()
        {
            var users = _context.Users
                .Include(u => u.Avatar)
                .ToList();
            var response = new List<UserInfo>();

            users.ForEach(u =>
            {
                response.Add(new UserInfo()
                {
                    Id = u.Id,
                    Email = u.Email,
                    Name = u.Name,
                    Telephone = u.Telephone,
                    AvatarId = u.AvatarId,
                    ImageUrl = AvatarsController.GetAvatarUrl(
                        Request, u.AvatarId
                    )
                });
            });

            return response;
        }

        [HttpGet("api/user/{userId:int}")]
        public ActionResult<BaseResponse<UserInfo>> GetUser(
            int userId
        )
        {
            var user = _context.Users.Find(userId);

            if (user == null)
            {
                return new BaseResponse<UserInfo>()
                {
                    Error = "Такого пользователя не существует"
                };
            }
            
            return new BaseResponse<UserInfo>()
            {
                Data = new UserInfo()
                {
                    Id = userId,
                    Name = user.Name,
                    Email = user.Email,
                    Telephone = user.Telephone,
                    AvatarId = user.AvatarId,
                    ImageUrl = AvatarsController.GetAvatarUrl(
                        HttpContext.Request, user.AvatarId
                    )
                }
            };
        }
    }
}