using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatApi.EntityFramework;
using ChatApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ChatApi.Middlewares
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _nextMiddleware;
        private readonly Keys _keys;

        public JwtMiddleware(
            RequestDelegate nextMiddleware, IOptions<Keys> keys
        )
        {
            _nextMiddleware = nextMiddleware;
            _keys = keys.Value;
        }

        public async Task Invoke(HttpContext context, ChatContext chatContext)
        {
            Console.WriteLine($"Invoke by: {context.Request.Path}, from {context.Connection.Id}");

            string token = context.Request.Headers["Authorization"]
                .FirstOrDefault()
                ?.Split(" ").Last();

            if (token == null)
            {
                token = context.Request.Query["access_token"];
            }

            if (token != null)
            {
                AttachUserToContext(context, token, chatContext);
            }

            await _nextMiddleware.Invoke(context);
        }

        private void AttachUserToContext(
            HttpContext context, string token,
            ChatContext chatContext
        )
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = _keys.JwtSecret;
                var symmetricKey = new SymmetricSecurityKey(
                    Encoding.ASCII.GetBytes(key)
                );

                tokenHandler.ValidateToken(token, new TokenValidationParameters()
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = symmetricKey,
                    ValidateIssuer = false,
                    ValidateAudience = false
                }, out var validatedToken);

                var jwtToken = validatedToken as JwtSecurityToken;

                var userId = int.Parse(
                    jwtToken.Claims.First(
                        cl => cl.Type == "userId"
                    ).Value
                );
                
                var user = chatContext.Users.ToList()
                    .SingleOrDefault(usr => usr.Id == userId);

                context.Items["User"] = user;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}