using System;
using System.Threading;
using System.Threading.Tasks;

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
}

