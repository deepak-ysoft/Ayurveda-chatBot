using Ayurveda_chatBot.Data;
using Ayurveda_chatBot.DTO;
using Ayurveda_chatBot.Helpers;
using Ayurveda_chatBot.Models;
using Ayurveda_chatBot.Services.Interface;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Ayurveda_chatBot.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtTokenGenerator _jwtGenerator;

        public AuthService(ApplicationDbContext context, JwtTokenGenerator jwtGenerator)
        {
            _context = context;
            _jwtGenerator = jwtGenerator;
        }

        public async Task<LoginResponseDto> SocialLoginAsync(SocialLoginDto dto)
        {
            string email = "";
            string name = "";
            string providerUserId = "";

            if (dto.Provider.ToLower() == "google")
            {
                try
                {
                    var payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken);
                    email = payload.Email;
                    name = payload.Name;
                    providerUserId = payload.Subject;
                }
                catch (InvalidJwtException)
                {
                    // If it's not a valid JWT, it might be an access token
                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", dto.IdToken);
                    var response = await client.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo");

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception("Invalid Google token");
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var googleUser = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(content);

                    email = googleUser.email;
                    name = googleUser.name;
                    providerUserId = googleUser.sub;
                }
            }
            else if (dto.Provider.ToLower() == "microsoft")
            {
                var handler = new JwtSecurityTokenHandler();
                var tokens = handler.ReadJwtToken(dto.IdToken);

                email = tokens.Claims.First(x => x.Type == "preferred_username").Value;
                name = tokens.Claims.First(x => x.Type == "name").Value;
                providerUserId = tokens.Claims.First(x => x.Type == "oid").Value;
            }
            else
            {
                throw new Exception("Unsupported provider");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email == email);

            if (user == null)
            {
                user = new User
                {
                    Name = name,
                    Email = email,
                    Provider = dto.Provider,
                    ProviderId = providerUserId,
                    PasswordHash = null
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            var token = _jwtGenerator.GenerateToken(user);
            return new LoginResponseDto
            {
                token=token,
                userId = user.Id,
                name = user.Name,
                email = user.Email,
                isOnboardingCompleted = user.isOnboardingCompleted
            };
        }

        public async Task<ProfileResponseDto> GetProfileAsync(Guid userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted);
            if (user == null)
                throw new Exception("User not found");
            var userDosha = await _context.UserSavedDoshas
                .Include(x => x.Dosha)
                .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);
            return new ProfileResponseDto
            {
                Age = user.Age??0,
                Gender = user.Gender,
                Diet = user.Diet,
                Weight = user.Weight,
                Dosha = userDosha?.Dosha?.Name
            };
        }

        public async Task<string> CompleteOnboardingAsync(Guid userId, OnboardingDto model)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted);

            if (user == null)
                throw new Exception("User not found");

            // Update user profile
            user.Age = model.Age;
            user.Gender = model.Gender;
            user.Diet = model.Diet;
            user.Weight = model.Weight;
            user.UpdatedAt = DateTime.UtcNow; 
            user.isOnboardingCompleted = true;

            // Find Dosha by name
            var dosha = await _context.Doshas
                .FirstOrDefaultAsync(x => x.Name == model.Dosha && !x.IsDeleted);

            if (dosha == null)
                throw new Exception("Invalid Dosha selected");

            // Remove old dosha if exists
            var existing = await _context.UserSavedDoshas
                .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);

            if (existing != null)
            {
                existing.IsDeleted = true;
            }

            // Save new dosha
            var userDosha = new UserSavedDosha
            {
                UserId = userId,
                DoshaId = dosha.Id
            };

            await _context.UserSavedDoshas.AddAsync(userDosha);

            await _context.SaveChangesAsync();

            return "Onboarding completed successfully.";
        }
    }
}
