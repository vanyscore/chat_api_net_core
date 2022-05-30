using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ChatApi.EntityFramework;
using ChatApi.EntityFramework.Models;
using ChatApi.Models;
using ChatApi.Models.Requests;
using ChatApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace ChatApi.Controllers
{
    [ApiController]
    [Route("api")]
    // [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {

        private ChatContext _context;
        private readonly AuthService _authService;

        public AuthController(
            ChatContext context,
            AuthService authService
        )
        {
            _context = context;
            _authService = authService;
        }
        
        [HttpPost("auth")]
        public ActionResult<BaseResponse<AuthResult>> Login(
            [FromBody] AuthRequest data
        )
        {
            var result = _authService.GetAuthResult(data.Login, data.Password);
            var response = new BaseResponse<AuthResult>();

            response.Error = result.Value;

            if (response.Error == null)
            {
                response.Data = result.Key;
            }

            return response;
        }

        [HttpPost("register")]
        public ActionResult<BaseResponse<AuthResult>> Register(
            [FromBody] RegisterRequest request
        )
        {
            var requestType = typeof(RegisterRequest);

            var nameAttr = (StringLengthAttribute) requestType
                .GetProperty("Name")
                ?.GetCustomAttribute(typeof(StringLengthAttribute))!;
            var emailAttr = (EmailAddressAttribute) requestType
                .GetProperty("Email")
                ?.GetCustomAttribute(typeof(EmailAddressAttribute))!;
            var telephoneAttr = (PhoneAttribute) requestType
                .GetProperty("Telephone")
                ?.GetCustomAttribute(typeof(PhoneAttribute))!;
            var passwordAttr = (StringLengthAttribute) request.GetType()
                .GetProperty("Password")
                ?.GetCustomAttribute(typeof(StringLengthAttribute))!;

            var validations = new Dictionary<string, string>();

            if (!nameAttr.IsValid(request.Name))
            {
                validations["name"] = "Имя не должно быть пустым";
            }

            if (!emailAttr.IsValid(request.Email))
            {
                validations["email"] = "Некорректный формат электронной почты";
            }

            if (!telephoneAttr.IsValid(request.Telephone))
            {
                validations["phone"] = "Некорректный формат телефонного номера";
            }

            if (!passwordAttr.IsValid(request.Password))
            {
                validations["password"] =
                    $"Длина пароля должна составлять" +
                    $" {passwordAttr.MinimumLength} -> {passwordAttr.MaximumLength}";
            }

            var response = new BaseResponse<AuthResult>();

            if (validations.Count > 0)
            {
                response.Error = "Ошибка валидации";
                response.Validations = validations;
            }
            else
            {
                var result = _authService.Register(
                    request
                );

                var commonRoom = _context.ChatRooms.Find(1);

                _context.ChatUsers.Add(
                    new ChatUser()
                    {
                        ChatRoomId = commonRoom.Id,
                        UserId = result.UserId,
                    }
                );
                _context.SaveChanges();

                response.Data = result;
            }

            return response;
        }
    }
}