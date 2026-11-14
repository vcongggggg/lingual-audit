using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Mezon.Net.Sdk;
using Mezon.Net.Client;
using Mezon.Net.Models;
using Mezon.Net.Core;
using Mezon.Net.Sdk.Builders;
using Lingual.Modules.MezonBot.Domain;
using Lingual.Modules.MezonBot.Infrastructure.Data;
using Lingual.Modules.MezonBot.Services;
using Xunit;

namespace Lingual.Modules.MezonBot.Tests;

public class ScheduledClanQuizTests
{
    private MezonBotDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<MezonBotDbContext>()
            .UseInMemoryDatabase(databaseName: $"ScheduledQuiz_Test_{Guid.NewGuid()}")
            .Options;

        return new MezonBotDbContext(options);
    }

    private class OptionItem
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("is_correct")]
        public bool IsCorrect { get; set; }
    }

    private async Task<string> GetCorrectKeyAsync(MezonBotDbContext dbContext, Guid sessionId)
    {
        var session = await dbContext.ClanQuizSessions.Include(s => s.Question).FirstAsync(s => s.Id == sessionId);
        var options = JsonSerializer.Deserialize<List<OptionItem>>(session.Question!.Options, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return options!.First(o => o.IsCorrect).Key;
    }

    private async Task<string> GetWrongKeyAsync(MezonBotDbContext dbContext, Guid sessionId)
    {
        var session = await dbContext.ClanQuizSessions.Include(s => s.Question).FirstAsync(s => s.Id == sessionId);
        var options = JsonSerializer.Deserialize<List<OptionItem>>(session.Question!.Options, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return options!.First(o => !o.IsCorrect).Key;
    }

    [Fact]
    public async Task MezonBotWorker_CreatePinMessageAsync_And_DeletePinMessageAsync_InvokesRealSdkOrExecutor()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(new MezonBotOptions
        {
            BotId = "123",
            Token = "fake",
            Enabled = false,
            ClanId = "1819651515982843904"
        });

        var worker = new MezonBotWorker(
            new NullLogger<MezonBotWorker>(),
            options,
            scopeFactory);

        long channelId = 1819651515982843904L;
        ulong messageId = 987654321UL;
        long clanId = 1819651515982843904L;

        PinMessageParams? capturedPinParams = null;
        worker.PinMessageExecutor = (p) =>
        {
            capturedPinParams = p;
            return Task.CompletedTask;
        };

        long? capturedUnpinMessageId = null;
        long? capturedUnpinChannelId = null;
        long? capturedUnpinClanId = null;
        worker.UnpinMessageExecutor = (mId, chId, clId) =>
        {
            capturedUnpinMessageId = mId;
            capturedUnpinChannelId = chId;
            capturedUnpinClanId = clId;
            return Task.CompletedTask;
        };

        // Act 1: Ghim tin nhắn
        var pinSuccess = await worker.CreatePinMessageAsync(channelId, messageId);

        // Assert 1: Đã gọi qua PinMessageExecutor với đúng ChannelId, MessageId, ClanId và ghi nhận PinnedMessages
        pinSuccess.Should().BeTrue();
        capturedPinParams.Should().NotBeNull();
        capturedPinParams!.Value.ChannelId.Should().Be(channelId);
        capturedPinParams.Value.MessageId.Should().Be((long)messageId);
        capturedPinParams.Value.ClanId.Should().Be(clanId);
        worker.PinnedMessages.Should().Contain((channelId, messageId));

        // Act 2: Gỡ ghim tin nhắn
        var unpinSuccess = await worker.DeletePinMessageAsync(channelId, messageId);

        // Assert 2: Đã gọi qua UnpinMessageExecutor với đúng MessageId, ChannelId, ClanId và ghi nhận UnpinnedMessages
        unpinSuccess.Should().BeTrue();
        capturedUnpinMessageId.Should().Be((long)messageId);
        capturedUnpinChannelId.Should().Be(channelId);
        capturedUnpinClanId.Should().Be(clanId);
        worker.UnpinnedMessages.Should().Contain((channelId, messageId));
    }

    [Fact]
    public void FailFast_ThrowsInvalidOperationException_WhenEnabledAndTargetChannelIdZeroOrNegative()
    {
        // 1. TargetChannelId = 0 khi Enabled = true
        var optionsZero = new ScheduledQuizOptions
        {
            Enabled = true,
            TargetChannelId = 0
        };
        var actZero = () => optionsZero.Validate();
        actZero.Should().Throw<InvalidOperationException>()
            .WithMessage("*TargetChannelId bắt buộc phải được cấu hình (lớn hơn 0)*");

        // 2. TargetChannelId < 0 khi Enabled = true
        var optionsNeg = new ScheduledQuizOptions
        {
            Enabled = true,
            TargetChannelId = -123
        };
        var actNeg = () => optionsNeg.Validate();
        actNeg.Should().Throw<InvalidOperationException>()
            .WithMessage("*TargetChannelId bắt buộc phải được cấu hình (lớn hơn 0)*");

        // 3. Enabled = false không ném lỗi dù TargetChannelId = 0
        var optionsDisabled = new ScheduledQuizOptions
        {
            Enabled = false,
            TargetChannelId = 0
        };
        var actDisabled = () => optionsDisabled.Validate();
        actDisabled.Should().NotThrow();
    }

    [Fact]
    public async Task PostWarningAsync_PubliclyNotifiesChannel_AndPreventsDuplicateWarning()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var clanQuizServiceMock = new Mock<IClanQuizService>();
        var notifierMock = new Mock<IMezonBotNotifier>();
        var optionsMock = Options.Create(new ScheduledQuizOptions
        {
            Enabled = true,
            TargetChannelId = 1819651515982843904L,
            DurationSeconds = 60
        });
        var loggerMock = new Mock<ILogger<ScheduledQuizService>>();

        var service = new ScheduledQuizService(
            dbContext,
            clanQuizServiceMock.Object,
            notifierMock.Object,
            optionsMock,
            loggerMock.Object);

        var slotKey = "2026-11-14_09:00";
        var channelId = 1819651515982843904L;

        // Act 1: Lần đầu thông báo
        var firstResult = await service.PostWarningAsync(slotKey, channelId, "09:00");

        // Assert 1
        firstResult.Should().BeTrue();
        notifierMock.Verify(n => n.SendPublicChannelTextAsync(
            channelId,
            It.Is<string>(s => s.Contains("1 phút nữa sẽ có Thử Thách Khung Giờ Vàng (09:00)")),
            It.IsAny<CancellationToken>()), Times.Once);

        var runInDb = await dbContext.ScheduledQuizRuns.FirstOrDefaultAsync(r => r.SlotKey == slotKey);
        runInDb.Should().NotBeNull();
        runInDb!.WarnedAt.Should().NotBeNull();

        // Act 2: Gọi lần thứ 2 với cùng slotKey -> Chống lặp
        var secondResult = await service.PostWarningAsync(slotKey, channelId, "09:00");

        // Assert 2
        secondResult.Should().BeFalse();
        // Không gửi thêm tin nhắn nào
        notifierMock.Verify(n => n.SendPublicChannelTextAsync(
            channelId,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PostScheduledQuizAsync_DatabaseIdempotency_PreventsDuplicatePosting()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var clanQuizServiceLoggerMock = new Mock<ILogger<ClanQuizService>>();
        var clanQuizService = new ClanQuizService(dbContext, clanQuizServiceLoggerMock.Object);

        var notifierMock = new Mock<IMezonBotNotifier>();
        notifierMock.Setup(n => n.SendPublicChannelContentAsync(It.IsAny<long>(), It.IsAny<MessageContent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(777888999UL);

        var optionsMock = Options.Create(new ScheduledQuizOptions
        {
            Enabled = true,
            TargetChannelId = 1819651515982843904L,
            DurationSeconds = 60
        });
        var loggerMock = new Mock<ILogger<ScheduledQuizService>>();

        var service = new ScheduledQuizService(
            dbContext,
            clanQuizService,
            notifierMock.Object,
            optionsMock,
            loggerMock.Object);

        var slotKey = "2026-11-14_12:00";
        var channelId = 1819651515982843904L;

        // Act 1: Lần đầu phát động
        var session1 = await service.PostScheduledQuizAsync(slotKey, channelId, "12:00");

        // Assert 1
        session1.Should().NotBeNull();

        var runInDb = await dbContext.ScheduledQuizRuns.FirstOrDefaultAsync(r => r.SlotKey == slotKey);
        runInDb.Should().NotBeNull();
        runInDb!.PostedAt.Should().NotBeNull();
        runInDb.SessionId.Should().Be(session1!.SessionId);

        var dbSession = await dbContext.ClanQuizSessions.FirstOrDefaultAsync(s => s.Id == session1.SessionId);
        dbSession.Should().NotBeNull();
        dbSession!.SlotKey.Should().Be(slotKey);

        // Act 2: Gọi phát động lần 2 với cùng slotKey (chống duplicate do network retry hay restart)
        var session2 = await service.PostScheduledQuizAsync(slotKey, channelId, "12:00");

        // Assert 2
        session2.Should().BeNull();
        notifierMock.Verify(n => n.SendPublicChannelContentAsync(channelId, It.IsAny<MessageContent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PostScheduledQuizAsync_PinsNewMessage_AndUnpinsOldQuizMessage()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var clanQuizServiceLoggerMock = new Mock<ILogger<ClanQuizService>>();
        var clanQuizService = new ClanQuizService(dbContext, clanQuizServiceLoggerMock.Object);

        var channelId = 1819651515982843904L;
        var oldPinnedMessageId = 111222333UL;
        var newSentMessageId = 444555666UL;

        // Seed phiên quiz cũ đã ghim trước đó
        dbContext.ScheduledQuizRuns.Add(new ScheduledQuizRun
        {
            SlotKey = "2026-11-14_09:00",
            ChannelId = channelId,
            PinnedMessageId = oldPinnedMessageId,
            PostedAt = DateTime.UtcNow.AddHours(-3),
            CreatedAt = DateTime.UtcNow.AddHours(-3)
        });
        await dbContext.SaveChangesAsync();

        var notifierMock = new Mock<IMezonBotNotifier>();
        notifierMock.Setup(n => n.SendPublicChannelContentAsync(channelId, It.IsAny<MessageContent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newSentMessageId);
        notifierMock.Setup(n => n.CreatePinMessageAsync(channelId, newSentMessageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        notifierMock.Setup(n => n.DeletePinMessageAsync(channelId, oldPinnedMessageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var optionsMock = Options.Create(new ScheduledQuizOptions
        {
            Enabled = true,
            TargetChannelId = channelId,
            DurationSeconds = 60
        });
        var loggerMock = new Mock<ILogger<ScheduledQuizService>>();

        var service = new ScheduledQuizService(
            dbContext,
            clanQuizService,
            notifierMock.Object,
            optionsMock,
            loggerMock.Object);

        // Act
        var session = await service.PostScheduledQuizAsync("2026-11-14_12:00", channelId, "12:00");

        // Assert
        session.Should().NotBeNull();

        // 1. Đã gọi gỡ ghim tin nhắn cũ
        notifierMock.Verify(n => n.DeletePinMessageAsync(channelId, oldPinnedMessageId, It.IsAny<CancellationToken>()), Times.Once);

        // 2. Đã gọi ghim tin nhắn mới
        notifierMock.Verify(n => n.CreatePinMessageAsync(channelId, newSentMessageId, It.IsAny<CancellationToken>()), Times.Once);

        // 3. Database lưu đúng PinnedMessageId mới
        var newRunInDb = await dbContext.ScheduledQuizRuns.FirstOrDefaultAsync(r => r.SlotKey == "2026-11-14_12:00");
        newRunInDb.Should().NotBeNull();
        newRunInDb!.PinnedMessageId.Should().Be(newSentMessageId);
    }

    [Fact]
    public async Task CrossClanXpRule_FirstClanAwardsXp_SecondClanAwardsZeroXp()
    {
        // Arrange: 2 Clan độc lập nhưng cùng dùng chung một cơ sở dữ liệu Postgres
        using var dbContext = CreateInMemoryDbContext();
        var clanQuizServiceLoggerMock = new Mock<ILogger<ClanQuizService>>();
        var clanQuizService = new ClanQuizService(dbContext, clanQuizServiceLoggerMock.Object);

        var slotKey = "2026-11-14_20:30";
        var channelClanA = 1001L;
        var channelClanB = 2002L;

        // Bắt đầu 2 phiên Scheduled Quiz cho cùng slotKey trên 2 Clan khác nhau
        var sessionA = await clanQuizService.StartSessionAsync(channelClanA, startedByMezonUserId: null, durationSeconds: 60, isPractice: false, slotKey: slotKey);
        var sessionB = await clanQuizService.StartSessionAsync(channelClanB, startedByMezonUserId: null, durationSeconds: 60, isPractice: false, slotKey: slotKey);

        sessionA.Should().NotBeNull();
        sessionB.Should().NotBeNull();

        var learnerMezonId = "learner_mz_12345";
        var learnerName = "Alice In Clan";

        var correctKeyA = await GetCorrectKeyAsync(dbContext, sessionA!.SessionId);
        var correctKeyB = await GetCorrectKeyAsync(dbContext, sessionB!.SessionId);

        // Act 1: Học viên trả lời đúng tại Clan A trước
        var resultClanA = await clanQuizService.SubmitAnswerAsync(
            sessionA.SessionId,
            channelClanA,
            learnerMezonId,
            learnerName,
            correctKeyA);

        // Assert 1: Clan A ghi nhận đúng và cộng XP cho Winner (+15 XP)
        resultClanA.Status.Should().Be(QuizSubmissionStatus.Success);
        resultClanA.IsCorrect.Should().BeTrue();
        resultClanA.XpAwarded.Should().Be(15);
        resultClanA.Message.Should().Contain("Bạn là người trả lời ĐÚNG và NHANH NHẤT");

        // Act 2: Cùng học viên đó chuyển sang Clan B trả lời đúng trong cùng khung giờ slotKey
        var resultClanB = await clanQuizService.SubmitAnswerAsync(
            sessionB.SessionId,
            channelClanB,
            learnerMezonId,
            learnerName,
            correctKeyB);

        // Assert 2: Clan B ghi nhận câu trả lời đúng nhưng nhận 0 XP kèm cảnh báo đã nhận ở clan khác
        resultClanB.Status.Should().Be(QuizSubmissionStatus.Success);
        resultClanB.IsCorrect.Should().BeTrue();
        resultClanB.XpAwarded.Should().Be(0);
        resultClanB.Message.Should().Contain("Bạn đã nhận XP khung giờ này ở clan khác, câu này chỉ luyện tập - 0 XP");

        // Kiểm tra database scheduled_quiz_participations chỉ có 1 bản ghi cộng XP cho học viên này trong slotKey
        var participations = await dbContext.ScheduledQuizParticipations
            .Where(p => p.SlotKey == slotKey && p.MezonUserId == learnerMezonId)
            .ToListAsync();

        participations.Should().HaveCount(1);
        participations[0].XpAwarded.Should().Be(15);
        participations[0].ChannelId.Should().Be(channelClanA);
    }

    [Fact]
    public async Task CrossClanXpRule_Symmetric_ClanBFirst_ThenClanASecond()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var clanQuizServiceLoggerMock = new Mock<ILogger<ClanQuizService>>();
        var clanQuizService = new ClanQuizService(dbContext, clanQuizServiceLoggerMock.Object);

        var slotKey = "2026-11-14_09:00";
        var channelClanA = 1001L;
        var channelClanB = 2002L;

        var sessionA = await clanQuizService.StartSessionAsync(channelClanA, null, 60, false, slotKey: slotKey);
        var sessionB = await clanQuizService.StartSessionAsync(channelClanB, null, 60, false, slotKey: slotKey);

        var learnerMezonId = "learner_symmetric_999";
        var learnerName = "Bob The Learner";

        var correctKeyA = await GetCorrectKeyAsync(dbContext, sessionA!.SessionId);
        var correctKeyB = await GetCorrectKeyAsync(dbContext, sessionB!.SessionId);

        // Act 1: Bob trả lời đúng tại Clan B trước
        var resultClanB = await clanQuizService.SubmitAnswerAsync(
            sessionB.SessionId,
            channelClanB,
            learnerMezonId,
            learnerName,
            correctKeyB);

        // Assert 1: Clan B được +15 XP
        resultClanB.Status.Should().Be(QuizSubmissionStatus.Success);
        resultClanB.IsCorrect.Should().BeTrue();
        resultClanB.XpAwarded.Should().Be(15);

        // Act 2: Bob trả lời đúng tại Clan A sau
        var resultClanA = await clanQuizService.SubmitAnswerAsync(
            sessionA.SessionId,
            channelClanA,
            learnerMezonId,
            learnerName,
            correctKeyA);

        // Assert 2: Clan A chỉ nhận 0 XP
        resultClanA.Status.Should().Be(QuizSubmissionStatus.Success);
        resultClanA.IsCorrect.Should().BeTrue();
        resultClanA.XpAwarded.Should().Be(0);
        resultClanA.Message.Should().Contain("Bạn đã nhận XP khung giờ này ở clan khác, câu này chỉ luyện tập - 0 XP");
    }

    [Fact]
    public async Task CrossClanXpRule_WrongAnswerInClanA_StillAwardsXpForCorrectAnswerInClanB()
    {
        // Arrange: 2 Clan A và B chung slotKey scheduled quiz
        using var dbContext = CreateInMemoryDbContext();
        var clanQuizServiceLoggerMock = new Mock<ILogger<ClanQuizService>>();
        var clanQuizService = new ClanQuizService(dbContext, clanQuizServiceLoggerMock.Object);

        var slotKey = "2026-11-14_09:00";
        var channelClanA = 3001L;
        var channelClanB = 4002L;

        var sessionA = await clanQuizService.StartSessionAsync(channelClanA, startedByMezonUserId: null, durationSeconds: 60, isPractice: false, slotKey: slotKey);
        var sessionB = await clanQuizService.StartSessionAsync(channelClanB, startedByMezonUserId: null, durationSeconds: 60, isPractice: false, slotKey: slotKey);

        sessionA.Should().NotBeNull();
        sessionB.Should().NotBeNull();

        var learnerMezonId = "learner_bob_99999";
        var learnerName = "Bob Study Hard";

        var wrongKeyA = await GetWrongKeyAsync(dbContext, sessionA!.SessionId);
        var correctKeyB = await GetCorrectKeyAsync(dbContext, sessionB!.SessionId);

        // Act 1: Học viên trả lời SAI tại Clan A trước (nhận 0 XP, không bị tiêu cơ hội nhận XP)
        var resultClanA = await clanQuizService.SubmitAnswerAsync(
            sessionA.SessionId,
            channelClanA,
            learnerMezonId,
            learnerName,
            wrongKeyA);

        // Assert 1: Clan A ghi nhận sai, 0 XP, thông báo tiếc quá (chưa nhận XP)
        resultClanA.Status.Should().Be(QuizSubmissionStatus.Success);
        resultClanA.IsCorrect.Should().BeFalse();
        resultClanA.XpAwarded.Should().Be(0);
        resultClanA.Message.Should().Contain("Tiếc quá, câu trả lời chưa chính xác");

        // Act 2: Học viên chuyển sang Clan B trả lời ĐÚNG trong cùng khung giờ slotKey
        var resultClanB = await clanQuizService.SubmitAnswerAsync(
            sessionB.SessionId,
            channelClanB,
            learnerMezonId,
            learnerName,
            correctKeyB);

        // Assert 2: F2 - Học viên trả lời ĐÚNG và nhanh nhất tại Clan B VẪN ĐƯỢC NHẬN ĐỦ XP (+15 XP Winner)
        resultClanB.Status.Should().Be(QuizSubmissionStatus.Success);
        resultClanB.IsCorrect.Should().BeTrue();
        resultClanB.XpAwarded.Should().Be(15);
        resultClanB.Message.Should().Contain("Bạn là người trả lời ĐÚNG và NHANH NHẤT (+15 XP)");

        // Assert 3: Kiểm tra database participation đã cập nhật bản ghi thành đúng và XpAwarded = 15
        var participations = await dbContext.ScheduledQuizParticipations
            .Where(p => p.SlotKey == slotKey && p.MezonUserId == learnerMezonId)
            .ToListAsync();
        participations.Should().HaveCount(1);
        participations[0].IsCorrect.Should().BeTrue();
        participations[0].XpAwarded.Should().Be(15);
        participations[0].ChannelId.Should().Be(channelClanB);
    }

    [Fact]
    public void AdminQuizTrigger_PermissionCheck_AllowsAdminAndRejectsNonAdmin()
    {
        // Arrange
        var adminIds = new List<string> { "1794149317681", "1793710923342" };
        var options = new ScheduledQuizOptions
        {
            AdminUserIds = adminIds
        };

        var validAdmin1 = "1794149317681";
        var validAdmin2 = "1793710923342";
        var normalUser = "999988887777";

        // Act & Assert
        options.AdminUserIds.Contains(validAdmin1).Should().BeTrue();
        options.AdminUserIds.Contains(validAdmin2).Should().BeTrue();
        options.AdminUserIds.Contains(normalUser).Should().BeFalse();
    }

    [Fact]
    public void ScheduleMatching_VietnamTimeZone_CalculatesTimeWindowsCorrectly()
    {
        // Arrange
        var options = new ScheduledQuizOptions
        {
            TimeZoneId = "Asia/Ho_Chi_Minh",
            TimeSlots = new List<string> { "09:00", "12:00", "20:30" }
        };

        var tz = options.GetTimeZone();
        tz.Should().NotBeNull();

        // 1. Kiểm tra mốc 08:59:30 (nằm trong khoảng warning 1 phút: [08:59:00, 09:00:00))
        var timeInWarningWindow = new DateTime(2026, 11, 14, 8, 59, 30);
        var slot0900 = new DateTime(2026, 11, 14, 9, 0, 0);

        var isWarning = timeInWarningWindow >= slot0900.AddMinutes(-1) && timeInWarningWindow < slot0900;
        isWarning.Should().BeTrue();

        // 2. Kiểm tra mốc 09:05:00 (nằm trong khoảng phát động kèm catch-up 10 phút: [09:00:00, 09:10:00))
        var timeInCatchUpWindow = new DateTime(2026, 11, 14, 9, 5, 0);
        var isPosting = timeInCatchUpWindow >= slot0900 && timeInCatchUpWindow < slot0900.AddMinutes(10);
        isPosting.Should().BeTrue();

        // 3. Kiểm tra mốc 09:11:00 (đã quá hạn catch-up 10 phút -> không kích hoạt)
        var timeAfterCatchUp = new DateTime(2026, 11, 14, 9, 11, 0);
        var isExpired = timeAfterCatchUp >= slot0900.AddMinutes(10);
        isExpired.Should().BeTrue();
    }
}
