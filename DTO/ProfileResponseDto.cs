using Ayurveda_chatBot.Models;

namespace Ayurveda_chatBot.DTO
{
    public class ProfileResponseDto
    {
        public int Age { get; set; }
        public string Gender { get; set; }
        public string Diet { get; set; }
        public string Weight { get; set; }
        public string Dosha { get; set; }
    }
}
