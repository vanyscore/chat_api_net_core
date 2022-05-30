using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ChatApi.EF.Models;
using ChatApi.EntityFramework.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ChatApi.EntityFramework
{
    public class ChatContext : DbContext
    {   
        public DbSet<User> Users { get; set; }
        public DbSet<UserPassword> UserPasswords { get; set; }
        public DbSet<Avatar> Avatars { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<ChatRoom> ChatRooms { get; set; }
        public DbSet<ChatUser> ChatUsers { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        
        public ChatContext(DbContextOptions<ChatContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var avatars = new List<Avatar>();

            var configuration = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json")
                .Build();
            var avatarsPath = configuration.GetSection("Paths")
                .GetValue<string>("Avatars");
            
            var avatarsDirectory = new DirectoryInfo(avatarsPath);

            for (int i = 0; i < avatarsDirectory.GetFiles().Length; i++)
            {
                avatars.Add(new Avatar()
                {
                    Id = i + 1,
                    ImagePath = $"avatar_{i + 1}.png"
                });
            }

            var users = new List<User>
            {
                new User()
                {
                    Id = 1,
                    Name = "Админ",
                    Email = "admin@this.com",
                    AvatarId = 2,
                    Telephone = "+70000000001"
                },
                new User()
                {
                    Id = 2,
                    Name = "Стэтхэм",
                    Email = "statham@mail.ru",
                    Telephone = "+70000000002",
                    AvatarId = 3
                },
                new User()
                {
                    Id = 3,
                    Name = "Режиссёр",
                    Email = "kino@mail.ru",
                    Telephone = "+70000000003",
                    AvatarId = 4
                },
                new User()
                {
                    Id = 4,
                    Name = "Боец",
                    Email = "makhregor@mail.ru",
                    Telephone = "+70000000004",
                    AvatarId = 5
                },
                new User()
                {
                    Id = 5,
                    Name = "Киану Ривз",
                    Email = "jon_silverhand@mail.ru",
                    Telephone = "+70000000005",
                    AvatarId = 6
                },
                new User()
                {
                    Id = 6,
                    Name = "Оби Ван",
                    Email = "jedi@republic.com",
                    Telephone = "+70000000006",
                    AvatarId = 7
                }
            };

            modelBuilder.Entity<Avatar>()
                .HasData(avatars);
            modelBuilder.Entity<User>()
                .HasData(
                    users
                );

            var defaultPassword = "12345678";

            var passwords = new List<UserPassword>();
            var index = 1;
            
            using (var alg = SHA256.Create())
            {
                users.ForEach(u =>
                {
                    var hash = alg.ComputeHash(
                        Encoding.ASCII.GetBytes(defaultPassword)
                    );
                    var hashString = new StringBuilder();
                
                    foreach (var b in hash)
                    {
                        hashString.Append(b.ToString());
                    }

                    passwords.Add(new UserPassword()
                    {
                        Id = index,
                        UserId = index,
                        PasswordHash = hashString.ToString()
                    });

                    index++;
                });
            }
            
            modelBuilder.Entity<UserPassword>()
                .HasData(passwords);

            modelBuilder.Entity<ChatRoom>()
                .HasData(new ChatRoom()
                {
                    Id = 1,
                    Name = "Общий чат",
                    OwnerId = 1
                });

            index = 1;
            users.ForEach(u =>
            {
                modelBuilder.Entity<ChatUser>()
                    .HasData(new ChatUser()
                    {
                        Id = index,
                        ChatRoomId = 1,
                        UserId = u.Id,
                        LastReadMessageId = 0
                    });
                
                index++;
            });

            modelBuilder.Entity<ChatMessage>()
                .HasData(new ChatMessage()
                {
                    Id = 1,
                    ChatId = 1,
                    Date = DateTime.Now,
                    SenderId = 1,
                    Message = "Здарова огалый"
                }, new ChatMessage()
                {
                    Id = 2,
                    ChatId = 1,
                    Date = DateTime.Now,
                    SenderId = 2,
                    Message = "Здарова - здарова молодой"
                }, new ChatMessage()
                {
                    Id = 3,
                    ChatId = 1,
                    Date = DateTime.Now,
                    SenderId = 3,
                    Message = "Здарова пацаны"
                });

            base.OnModelCreating(modelBuilder);
        }
    }
}
