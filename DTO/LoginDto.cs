namespace Ayurveda_chatBot.DTO
{
    public class LoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class SocialLoginDto
    {
        // ID token received from frontend (VERY IMPORTANT)
        public string IdToken { get; set; } = string.Empty;

        // "google" or "microsoft"
        public string? Provider { get; set; }
        public string? ProviderId { get; set; }
    }
    public class SocialLoginResponseDto
    {
        public string token { get; set; }
        public bool isOnboardingCompleted { get; set; }
    }



}
