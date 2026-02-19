using Ayurveda_chatBot.Data;
using Ayurveda_chatBot.DTO;
using Ayurveda_chatBot.Helpers;
using Ayurveda_chatBot.Models;
using Ayurveda_chatBot.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace Ayurveda_chatBot.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;

        public AuthService(ApplicationDbContext context)
        {
            _context = context;
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

            _context.UserSavedDoshas.Add(userDosha);

            await _context.SaveChangesAsync();

            return "Onboarding completed successfully.";
        }
    }
}
