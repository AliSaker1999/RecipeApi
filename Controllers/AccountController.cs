using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeApi.Dtos;
using RecipeApi.Models;
using RecipeApi.Services;
using RecipeApi.Interfaces;
namespace RecipeApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ITokenService _tokenService;

        public AccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            ITokenService tokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserTokenDto>> Login(LoginDto dto)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == dto.Username);
            if (user == null) return Unauthorized("Invalid username");

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
            if (!result.Succeeded) return Unauthorized("Invalid password");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "User";

            return new UserTokenDto
            {
                Username = user.UserName,
                Role = role,
                Token = _tokenService.CreateToken(user, role)
            };
        }

        [HttpPost("register")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Register(RegisterDto dto)
        {
            var userExists = await _userManager.FindByNameAsync(dto.Username);
            if (userExists != null)
                return BadRequest("Username already exists.");

            var user = new AppUser
            {
                UserName = dto.Username,
                Email = dto.Email
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(user, "User");
            return Ok("User registered successfully.");
        }
        [HttpDelete("{username}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteUser(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null) return NotFound("User not found.");

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok("User deleted successfully.");
        }
        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<string>>> GetAllUsers()
        {
            var users = await _userManager.Users.Select(u => u.UserName).ToListAsync();
            return Ok(users);
        }

    }
}
