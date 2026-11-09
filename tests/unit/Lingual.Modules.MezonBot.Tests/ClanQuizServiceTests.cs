using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Lingual.Modules.MezonBot.Domain;
using Lingual.Modules.MezonBot.Infrastructure.Data;
using Lingual.Modules.MezonBot.Services;
using Xunit;

namespace Lingual.Modules.MezonBot.Tests;

public class ClanQuizServiceTests
{
    private MezonBotDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<MezonBotDbContext>()
            .UseInMemoryDatabase(databaseName: $"MezonBot_Test_{Guid.NewGuid()}")
            .Options;

        return new MezonBotDbContext(options);
    }

    [Fact]
    public async Task StartSessionAsync_CreatesOpenSession_WithOptions()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var loggerMock = new Mock<ILogger<ClanQuizService>>();
        var service = new ClanQuizService(dbContext, loggerMock.Object);

        var channelId = 123456789L;

        // Act
        var result = await service.StartSessionAsync(channelId, "user_1", 30);

        // Assert
        result.Should().NotBeNull();
        result!.ChannelId.Should().Be(channelId);
        result.DurationSeconds.Should().Be(30);
        result.Options.Should().HaveCount(4);
        result.QuestionPrompt.Should().NotBeNullOrWhiteSpace();

        var inDb = await dbContext.ClanQuizSessions.FirstOrDefaultAsync(s => s.Id == result.SessionId);
        inDb.Should().NotBeNull();
        inDb!.Status.Should().Be("open");
        inDb.ChannelMezonId.Should().Be(channelId.ToString());
    }

    [Fact]
    public async Task HasActiveSessionAsync_ReturnsTrue_WhenSessionIsOpen()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var loggerMock = new Mock<ILogger<ClanQuizService>>();
        var service = new ClanQuizService(dbContext, loggerMock.Object);

        var channelId = 987654321L;
        await service.StartSessionAsync(channelId, "user_2", 30);

        // Act
        var isActive = await service.HasActiveSessionAsync(channelId);

        // Assert
        isActive.Should().BeTrue();
    }

    [Fact]
    public async Task StartSessionAsync_ReturnsNull_WhenActiveSessionAlreadyExists()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var loggerMock = new Mock<ILogger<ClanQuizService>>();
        var service = new ClanQuizService(dbContext, loggerMock.Object);

        var channelId = 555666777L;
        var firstSession = await service.StartSessionAsync(channelId, "user_1", 30);
        firstSession.Should().NotBeNull();

        // Act
        var secondSession = await service.StartSessionAsync(channelId, "user_2", 30);

        // Assert
        secondSession.Should().BeNull();
    }

    [Fact]
    public async Task ConcludeSessionAsync_CalculatesCorrectWinnerAndStats()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var loggerMock = new Mock<ILogger<ClanQuizService>>();
        var service = new ClanQuizService(dbContext, loggerMock.Object);

        var channelId = 111222333L;
        var sessionDto = await service.StartSessionAsync(channelId, "user_1", 30);
        sessionDto.Should().NotBeNull();

        // Giả lập dữ liệu responses trong DB
        var session = await dbContext.ClanQuizSessions.FirstAsync(s => s.Id == sessionDto!.SessionId);
        session.Responses = @"[
            {""user_id"":""00000000-0000-0000-0000-000000000001"",""mezon_user_id"":""mz_1"",""username"":""Player1"",""selected_key"":""A"",""is_correct"":false,""response_ms"":2100,""responded_at"":""2026-10-29T12:00:00Z""},
            {""user_id"":""00000000-0000-0000-0000-000000000002"",""mezon_user_id"":""mz_2"",""username"":""Player2"",""selected_key"":""C"",""is_correct"":true,""response_ms"":1450,""responded_at"":""2026-10-29T12:00:01Z""},
            {""user_id"":""00000000-0000-0000-0000-000000000003"",""mezon_user_id"":""mz_3"",""username"":""Player3"",""selected_key"":""C"",""is_correct"":true,""response_ms"":2900,""responded_at"":""2026-10-29T12:00:02Z""}
        ]";
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.ConcludeSessionAsync(sessionDto!.SessionId);

        // Assert
        result.Should().NotBeNull();
        result!.TotalResponses.Should().Be(3);
        result.CorrectResponses.Should().Be(2);
        result.WinnerUsername.Should().Be("Player2");
        result.WinningResponseMs.Should().Be(1450);
        result.CorrectKey.Should().Be("C");
        result.CorrectText.Should().Be("agree");
        result.Explanation.Should().Contain("agree - đồng ý");

        // Verify status in DB is closed
        var updatedInDb = await dbContext.ClanQuizSessions.FirstAsync(s => s.Id == sessionDto.SessionId);
        updatedInDb.Status.Should().Be("closed");
    }

    [Fact]
    public async Task SubmitAnswerAsync_ReturnsSessionNotFound_WhenInvalidSessionId()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var loggerMock = new Mock<ILogger<ClanQuizService>>();
        var service = new ClanQuizService(dbContext, loggerMock.Object);

        // Act
        var result = await service.SubmitAnswerAsync(Guid.NewGuid(), 123L, "user_1", "User One", "A");

        // Assert
        result.Status.Should().Be(QuizSubmissionStatus.SessionNotFound);
    }

    [Fact]
    public async Task SubmitAnswerAsync_ReturnsSessionExpired_WhenSessionClosed()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var loggerMock = new Mock<ILogger<ClanQuizService>>();
        var service = new ClanQuizService(dbContext, loggerMock.Object);

        var channelId = 444555666L;
        var sessionDto = await service.StartSessionAsync(channelId, "user_1", 30);
        sessionDto.Should().NotBeNull();

        // Đóng session
        var session = await dbContext.ClanQuizSessions.FirstAsync(s => s.Id == sessionDto!.SessionId);
        session.Status = "closed";
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.SubmitAnswerAsync(sessionDto!.SessionId, channelId, "user_1", "User One", "A");

        // Assert
        result.Status.Should().Be(QuizSubmissionStatus.SessionExpired);
    }

    [Fact]
    public async Task StartSessionAsync_PracticeMode_StoresDmPrefixAndIsolatesSession()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var loggerMock = new Mock<ILogger<ClanQuizService>>();
        var service = new ClanQuizService(dbContext, loggerMock.Object);
        var dmChannelId = 999888777L;

        // Act - Bắt đầu session trong DM (isPractice = true)
        var sessionDto = await service.StartSessionAsync(dmChannelId, "user_dm", 30, isPractice: true);

        // Assert
        sessionDto.Should().NotBeNull();
        sessionDto!.IsPractice.Should().BeTrue();

        var dbSession = await dbContext.ClanQuizSessions.FirstAsync(s => s.Id == sessionDto.SessionId);
        dbSession.ChannelMezonId.Should().Be($"dm_{dmChannelId}");

        // Isolate check: DM active, Clan active should be false
        var isDmActive = await service.HasActiveSessionAsync(dmChannelId, isPractice: true);
        var isClanActive = await service.HasActiveSessionAsync(dmChannelId, isPractice: false);
        isDmActive.Should().BeTrue();
        isClanActive.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitAnswerAsync_PracticeMode_DoesNotAwardXp()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var loggerMock = new Mock<ILogger<ClanQuizService>>();
        var service = new ClanQuizService(dbContext, loggerMock.Object);
        var dmChannelId = 999888777L;

        var sessionDto = await service.StartSessionAsync(dmChannelId, "user_dm", 30, isPractice: true);
        sessionDto.Should().NotBeNull();

        // Act - Nộp đáp án trong DM
        var result = await service.SubmitAnswerAsync(sessionDto!.SessionId, dmChannelId, "user_dm", "Learner", "C");

        // Assert
        result.Status.Should().Be(QuizSubmissionStatus.Success);
        result.IsPractice.Should().BeTrue();
        result.XpAwarded.Should().Be(0);
        result.IsCorrect.Should().BeTrue();

        // Conclude session
        var conclude = await service.ConcludeSessionAsync(sessionDto.SessionId);
        conclude.Should().NotBeNull();
        conclude!.IsPractice.Should().BeTrue();
    }
}
