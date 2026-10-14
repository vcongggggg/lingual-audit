namespace Lingual.Modules.MezonBot;

public class MezonBotOptions
{
    public const string SectionName = "Mezon";

    /// <summary>
    /// ID ứng dụng hoặc Bot trên Mezon (hỗ trợ cả AppId hoặc BotId).
    /// </summary>
    public string AppId { get; set; } = string.Empty;
    public string BotId { get; set; } = string.Empty;

    /// <summary>
    /// Chuỗi Bot Token bảo mật được cấp trên Mezon Developer Portal.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Clan ID mặc định.
    /// </summary>
    public string ClanId { get; set; } = string.Empty;

    /// <summary>
    /// Channel ID mặc định.
    /// </summary>
    public string DefaultChannelId { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Cờ bật/tắt dịch vụ Mezon Bot.
    /// </summary>
    public bool Enabled { get; set; } = true;

    public long GetEffectiveBotId()
    {
        var idStr = !string.IsNullOrWhiteSpace(BotId) ? BotId : AppId;
        return long.TryParse(idStr, out var id) ? id : 0;
    }

    public long GetEffectiveClanId()
    {
        return long.TryParse(ClanId, out var id) ? id : 0;
    }

    public long GetEffectiveChannelId()
    {
        var idStr = !string.IsNullOrWhiteSpace(ChannelId) ? ChannelId : DefaultChannelId;
        return long.TryParse(idStr, out var id) ? id : 0;
    }
}
