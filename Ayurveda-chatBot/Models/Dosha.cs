namespace Ayurveda_chatBot.Models
{
    public class Dosha : BaseEntity
    {
        public string Name { get; set; }
        public string Qualities { get; set; }
        public string ImbalanceSymptoms { get; set; }
        public string DietAdvice { get; set; }
        public string LifestyleAdvice { get; set; }
    }

    public class UserSavedDosha : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid DoshaId { get; set; }
        public User User { get; set; }
        public Dosha Dosha { get; set; }
    }
}
