namespace Ayurveda_chatBot.Models
{
    public class Herb : BaseEntity
    {
        public string EnglishName { get; set; }
        public string SanskritName { get; set; }
        public string Uses { get; set; }
        public string Contraindications { get; set; }
    }

}
