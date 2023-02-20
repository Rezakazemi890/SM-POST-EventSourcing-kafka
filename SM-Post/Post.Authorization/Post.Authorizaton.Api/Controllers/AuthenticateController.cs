using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Post.Authorization.Api.DTOs;
using Post.Authorization.Domain.Entities;
using Post.Authorization.Domain.Model;
using Post.Authorization.Domain.Repositories;
using Post.Authorization.Infrastructure.Base;
using Post.Authorization.Infrastructure.DataAccess;
using Post.Common.DTOs;
using Post.Common.Utils;

namespace Post.Authorization.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[Controller]")]
    public class AuthenticateController : BaseController
    {
        private readonly ILogger<AuthenticateController> _logger;
        private IConfiguration _config;
        private readonly IPostUserRepository _postUserRepository;

        public AuthenticateController(ILogger<AuthenticateController> logger, IConfiguration config, IPostUserRepository postUserRepository)
        {
            _logger = logger;
            _config = config;
            _postUserRepository = postUserRepository;
        }

        private string GenerateJSONWebToken(LoginModel userInfo)
        {
            if (AuthenticateUser(userInfo))
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_config.GetSection("Jwt").GetRequiredSection("Key").Value);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new System.Security.Claims.ClaimsIdentity(
                        new Claim[]{
                            new Claim(ClaimTypes.Name,userInfo.UserName)
                        }
                    ),
                    Expires = DateTime.UtcNow.AddDays(1),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                return tokenString;
            }
            else
            {
                return string.Empty;
            }
        }

        private bool AuthenticateUser(LoginModel login)
        {
            bool result = false;

            //Validate the User Credentials      
            //Demo Purpose, I have Passed HardCoded User Information      
            if (login.UserName == "reza")
            {
                result = true;
            }


            return result;
        }


        [AllowAnonymous]
        [HttpPost(nameof(Login))]
        public async Task<IActionResult> Login([FromBody] LoginModel data)
        {
            IActionResult response = Unauthorized();
            if (data != null)
            {
                string tokenString = GenerateJSONWebToken(data);
                if (!string.IsNullOrEmpty(tokenString))
                    response = Ok(new { Token = tokenString, Message = "Success Authentication" });
            }
            return response;
        }


        [HttpGet(nameof(Get))]
        public async Task<IEnumerable<string>> Get()
        {
            var accessToken = await HttpContext.GetTokenAsync("access_token");

            return new string[] { accessToken };
        }

        [AllowAnonymous]
        [HttpPost(nameof(RegisterUser))]
        public async Task<ActionResult> RegisterUser([FromBody] LoginModel data)
        {
            try
            {
                var newUser = new PostUser
                {
                    UserName = data.UserName,
                    PasswordHash = SecurePasswordHasher.Hash(data.Password),
                };

                await _postUserRepository.CreateUserAsync(newUser);

                return NormalResponse("Register User");
            }
            catch (Exception ex)
            {
                const string SAFE_ERROR_MESSAGE = "Error while processing register user !";
                return ErrorResponse(ex, SAFE_ERROR_MESSAGE);
            }
        }

        private ActionResult NormalResponse(string operation)
        {
            if (operation == null || string.IsNullOrEmpty(operation))
            {
                return NoContent();
            }
            return Ok(new AuthenticationResponse

            {
                Message = $"{operation} Successfully Done!"
            });
        }

        private ActionResult ErrorResponse(Exception ex, string SAFE_ERROR_MESSAGE)
        {
            _logger.LogError(ex, SAFE_ERROR_MESSAGE);
            return StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse
            {
                Message = SAFE_ERROR_MESSAGE
            });
        }
    }
}

