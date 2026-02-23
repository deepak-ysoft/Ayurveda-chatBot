namespace Ayurveda_chatBot.Helpers
{
    public static class EmergencyChecker
    {
        public static bool IsEmergency(string message)
        {
            var keywords = new List<string>
        {
            "chest pain",
            "breathing difficulty",
            "bleeding",
            "unconscious",
            "heart attack",
            "stroke"
        };

            return keywords.Any(k => message.ToLower().Contains(k));
        }
    }
}
