using System;
using System.ComponentModel.DataAnnotations;

namespace Post.Authorization.Domain.Model
{
    public class LoginModel
    {
        [Required]
        public string UserName { get; set; }
        [Required]
        public string Password { get; set; }
    }
}

