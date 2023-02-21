using System;
using Post.Authorization.Domain.Entities;
using Post.Common.DTOs;

namespace Post.Authorization.Api.DTOs
{
    public class AuthenticationResponse : BaseResponse
    {
        public string Result { get; set; }

        public List<PostUser>? PostUsers { get; set; }
    }
}

