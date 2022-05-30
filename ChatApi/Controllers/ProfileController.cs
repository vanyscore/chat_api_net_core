using ChatApi.EntityFramework;
using ChatApi.EntityFramework.Models;
using ChatApi.Helpers;
using ChatApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChatApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private ChatContext _context;
        
        public ProfileController(ChatContext context)
        {
            _context = context;
        }
        
        [Authorize]
        public ActionResult<BaseResponse<UserInfo>> Get()
        {
            var user = (User) HttpContext.Items["User"];
            var userInfo = new UserInfo
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                AvatarId = user.AvatarId,
                ImageUrl = AvatarsController
                    .GetAvatarUrl(HttpContext.Request, user.AvatarId),
                Telephone = user.Telephone

            };
            var response = new BaseResponse<UserInfo>
            {
                Data = userInfo,
                Error = null
            };
            
            return response;
        }
    }
}