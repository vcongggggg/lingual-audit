using Mezon.Net.Client;
using SdkClient = Mezon.Net.Sdk.MezonClient;

namespace Lingual.Modules.MezonBot.Services;

public record TrackedButton(
    string Id,
    string Label,
    int Style = 2,
    string Url = "",
    string Icon = ""
);

public class TrackedButtonMessage
{
    public long ChannelId { get; init; }
    public long MessageId { get; init; }
    public string Text { get; init; } = string.Empty;
    public MessageEmbed? Embed { get; init; }
    public List<TrackedButton> Buttons { get; init; } = new();
    public bool SingleUse { get; init; } = true;
    public bool IsDisabled { get; set; }
    public System.Collections.Concurrent.ConcurrentDictionary<string, byte> ClickedUsers { get; } = new();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public record ClickValidationResult(
    bool Allowed,
    string? EphemeralReason = null
);

public interface IButtonInteractionObserver
{
    void TrackMessage(TrackedButtonMessage message);
    bool TryGetTrackedMessage(long messageId, out TrackedButtonMessage? message);
    void MarkMessageDisabled(long messageId);
    ClickValidationResult CanProcessClick(long channelId, long messageId, string buttonId, string mezonUserId);
    Task DisableButtonsAsync(
        SdkClient? client,
        long channelId,
        long messageId,
        string clickedButtonId);
}
