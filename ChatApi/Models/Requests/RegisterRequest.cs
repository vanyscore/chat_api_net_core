using System.ComponentModel.DataAnnotations;

namespace ChatApi.Models.Requests
{
    public class RegisterRequest
    {
        [StringLength(30, MinimumLength = 1)]
        public string Name { get; set; }
        [EmailAddress]
        public string Email { get; set; }
        [Phone]
        public string Telephone { get; set; }
        [StringLength(30, MinimumLength = 8)]
        public string Password { get; set; }
    }
}