using System;
using System.Threading;
using System.Threading.Tasks;
using Mezon.Net.Sdk;
using Mezon.Net.Models;
using Mezon.Net.Core;
using Mezon.Net.Client;
using Mezon.Net.Sdk.Builders;

namespace Lingual.Modules.MezonBot.Services;

public interface IMezonBotNotifier
{
    Task<bool> SendSrsReminderAsync(
        Guid userId,
        long channelId,
        long mezonUserId,
        int dueCardsCount,
        int currentStreakDays,
        int freezeBalance,
        CancellationToken cancellationToken = default);

    (long channelId, long mezonUserId)? GetUserChannelMapping(Guid userId);

    Task<ulong?> SendPublicChannelContentAsync(long channelId, MessageContent content, CancellationToken cancellationToken = default);
    Task<bool> SendPublicChannelTextAsync(long channelId, string text, CancellationToken cancellationToken = default);
}

