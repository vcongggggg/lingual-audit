namespace Lingual.Modules.AITutor.Options;

public class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-3.1-flash-lite";
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
    public int DailyFreeTurns { get; set; } = 5;
    public int DailyBonusCap { get; set; } = 5;
    public int GuestDailyTurns { get; set; } = 2;
}
