using System;
using System.IO;
using System.Linq;
using ChatApi.EntityFramework;
using ChatApi.EntityFramework.Models;
using ChatApi.Helpers;
using ChatApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatApi.Controllers
{

    [ApiController]
    [Route("api")]
    public class AvatarsController : ControllerBase
    {
        private ChatContext _context;
        private readonly string _avatarsFolder;

        public AvatarsController(ChatContext context)
        {
            _context = context;
            _avatarsFolder = Environment.CurrentDirectory + "/Images/Avatars";
        }

        [HttpGet("avatars/{id:int}")]
        public IActionResult Get(int id)
        {
            var avatar = _context.Avatars.Find(id);

            var path = @$"{_avatarsFolder}/{avatar.ImagePath}";
            var stream = System.IO.File.OpenRead(path);

            return File(stream, "image/png");
        }

        public static string GetAvatarUrl(HttpRequest request, int id)
        {
            return @$"{request.Scheme}://{request.Host}/api/avatars/{id}";
        }

        [HttpPatch("avatar")]
        [Authorize]
        public ActionResult UploadAvatar(
            [FromQuery] int userId, [FromForm] IFormFile image
        )
        {
            var user = HttpContext.Items["User"] as User;

            if (userId < 7)
            {
                return BadRequest();
            }

            if (user != null)
            {
                var avatar = _context.Avatars.Find(user.AvatarId);

                if (avatar != null)
                {
                    if (avatar.Id == 1)
                    {
                        avatar = new Avatar
                        {
                            ImagePath = $"avatar_{userId + 1}.png"
                        };

                        _context.Avatars.Add(avatar);
                        _context.SaveChanges();
                        
                        avatar = _context.Avatars.Find(user.Id + 1);
                    }
                }

                var fileName = $"{_avatarsFolder}/{avatar.ImagePath}";
                
                var fileStream = System.IO.File.Open(
                    fileName, FileMode.Create
                );

                image.CopyTo(fileStream);
                
                fileStream.Close();

                user.AvatarId = avatar.Id;
                
                _context.SaveChanges();

                return Ok();
            }

            return BadRequest();
        }

    }
}