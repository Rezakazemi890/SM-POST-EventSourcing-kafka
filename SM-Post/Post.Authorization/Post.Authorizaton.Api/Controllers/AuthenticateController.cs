using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DnsClient;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
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

        private async Task<string> GenerateJSONWebToken(LoginModel userInfo)
        {
            if (await AuthenticateUser(userInfo))
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
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                    Issuer = _config.GetSection("Jwt").GetRequiredSection("Issuer").Value,
                    Audience = _config.GetSection("Jwt").GetRequiredSection("Issuer").Value
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

        private async Task<bool> AuthenticateUser(LoginModel login)
        {
            bool result = false;
            var user = await _postUserRepository.GetUserByUserName(login.UserName);
            if (user == null || !SecurePasswordHasher.Verify(login.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("User or password is incorrect!");
            else
                result = true;

            return result;
        }


        [AllowAnonymous]
        [HttpPost(nameof(Login))]
        public async Task<IActionResult> Login([FromBody] LoginModel data)
        {
            try
            {
                IActionResult response = Unauthorized();
                if (data != null)
                {
                    string tokenString = await GenerateJSONWebToken(data);
                    
                    if (!string.IsNullOrEmpty(tokenString))
                        response = Ok(new { Token = tokenString, Message = "Success Authentication" });
                }
                return response;
            }
            catch (Exception ex)
            {
                const string SAFE_ERROR_MESSAGE = "Username or Password is incorrect! please check your data.";
                return ErrorResponse(ex, SAFE_ERROR_MESSAGE);
            }
        }

        //[HttpGet(nameof(Get))]
        //public async Task<IEnumerable<string>> Get()
        //{
        //    var accessToken = await HttpContext.GetTokenAsync("access_token");

        //    return new string[] { accessToken };
        //}

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

                return NormalResponse("Register User Action", newUser.Id.ToString());
            }
            catch (Exception ex)
            {
                const string SAFE_ERROR_MESSAGE = "Error while processing register user! Check Data information.";
                return ErrorResponse(ex, SAFE_ERROR_MESSAGE);
            }
        }

        [HttpGet(nameof(GetListOfUsers))]
        public async Task<ActionResult> GetListOfUsers()
        {
            try
            {
                var res = await _postUserRepository.GetlistOfUsers();
                res.ForEach(x => x.PasswordHash = "Protected");

                return NormalResponse("Get All Users", $"Returned {res.Count()} Users.", res);
            }
            catch (Exception ex)
            {
                const string SAFE_ERROR_MESSAGE = "Error while processing get users! Contact admin.";
                return ErrorResponse(ex, SAFE_ERROR_MESSAGE);
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateUser(Guid id, [FromBody] LoginModel data, string oldPassword)
        {
            try
            {
                var user = await _postUserRepository.GetUserByUserId(id.ToString());
                if (user == null)
                    throw new InvalidDataException("User Not found!");

                if (data.UserName != user.UserName && await _postUserRepository.GetUserByUserName(data.UserName) != null)
                    throw new InvalidDataException("New Username Is Exists!");

                if (!SecurePasswordHasher.Verify(oldPassword, user.PasswordHash))
                    throw new InvalidDataException("Username or password is incorrect!");

                user.UserName = data.UserName;
                user.PasswordHash = SecurePasswordHasher.Hash(data.Password);

                await _postUserRepository.UpdateUserAsync(user);

                return NormalResponse("Update User Data", $"User Updated.");
            }
            catch (Exception ex)
            {
                const string SAFE_ERROR_MESSAGE = "Error while processing Update user! Contact admin.";
                return ErrorResponse(ex, SAFE_ERROR_MESSAGE);
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> RemoveUser(Guid id)
        {
            try
            {
                var user = await _postUserRepository.GetUserByUserId(id.ToString());
                if (user == null)
                    throw new InvalidDataException("User Not found!");

                await _postUserRepository.DeleteUserAsync(id.ToString());

                return NormalResponse("Remove User", $"User Removed.");
            }
            catch (Exception ex)
            {
                const string SAFE_ERROR_MESSAGE = "Error while processing Remove user! Contact admin.";
                return ErrorResponse(ex, SAFE_ERROR_MESSAGE);
            }
        }

        private ActionResult NormalResponse(string operation, string result)
        {
            if (operation == null || string.IsNullOrEmpty(operation))
            {
                return NoContent();
            }
            return Ok(new AuthenticationResponse

            {
                Result = result,
                Message = $"{operation} Successfully Done!"
            });
        }

        private ActionResult NormalResponse(string operation, string result, List<PostUser>? postUsers)
        {
            if (postUsers == null || !postUsers.Any())
            {
                return NoContent();
            }
            return Ok(new AuthenticationResponse

            {
                Result = result,
                PostUsers = postUsers,
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

