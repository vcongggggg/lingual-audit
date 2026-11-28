using System.Text.Json;
using Lingual.Modules.AITutor.Contracts;
using Lingual.Modules.Gamification.Contracts;
using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.Learning.Contracts;
using Lingual.Modules.Learning.Contracts.DTOs;
using Lingual.Modules.Learning.Domain;
using Lingual.Modules.Learning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lingual.Modules.Learning.Services;

public class SrsService(
    LearningDbContext dbContext,
    IGamificationService gamificationService,
    ILogger<SrsService> logger,
    IAiQuotaService? aiQuotaService = null) : ISrsService
{
    private static readonly Guid DemoUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private const int StandardReviewXp = 20;

    public async Task<ReviewSubmissionResultDto> SubmitReviewAsync(
        ReviewSubmissionRequestDto request,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        if (!userId.HasValue)
        {
            // Người dùng chưa đăng nhập: chế độ học thử không tích lũy điểm XP hay lưu vào thẻ học cố định
            return new ReviewSubmissionResultDto(Saved: false, Xp: 0, NextDueAt: request.NextDueAt);
        }

        var effectiveUserId = userId.Value;
        var vocabGuid = Guid.Parse(request.VocabularyId!);

        // 1. Cập nhật thẻ nhớ SRS trong CSDL
        try
        {
            var srsCard = await dbContext.SrsCards
                .AsTracking()
                .FirstOrDefaultAsync(s => s.UserId == effectiveUserId && s.VocabularyId == vocabGuid, cancellationToken);

            if (srsCard == null)
            {
                srsCard = new SrsCard
                {
                    UserId = effectiveUserId,
                    VocabularyId = vocabGuid,
                    Repetitions = request.Repetitions,
                    IntervalDays = request.IntervalDays,
                    EaseFactor = request.EaseFactor,
                    NextDueAt = request.NextDueAt.UtcDateTime,
                    LastReviewedAt = DateTime.UtcNow,
                    Lapses = request.Quality < 3 ? 1 : 0,
                    ReviewHistory = JsonSerializer.Serialize(new[]
                    {
                        new
                        {
                            rating = request.Rating,
                            quality = request.Quality,
                            reviewed_at = DateTime.UtcNow,
                            time_spent_ms = request.TimeSpentMs
                        }
                    })
                };
                dbContext.SrsCards.Add(srsCard);
            }
            else
            {
                srsCard.Repetitions = request.Repetitions;
                srsCard.IntervalDays = request.IntervalDays;
                srsCard.EaseFactor = request.EaseFactor;
                srsCard.NextDueAt = request.NextDueAt.UtcDateTime;
                srsCard.LastReviewedAt = DateTime.UtcNow;
                if (request.Quality < 3)
                {
                    srsCard.Lapses += 1;
                }

                var history = JsonSerializer.Deserialize<List<object>>(srsCard.ReviewHistory) ?? new List<object>();
                history.Add(new
                {
                    rating = request.Rating,
                    quality = request.Quality,
                    reviewed_at = DateTime.UtcNow,
                    time_spent_ms = request.TimeSpentMs
                });
                srsCard.ReviewHistory = JsonSerializer.Serialize(history);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Không thể lưu cập nhật thẻ nhớ srs_cards cho user {UserId}, vocab {VocabId}", effectiveUserId, vocabGuid);
        }

        // 2. Thưởng điểm XP idempotent qua GamificationService
        var awardResult = await gamificationService.AwardXpAsync(new AwardXpRequest(
            UserId: effectiveUserId,
            SourceType: "srs_review",
            XpDelta: StandardReviewXp,
            SourceId: vocabGuid,
            IdempotencyKey: request.IdempotencyKey,
            Metadata: null), cancellationToken);

        var awardedXp = awardResult.AlreadyAwarded ? 0 : StandardReviewXp;

        // 3. Thưởng +1 lượt AI khi mốc ôn tập >= 5 thẻ trong ngày (GMT+7), tính 100% từ bảng srs_cards trên server
        if (aiQuotaService != null)
        {
            try
            {
                var (todayVn, todayStartUtc) = GetVietnamTodayBoundsUtc();
                var reviewedTodayCount = await dbContext.SrsCards
                    .AsNoTracking()
                    .CountAsync(s => s.UserId == effectiveUserId && s.LastReviewedAt != null && s.LastReviewedAt >= todayStartUtc, cancellationToken);

                if (reviewedTodayCount >= 5)
                {
                    var milestone = reviewedTodayCount / 5;
                    await aiQuotaService.GrantBonusCreditsAsync(
                        userId: effectiveUserId,
                        amount: 1,
                        reason: "reward_flashcard",
                        idempotencyKey: $"reward_flashcard_{effectiveUserId:N}_{todayVn:yyyyMMdd}_m{milestone}",
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Không thể cộng lượt AI thưởng sau ôn tập SRS cho user {UserId}", effectiveUserId);
            }
        }

        return new ReviewSubmissionResultDto(Saved: true, Xp: awardedXp, NextDueAt: request.NextDueAt);
    }

    private static (DateOnly TodayVn, DateTime TodayStartUtc) GetVietnamTodayBoundsUtc()
    {
        TimeZoneInfo tz;
        try { tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
        catch (TimeZoneNotFoundException)
        {
            try { tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }
            catch (TimeZoneNotFoundException) { tz = TimeZoneInfo.CreateCustomTimeZone("Asia/Ho_Chi_Minh", TimeSpan.FromHours(7), "ICT", "ICT"); }
        }
        var vnNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        var todayVn = DateOnly.FromDateTime(vnNow);
        var vnStart = todayVn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return (todayVn, TimeZoneInfo.ConvertTimeToUtc(vnStart, tz));
    }

    public async Task<SrsDueCardsResponseDto> GetDueCardsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        try
        {
            var dueCards = await dbContext.SrsCards
                .AsNoTracking()
                .Include(s => s.VocabularyItem)
                .Where(s => s.UserId == userId && s.NextDueAt <= now && s.SuspendedAt == null)
                .OrderBy(s => s.NextDueAt)
                .Take(50)
                .Select(s => new SrsDueCardDto(
                    s.VocabularyId,
                    s.VocabularyItem != null ? s.VocabularyItem.Term : string.Empty,
                    s.VocabularyItem != null ? s.VocabularyItem.Meaning : string.Empty,
                    s.VocabularyItem != null ? s.VocabularyItem.Ipa : null,
                    s.Repetitions,
                    s.IntervalDays,
                    s.EaseFactor,
                    s.NextDueAt))
                .ToListAsync(cancellationToken);

            return new SrsDueCardsResponseDto(dueCards.Count, dueCards);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi lấy danh sách thẻ đến hạn ôn tập của user {UserId}", userId);
            return new SrsDueCardsResponseDto(0, Array.Empty<SrsDueCardDto>());
        }
    }
}
