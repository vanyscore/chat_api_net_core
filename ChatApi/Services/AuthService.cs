using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ChatApi.EntityFramework;
using ChatApi.EntityFramework.Models;
using ChatApi.Models;
using ChatApi.Models.Requests;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ChatApi.Services
{
    public class AuthService
    {
        private readonly ChatContext _context;
        private readonly Keys _keys;

        public AuthService(ChatContext context, IOptions<Keys> keys)
        {
            _context = context;
            _keys = keys.Value;
        }
        
        public KeyValuePair<AuthResult, string> GetAuthResult(
            string login, string password
        )
        {
            var result = new AuthResult()
            {
                Token = null,
                UserId = -1
            };
            string errorMessage = null;
            
            using (var alg = SHA256.Create())
            {
                var user = _context.Users.FirstOrDefault(
                    usr => usr.Email == login || usr.Telephone == login
                );
                var hashString = new StringBuilder();
                var passwordHash = alg.ComputeHash(
                    Encoding.ASCII.GetBytes(password)
                );
                
                foreach (var b in passwordHash)
                {
                    hashString.Append(b);
                }
                
                if (user != null)
                {
                    var userId = user.Id;
                
                    var passwordWrite = _context.UserPasswords
                        .FirstOrDefault(
                            psw => psw.UserId == userId
                                   && psw.PasswordHash == hashString.ToString()
                        );

                    if (passwordWrite != null)
                    {
                        result.Token = GenerateJwtToken(passwordWrite.UserId);
                        result.UserId = passwordWrite.UserId;
                    }
                    else
                    {
                        errorMessage = "Неправильный логин и/или пароль";
                    }                
                }
                else
                {
                    errorMessage = "Данного пользователя не существует";
                }                
            }

            return new KeyValuePair<AuthResult, string>(
                result, errorMessage
            );
        }

        public AuthResult Register(
            RegisterRequest model
        )
        {
            using (var alg = SHA256.Create())
            {
                var pswString = new StringBuilder();
                var pswHash = alg.ComputeHash(
                    Encoding.ASCII.GetBytes(model.Password)
                );
                var lastUser = _context.Users.ToList().Last();

                foreach (var b in pswHash)
                {
                    pswString.Append(b.ToString());
                }

                var user = new User
                {
                    Name = model.Name,
                    Email = model.Email,
                    Telephone = model.Telephone,
                    AvatarId = 1
                };

                var passwordWrite = new UserPassword()
                {
                    UserId = lastUser.Id + 1,
                    PasswordHash = pswString.ToString()
                };

                _context.Users.Add(user);
                _context.UserPasswords.Add(passwordWrite);

                _context.SaveChanges();
            }

            var resultUser = _context.Users.ToList().Last();
            var token = GenerateJwtToken(resultUser.Id);
            var authResult = new AuthResult()
            {
                UserId = resultUser.Id,
                Token = token
            };

            return authResult;
        }

        private string GenerateJwtToken(int userId)
        {
            var secretKey = _keys.JwtSecret;
            var symmetricKey = new SymmetricSecurityKey(
                Encoding.ASCII.GetBytes(secretKey)
            );

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("userId", userId.ToString())
                }),
                Expires = DateTime.Now.AddDays(30),
                SigningCredentials = new SigningCredentials(
                    symmetricKey, SecurityAlgorithms.HmacSha256Signature
                )
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}