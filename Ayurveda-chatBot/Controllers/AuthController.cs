using Ayurveda_chatBot.Data;
using Ayurveda_chatBot.DTO;
using Ayurveda_chatBot.Helpers;
using Ayurveda_chatBot.Models;
using Ayurveda_chatBot.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Ayurveda_chatBot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;
        private readonly JwtTokenGenerator _jwtGenerator;

        public AuthController(ApplicationDbContext context, IAuthService authService,
                              JwtTokenGenerator jwtGenerator)
        {
            _context = context;
            _authService = authService;
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
                PasswordHash = PasswordHasher.HashPassword(dto.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = _jwtGenerator.GenerateToken(user);

            return Ok(new LoginResponseDto
            {
                token = token,
                userId = user.Id,
                name = user.Name,
                email = user.Email,
                isOnboardingCompleted = user.isOnboardingCompleted
            });
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

            return Ok(new LoginResponseDto
            {
               token= token,
                userId = user.Id,
                name = user.Name,
                email = user.Email,
                isOnboardingCompleted = user.isOnboardingCompleted
            });
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = Guid.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var res = await _authService.GetProfileAsync(userId);
            return Ok(res);
        }

        [HttpPost("social-login")]
        public async Task<IActionResult> SocialLogin([FromBody] SocialLoginDto dto)
        {
            var res = await _authService.SocialLoginAsync(dto);
            return Ok(res);
        }

        [Authorize]
        [HttpPost("onboarding")]
        public async Task<IActionResult> CompleteOnboarding(OnboardingDto model)
        {
            var userId = Guid.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var result = await _authService.CompleteOnboardingAsync(userId, model);

            return Ok(new { message = result });
        }
    }
}
