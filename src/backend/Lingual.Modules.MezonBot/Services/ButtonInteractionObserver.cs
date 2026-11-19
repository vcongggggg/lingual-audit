using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Mezon.Net.Client;
using Mezon.Net.Models;
using Mezon.Net.Sdk.Builders;
using SdkClient = Mezon.Net.Sdk.MezonClient;

namespace Lingual.Modules.MezonBot.Services;

public class ButtonInteractionObserver : IButtonInteractionObserver
{
    private readonly ILogger<ButtonInteractionObserver> _logger;
    private readonly ConcurrentDictionary<long, TrackedButtonMessage> _trackedMessages = new();
    private readonly ConcurrentDictionary<string, DateTime> _clickCooldowns = new();
    private readonly ConcurrentDictionary<string, DateTime> _historicalClicks = new();

    public ButtonInteractionObserver(ILogger<ButtonInteractionObserver> logger)
    {
        _logger = logger;
    }

    public void TrackMessage(TrackedButtonMessage message)
    {
        if (message.MessageId <= 0) return;

        _trackedMessages[message.MessageId] = message;

        // Dọn dẹp tin nhắn cũ nếu vượt quá 500 mục hoặc cũ hơn 2 giờ
        if (_trackedMessages.Count > 500)
        {
            var cutoff = DateTime.UtcNow.AddHours(-2);
            foreach (var kvp in _trackedMessages)
            {
                if (kvp.Value.CreatedAt < cutoff)
                {
                    _trackedMessages.TryRemove(kvp.Key, out _);
                }
            }
        }
    }

    public bool TryGetTrackedMessage(long messageId, out TrackedButtonMessage? message)
    {
        return _trackedMessages.TryGetValue(messageId, out message);
    }

    public void MarkMessageDisabled(long messageId)
    {
        if (_trackedMessages.TryGetValue(messageId, out var tracked))
        {
            tracked.IsDisabled = true;
        }
    }

    public ClickValidationResult CanProcessClick(long channelId, long messageId, string buttonId, string mezonUserId)
    {
        var now = DateTime.UtcNow;

        // 1. Kiểm tra pattern nút đã disable từ trước
        if (buttonId.StartsWith("btn_quiz_disabled_") || buttonId.StartsWith("btn_disabled_"))
        {
            return new(false, "🔒 Lựa chọn này đã kết thúc hoặc bạn đã chọn trước đó rồi nhé!");
        }

        // 2. Debounce chống spam click liên tiếp (2 giây)
        var debounceKey = $"{channelId}_{mezonUserId}_{buttonId}";
        if (_clickCooldowns.TryGetValue(debounceKey, out var lastClick) && (now - lastClick).TotalSeconds < 2)
        {
            return new(false, "⏳ Thao tác quá nhanh, vui lòng chờ giây lát trước khi bấm tiếp!");
        }

        // 3. Quan sát trạng thái tin nhắn được theo dõi (Tracked Message)
        if (_trackedMessages.TryGetValue(messageId, out var tracked))
        {
            if (tracked.IsDisabled)
            {
                return new(false, "🔒 Phản hồi này đã được hoàn tất. Nút bấm đã bị vô hiệu hóa để tránh spam tin nhắn!");
            }

            // Với tin nhắn single-use (chỉ cho phép chọn 1 lần duy nhất trên tin nhắn)
            if (tracked.SingleUse && tracked.ClickedUsers.Count > 0)
            {
                return new(false, "🔒 Lựa chọn trên tin nhắn này đã được ghi nhận rồi!");
            }

            // Với tin nhắn nhiều người tham gia (ví dụ Clan Quiz 30s), mỗi người chỉ được chọn 1 lần
            if (tracked.ClickedUsers.ContainsKey(mezonUserId))
            {
                return new(false, "⚠️ Bạn đã gửi câu trả lời/lựa chọn cho nội dung này rồi nhé!");
            }

            // Khóa đồng thời ngay lập tức để chặn race condition khi nhiều luồng tới cùng lúc
            _clickCooldowns[debounceKey] = now;
            tracked.ClickedUsers.TryAdd(mezonUserId, 1);
            if (tracked.SingleUse)
            {
                tracked.IsDisabled = true;
            }

            return new(true);
        }

        // 4. Dự phòng cho các tin nhắn lịch sử (gửi trước khi bot khởi động lại)
        var historicalKey = $"{channelId}_{messageId}_{buttonId}";
        if (_historicalClicks.ContainsKey(historicalKey))
        {
            return new(false, "🔒 Nút bấm của tin nhắn cũ này đã được xử lý trước đó rồi!");
        }

        _clickCooldowns[debounceKey] = now;
        _historicalClicks.TryAdd(historicalKey, now);
        return new(true);
    }

    public async Task DisableButtonsAsync(
        SdkClient? client,
        long channelId,
        long messageId,
        string clickedButtonId)
    {
        if (client == null || channelId <= 0 || messageId <= 0) return;

        if (!_trackedMessages.TryGetValue(messageId, out var tracked))
        {
            return;
        }

        try
        {
            var disabledButtons = new ButtonBuilder();
            foreach (var btn in tracked.Buttons)
            {
                var isClicked = btn.Id.Equals(clickedButtonId, StringComparison.OrdinalIgnoreCase);
                var label = btn.Label;
                if (isClicked && !label.StartsWith("✓ "))
                {
                    label = $"✓ {label}";
                }

                var shouldDisable = tracked.SingleUse || isClicked;
                var style = isClicked ? 1 : btn.Style;

                disabledButtons.AddButton(
                    btn.Id,
                    label,
                    style,
                    disable: shouldDisable,
                    url: btn.Url,
                    icon: btn.Icon);
            }

            var updateBuilder = new MessageContentBuilder().SetText(tracked.Text);
            if (tracked.Embed != null)
            {
                updateBuilder.AddEmbed(tracked.Embed);
            }
            updateBuilder.AddActionRow(disabledButtons.BuildComponents());
            var updatedContent = updateBuilder.Build();

            var channel = await client.GetChannelAsync(channelId);
            if (channel != null)
            {
                await channel.UpdateMessageAsync(messageId, updatedContent);
                _logger.LogInformation("🔒 [ButtonObserver] Đã tự động disable buttons cho Message {MessageId} tại Channel {ChannelId}", messageId, channelId);
            }
            else
            {
                var updateParams = new ChannelMessageUpdateParams(
                    clanId: 0,
                    channelId: channelId,
                    messageId: messageId,
                    content: updatedContent.ToJson(),
                    mentions: null,
                    attachments: null,
                    mode: 0,
                    isPublic: false,
                    hideEditted: false,
                    topicId: null,
                    isUpdateMsgTopic: null,
                    createTimeSeconds: null
                );
                await client.UpdateChannelMessageAsync(updateParams);
                _logger.LogInformation("🔒 [ButtonObserver] Đã tự động disable buttons (Fallback) cho Message {MessageId} tại Channel {ChannelId}", messageId, channelId);
            }

            if (tracked.SingleUse)
            {
                tracked.IsDisabled = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [ButtonObserver] Lỗi khi cập nhật disable buttons cho Message {MessageId}", messageId);
        }
    }
}

