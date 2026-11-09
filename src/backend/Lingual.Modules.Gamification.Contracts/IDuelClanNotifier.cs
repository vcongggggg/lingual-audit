using System.Threading;
using System.Threading.Tasks;
using Lingual.Modules.Gamification.Contracts.DTOs;

namespace Lingual.Modules.Gamification.Contracts;

/// <summary>
/// Giao diện thông báo kết quả trận đấu Word Duel 1vs1 ra kênh Clan Mezon (SP3-04).
/// Đảm bảo tính chất Idempotent (không đăng trùng thẻ vinh danh cho cùng một trận đấu).
/// </summary>
public interface IDuelClanNotifier
{
    Task<bool> PublishMatchHonorCardAsync(
        DuelMatchDto completedMatch,
        long? preferredChannelId = null,
        CancellationToken cancellationToken = default);
}

