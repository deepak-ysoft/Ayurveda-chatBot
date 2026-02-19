using Ayurveda_chatBot.DTO;

namespace Ayurveda_chatBot.Services.Interface
{
    public interface IAuthService
    {
        Task<string> CompleteOnboardingAsync(Guid userId, OnboardingDto model); 
        Task<SocialLoginResponseDto> SocialLoginAsync(SocialLoginDto dto);
    }
}
