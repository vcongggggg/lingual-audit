using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Mezon.Net.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Lingual.Modules.MezonBot.Tests;

public class PublicSdkApiReplacementTests
{
    [Fact]
    public void GrepBackend_HasZeroOccurrencesOfPrivateReflection()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var dirInfo = new DirectoryInfo(currentDir);
        while (dirInfo != null && !Directory.Exists(Path.Combine(dirInfo.FullName, "src", "backend")))
        {
            dirInfo = dirInfo.Parent;
        }

        dirInfo.Should().NotBeNull("Phải tìm thấy thư mục gốc chứa src/backend");
        var backendDir = Path.Combine(dirInfo!.FullName, "src", "backend");

        var csFiles = Directory.GetFiles(backendDir, "*.cs", SearchOption.AllDirectories);
        csFiles.Should().NotBeEmpty();

        foreach (var file in csFiles)
        {
            var content = File.ReadAllText(file);
            content.Should().NotContain("GetField(\"_engine\")", $"File {file} không được chứa reflection _engine");
            content.Should().NotContain("SendMessageReactionRtAsync", $"File {file} không được chứa reflection SendMessageReactionRtAsync");
            content.Should().NotContain("typeof(SdkClient).GetProperty(\"DmChannels\"", $"File {file} không được chứa reflection DmChannels");
            content.Should().NotContain("GetProperty(\"DmChannels\"", $"File {file} không được chứa reflection DmChannels");
        }
    }

    [Fact]
    public async Task AddReactionToMessageAsync_UsesPublicApi_WhenClientNull_LogsWarning_AndDoesNotLogSuccess()
    {
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(new MezonBotOptions
        {
            BotId = "123456",
            Token = "test-token",
            Enabled = false
        });

        var loggerMock = new Mock<ILogger<MezonBotWorker>>();
        var worker = new MezonBotWorker(loggerMock.Object, options, scopeFactory);

        // Act - Client is null by default in unit test
        var result = await worker.AddReactionToMessageAsync(clanId: 10, channelId: 100, messageId: 200, emoji: "✅");

        // Assert
        result.Should().BeFalse();

        // 1. Không log Information 'Đã thêm reaction'
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Đã thêm reaction")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);

        // 2. Phải log Warning
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Không thể thêm reaction")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SendDirectMessageAsync_UsesPublicSdkUserApi_WhenClientNull_ReturnsFalse()
    {
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(new MezonBotOptions
        {
            BotId = "123456",
            Token = "test-token",
            Enabled = false
        });

        var loggerMock = new Mock<ILogger<MezonBotWorker>>();
        var worker = new MezonBotWorker(loggerMock.Object, options, scopeFactory);

        var content = new Mezon.Net.Sdk.Builders.MessageContentBuilder()
            .SetText("Test DM")
            .Build();

        var result = await worker.SendDirectMessageAsync(123456789L, content);
        result.Should().BeFalse();

        var resultText = await worker.SendDirectMessageTextAsync(123456789L, "Test DM Text");
        resultText.Should().BeFalse();
    }
}
