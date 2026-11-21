using System;
using System.Collections.Generic;
using FluentAssertions;
using Lingual.Modules.MezonBot.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lingual.Modules.MezonBot.Tests;

public class ButtonInteractionObserverTests
{
    private readonly ButtonInteractionObserver _observer;

    public ButtonInteractionObserverTests()
    {
        _observer = new ButtonInteractionObserver(NullLogger<ButtonInteractionObserver>.Instance);
    }

    [Fact]
    public void CanProcessClick_DisabledButtonPattern_ReturnsRejected()
    {
        // Act
        var result = _observer.CanProcessClick(123L, 456L, "btn_quiz_disabled_A", "user_1");

        // Assert
        result.Allowed.Should().BeFalse();
        result.EphemeralReason.Should().Contain("Lựa chọn này đã kết thúc");
    }

    [Fact]
    public void CanProcessClick_SingleUseMessage_AllowsFirstClick_AndRejectsSubsequentClicks()
    {
        // Arrange
        var messageId = 1001L;
        var channelId = 500L;
        var tracked = new TrackedButtonMessage
        {
            ChannelId = channelId,
            MessageId = messageId,
            Text = "Daily Streak",
            Buttons = new List<TrackedButton>
            {
                new("btn_daily_checkin", "🔥 Điểm danh", 1),
                new("btn_view_streak", "🔥 Chuỗi", 2)
            },
            SingleUse = true
        };
        _observer.TrackMessage(tracked);

        // Act 1: First click by user_1
        var firstClick = _observer.CanProcessClick(channelId, messageId, "btn_daily_checkin", "user_1");

        // Assert 1
        firstClick.Allowed.Should().BeTrue();
        tracked.IsDisabled.Should().BeTrue();

        // Act 2: Second click by user_1 or another user on the same single-use message
        var secondClick = _observer.CanProcessClick(channelId, messageId, "btn_view_streak", "user_2");

        // Assert 2: Must be rejected to avoid message spam
        secondClick.Allowed.Should().BeFalse();
        secondClick.EphemeralReason.Should().Contain("vô hiệu hóa để tránh spam tin nhắn");
    }

    [Fact]
    public void CanProcessClick_MultiUserMessage_AllowsDifferentUsers_RejectsSameUser()
    {
        // Arrange (e.g. Clan Quiz with multiple participants)
        var messageId = 2002L;
        var channelId = 500L;
        var tracked = new TrackedButtonMessage
        {
            ChannelId = channelId,
            MessageId = messageId,
            Text = "Clan Quiz",
            Buttons = new List<TrackedButton>
            {
                new("btn_quiz_session_A", "A. Apple", 2),
                new("btn_quiz_session_B", "B. Banana", 2)
            },
            SingleUse = false
        };
        _observer.TrackMessage(tracked);

        // Act 1: User 1 answers
        var user1Click = _observer.CanProcessClick(channelId, messageId, "btn_quiz_session_A", "user_1");

        // Act 2: User 2 answers
        var user2Click = _observer.CanProcessClick(channelId, messageId, "btn_quiz_session_B", "user_2");

        // Act 3: User 1 tries to answer again
        var user1Repeat = _observer.CanProcessClick(channelId, messageId, "btn_quiz_session_B", "user_1");

        // Assert
        user1Click.Allowed.Should().BeTrue();
        user2Click.Allowed.Should().BeTrue();
        user1Repeat.Allowed.Should().BeFalse();
        user1Repeat.EphemeralReason.Should().Contain("Bạn đã gửi câu trả lời");
    }

    [Fact]
    public void CanProcessClick_HistoricalUntrackedMessage_AllowsFirst_RejectsSecond()
    {
        // Arrange: Message sent before bot restart (not in TrackMessage)
        var messageId = 9999L;
        var channelId = 500L;
        var buttonId = "btn_roleplay_select_cafe_order";

        // Act 1: First click
        var firstClick = _observer.CanProcessClick(channelId, messageId, buttonId, "user_1");

        // Act 2: Second click on the same historical button
        var secondClick = _observer.CanProcessClick(channelId, messageId, buttonId, "user_1");

        // Assert
        firstClick.Allowed.Should().BeTrue();
        secondClick.Allowed.Should().BeFalse();
    }

    [Fact]
    public void MarkMessageDisabled_SetsIsDisabledToTrue_AndRejectsClicks()
    {
        // Arrange
        var messageId = 3003L;
        var channelId = 500L;
        var tracked = new TrackedButtonMessage
        {
            ChannelId = channelId,
            MessageId = messageId,
            Text = "Quiz question",
            Buttons = new List<TrackedButton>
            {
                new("btn_quiz_A", "A", 2)
            },
            SingleUse = false
        };
        _observer.TrackMessage(tracked);

        // Act
        _observer.MarkMessageDisabled(messageId);
        var click = _observer.CanProcessClick(channelId, messageId, "btn_quiz_A", "user_1");

        // Assert
        tracked.IsDisabled.Should().BeTrue();
        click.Allowed.Should().BeFalse();
        click.EphemeralReason.Should().Contain("vô hiệu hóa để tránh spam tin nhắn");
    }
}

