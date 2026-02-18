using Ayurveda_chatBot.Data;
using Ayurveda_chatBot.DTO;
using Ayurveda_chatBot.Helpers;
using Ayurveda_chatBot.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ayurveda_chatBot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtTokenGenerator _jwtGenerator;

        public AuthController(ApplicationDbContext context,
                              JwtTokenGenerator jwtGenerator)
        {
            _context = context;
            _jwtGenerator = jwtGenerator;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (_context.Users.Any(u => u.Email == dto.Email))
                return BadRequest("Email already exists.");

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                PasswordHash = PasswordHasher.HashPassword(dto.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("User registered successfully.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = _context.Users
                .FirstOrDefault(u => u.Email == dto.Email && !u.IsDeleted);

            if (user == null)
                return Unauthorized("Invalid credentials.");

            if (!PasswordHasher.VerifyPassword(dto.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials.");

            var token = _jwtGenerator.GenerateToken(user);

            return Ok(new
            {
                token,
                userId = user.Id,
                name = user.Name,
                email = user.Email
            });
        }
    }

}
