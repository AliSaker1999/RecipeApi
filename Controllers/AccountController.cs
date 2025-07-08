using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RecipeApi.Dtos;
using RecipeApi.Models;
using RecipeApi.Services;
using RecipeApi.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RecipeApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly ITokenService _tokenService;
        private readonly PasswordHasher<AppUser> _hasher = new();

        public AccountController(UserService userService, ITokenService tokenService)
        {
            _userService = userService;
            _tokenService = tokenService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserTokenDto>> Login(LoginDto dto)
        {
            var user = await _userService.GetByUsernameAsync(dto.Username);
            if (user == null)
                return Unauthorized("Invalid username");

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
                return Unauthorized("Invalid password");

            return new UserTokenDto
            {
                Username = user.UserName,
                Role = user.Role,
                Token = _tokenService.CreateToken(user)
            };
        }

        [HttpPost("register")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Register(RegisterDto dto)
        {
            var userExists = await _userService.GetByUsernameAsync(dto.Username);
            if (userExists != null)
                return BadRequest("Username already exists.");

            var user = new AppUser
            {
                UserName = dto.Username,
                Email = dto.Email,
                Role = "User"
            };
            user.PasswordHash = _hasher.HashPassword(user, dto.Password);

            await _userService.CreateAsync(user);
            return Ok("User registered successfully.");
        }

        [HttpDelete("{username}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteUser(string username)
        {
            var user = await _userService.GetByUsernameAsync(username);
            if (user == null) return NotFound("User not found.");

            await _userService.DeleteAsync(user.Id);
            return Ok("User deleted successfully.");
        }

        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<string>>> GetAllUsers()
        {
            var users = await _userService.GetAllUsernamesAsync();
            return Ok(users);
        }
    }
}
