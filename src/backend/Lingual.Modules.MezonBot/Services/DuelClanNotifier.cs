using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Lingual.Modules.Gamification.Contracts;
using Lingual.Modules.Gamification.Contracts.DTOs;
using Microsoft.Extensions.Logging;

namespace Lingual.Modules.MezonBot.Services;

public class DuelClanNotifier : IDuelClanNotifier
{
    private readonly ILogger<DuelClanNotifier> _logger;
    private static readonly ConcurrentDictionary<Guid, DateTime> PublishedMatches = new();

    public DuelClanNotifier(ILogger<DuelClanNotifier> logger)
    {
        _logger = logger;
    }

    public async Task<bool> PublishMatchHonorCardAsync(
        DuelMatchDto completedMatch,
        long? preferredChannelId = null,
        CancellationToken cancellationToken = default)
    {
        if (completedMatch == null || completedMatch.Id == Guid.Empty) return false;

        // Đảm bảo Idempotency: Mỗi trận đấu chỉ được vinh danh tối đa 1 lần
        if (!PublishedMatches.TryAdd(completedMatch.Id, DateTime.UtcNow))
        {
            _logger.LogInformation("ℹ️ [DuelClanNotifier] Trận đấu {MatchId} đã được vinh danh trước đó. Bỏ qua đăng trùng.", completedMatch.Id);
            return false;
        }

        var targetChannelId = preferredChannelId ?? completedMatch.ClanChannelId ?? MezonBotWorker.LastActiveClanChannelId;
        if (targetChannelId <= 0)
        {
            _logger.LogInformation("ℹ️ [DuelClanNotifier] Chưa xác định được ChannelId Clan cho trận đấu {MatchId}. Đã ghi nhận kết quả.", completedMatch.Id);
            return true;
        }

        var sent = await MezonBotWorker.SendDuelHonorCardToChannelAsync(targetChannelId, completedMatch);
        return sent;
    }
}

