using System.IO;
using System.Reflection;
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
            content.Should().NotContain("GetProperty(\"Engine\"", $"File {file} không được chứa reflection Engine");
            content.Should().NotContain("ListChannelByUserIdAsync", $"File {file} không được chứa internal engine method ListChannelByUserIdAsync");
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

    private static (Mezon.Net.Sdk.MezonClient client, Mezon.Net.Sdk.Entities.Channel channel, Mezon.Net.Sdk.Caching.EntityCache<Mezon.Net.Sdk.Entities.Message> msgCache) CreateFakeClientAndChannel(long channelId)
    {
        var client = new Mezon.Net.Sdk.MezonClient(new Mezon.Net.Sdk.MezonClientOptions(123456, "dummy-token"));
        var channel = (Mezon.Net.Sdk.Entities.Channel)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Mezon.Net.Sdk.Entities.Channel));
        var descType = typeof(Mezon.Net.Sdk.Entities.Channel).GetField("_desc", BindingFlags.NonPublic | BindingFlags.Instance)!.FieldType;
        var desc = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(descType);
        typeof(Mezon.Net.Sdk.Entities.Channel).GetField("_desc", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(channel, desc);
        typeof(Mezon.Net.Sdk.Entities.Channel).GetField("_client", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(channel, client);

        var msgCache = new Mezon.Net.Sdk.Caching.EntityCache<Mezon.Net.Sdk.Entities.Message>(100);
        typeof(Mezon.Net.Sdk.Entities.Channel).GetField("<Messages>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(channel, msgCache);

        client.Channels.Set(channelId, channel);
        return (client, channel, msgCache);
    }

    [Fact]
    public async Task AddReactionToMessageAsync_MessageAppearsAfterDelay_ReturnsTrueAndRetries()
    {
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(new MezonBotOptions
        {
            BotId = "123456",
            Token = "test-token",
            Enabled = true
        });

        var loggerMock = new Mock<ILogger<MezonBotWorker>>();
        var worker = new MezonBotWorker(loggerMock.Object, options, scopeFactory);

        const long clanId = 10L;
        const long channelId = 100L;
        const long messageId = 200L;
        const string emoji = "🔥";

        var (client, channel, msgCache) = CreateFakeClientAndChannel(channelId);
        worker.SetClientForTesting(client);

        var retryCount = 0;
        worker.RetryDelayProvider = _ =>
        {
            retryCount++;
            if (retryCount == 2)
            {
                // Tin nhắn xuất hiện trong cache sau lần retry thứ 2
                var fakeMsg = (Mezon.Net.Sdk.Entities.Message)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Mezon.Net.Sdk.Entities.Message));
                msgCache.Set(messageId, fakeMsg);
            }
            return Task.CompletedTask;
        };

        var reactionCalled = false;
        worker.MessageReactionSender = (msg, emojiId, reactionEmoji, botId) =>
        {
            reactionCalled = true;
            reactionEmoji.Should().Be(emoji);
            botId.Should().Be(123456L);
            return Task.CompletedTask;
        };

        // Act
        var result = await worker.AddReactionToMessageAsync(clanId, channelId, messageId, emoji);

        // Assert
        result.Should().BeTrue();
        retryCount.Should().Be(2);
        reactionCalled.Should().BeTrue();
        worker.ReactionHistory.Should().Contain(x => x.MessageId == messageId && x.Success);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Đã thêm reaction")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AddReactionToMessageAsync_MessageNeverAppears_ReturnsFalse_LogsWarning_AndDoesNotHang()
    {
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(new MezonBotOptions
        {
            BotId = "123456",
            Token = "test-token",
            Enabled = true
        });

        var loggerMock = new Mock<ILogger<MezonBotWorker>>();
        var worker = new MezonBotWorker(loggerMock.Object, options, scopeFactory);

        const long clanId = 10L;
        const long channelId = 100L;
        const long messageId = 999L;
        const string emoji = "🔥";

        var (client, channel, _) = CreateFakeClientAndChannel(channelId);
        worker.SetClientForTesting(client);

        var retryCount = 0;
        worker.RetryDelayProvider = _ =>
        {
            retryCount++;
            return Task.CompletedTask;
        };

        var reactionCalled = false;
        worker.MessageReactionSender = (msg, emojiId, reactionEmoji, botId) =>
        {
            reactionCalled = true;
            return Task.CompletedTask;
        };

        // Act
        var result = await worker.AddReactionToMessageAsync(clanId, channelId, messageId, emoji);

        // Assert
        result.Should().BeFalse();
        retryCount.Should().Be(4); // 5 attempts: 1st try + 4 delay intervals
        reactionCalled.Should().BeFalse();
        worker.ReactionHistory.Should().Contain(x => x.MessageId == messageId && !x.Success);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("sau 5 lần thử")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Đã thêm reaction")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void OnUserChannelAddedAsync_And_EnsureDmChannelsJoinedAsync_UsePublicSdkApis_AndNoEngineReflection()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var dirInfo = new DirectoryInfo(currentDir);
        while (dirInfo != null && !Directory.Exists(Path.Combine(dirInfo.FullName, "src", "backend")))
        {
            dirInfo = dirInfo.Parent;
        }

        var workerFile = Path.Combine(dirInfo!.FullName, "src", "backend", "Lingual.Modules.MezonBot", "MezonBotWorker.cs");
        File.Exists(workerFile).Should().BeTrue();

        var content = File.ReadAllText(workerFile);

        // 1. OnUserChannelAddedAsync phải dùng Channel.JoinAsync, không chứa reflection Engine, và log Warning khi channel entity null
        var onUserChannelAddedIndex = content.IndexOf("OnUserChannelAddedAsync(UserChannelAddedEventData");
        onUserChannelAddedIndex.Should().BeGreaterThan(0);
        var nextMethodIndex = content.IndexOf("OnChannelMessageReceivedAsync", onUserChannelAddedIndex);
        var methodSnippet = content.Substring(onUserChannelAddedIndex, nextMethodIndex - onUserChannelAddedIndex);

        methodSnippet.Should().NotContain("GetProperty(\"Engine\"", "OnUserChannelAddedAsync không được dùng reflection Engine nữa");
        methodSnippet.Should().Contain("await channel.JoinAsync()", "OnUserChannelAddedAsync phải dùng public API Channel.JoinAsync");
        methodSnippet.Should().Contain("LogWarning", "OnUserChannelAddedAsync phải log Warning khi channel entity null");

        // 2. EnsureDmChannelsJoinedAsync phải dùng public API _client.ListChannelDescsAsync với clanId: 0 và ChannelType.Dm
        var ensureDmIndex = content.IndexOf("EnsureDmChannelsJoinedAsync()");
        ensureDmIndex.Should().BeGreaterThan(0);
        var ensureDmSnippet = content.Substring(ensureDmIndex, onUserChannelAddedIndex - ensureDmIndex);

        ensureDmSnippet.Should().NotContain("GetProperty(\"Engine\"", "EnsureDmChannelsJoinedAsync không được dùng reflection Engine");
        ensureDmSnippet.Should().NotContain("ListChannelByUserIdAsync", "EnsureDmChannelsJoinedAsync không được dùng ListChannelByUserIdAsync");
        ensureDmSnippet.Should().Contain("_client.ListChannelDescsAsync", "EnsureDmChannelsJoinedAsync phải dùng public API ListChannelDescsAsync");
        ensureDmSnippet.Should().Contain("clanId: 0", "EnsureDmChannelsJoinedAsync phải truy vấn clanId: 0 cho kênh DM");
        ensureDmSnippet.Should().Contain("channelType: (int)ChannelType.Dm", "EnsureDmChannelsJoinedAsync phải truyền channelType Dm");
        ensureDmSnippet.Should().Contain("await channel.JoinAsync()", "EnsureDmChannelsJoinedAsync phải dùng public API Channel.JoinAsync");
        ensureDmSnippet.Should().Contain("LogWarning", "EnsureDmChannelsJoinedAsync phải log Warning khi channel entity null");
    }
}
