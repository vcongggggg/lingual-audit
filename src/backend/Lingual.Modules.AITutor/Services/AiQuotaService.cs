using System.Collections.Concurrent;
using Lingual.Modules.AITutor.Contracts;
using Lingual.Modules.AITutor.Contracts.DTOs;
using Lingual.Modules.AITutor.Domain;
using Lingual.Modules.AITutor.Infrastructure.Data;
using Lingual.Modules.AITutor.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Lingual.Modules.AITutor.Services;

public class AiQuotaService : IAiQuotaService
{
    public const string QuotaExceededCode = "AI_QUOTA_EXCEEDED";

    private static readonly Lazy<TimeZoneInfo> VietnamTimeZoneLazy = new(() =>
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.CreateCustomTimeZone("Asia/Ho_Chi_Minh", TimeSpan.FromHours(7), "ICT", "ICT");
            }
        }
    });

    // Per-user & per-IP locks for InMemory EF Core / IMemoryCache atomic sections
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> InMemoryUserLocks = new();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> GuestIpLocks = new(StringComparer.OrdinalIgnoreCase);

    private readonly AITutorDbContext _dbContext;
    private readonly bool _isRelational;
    private readonly IMemoryCache _memoryCache;
    private readonly GeminiOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AiQuotaService> _logger;

    public AiQuotaService(
        AITutorDbContext dbContext,
        IMemoryCache memoryCache,
        IOptions<GeminiOptions> options,
        TimeProvider timeProvider,
        ILogger<AiQuotaService> logger)
    {
        _dbContext = dbContext;
        _isRelational = dbContext.Database.IsRelational();
        _memoryCache = memoryCache;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    private int DailyFreeLimit => Math.Max(1, _options.DailyFreeTurns);
    private int DailyBonusCap => Math.Max(0, _options.DailyBonusCap);
    private int GuestDailyLimit => Math.Max(1, _options.GuestDailyTurns);

    public static DateOnly GetVietnamDate(DateTime utcNow)
    {
        var vnNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), VietnamTimeZoneLazy.Value);
        return DateOnly.FromDateTime(vnNow);
    }

    public static DateTime GetNextVietnamMidnightUtc(DateTime utcNow)
    {
        var vnToday = GetVietnamDate(utcNow);
        var nextVnMidnight = vnToday.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(nextVnMidnight, VietnamTimeZoneLazy.Value);
    }

    public async Task<AiQuotaStatusDto> GetQuotaAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var today = GetVietnamDate(nowUtc);
        var nextResetUtc = GetNextVietnamMidnightUtc(nowUtc);

        if (userId == Guid.Empty)
        {
            return BuildStatus(DailyFreeLimit, DailyFreeLimit, 0, 0, nextResetUtc, isGuest: false);
        }

        if (_isRelational)
        {
            var connStr = _dbContext.Database.GetConnectionString()
                ?? throw new InvalidOperationException("AITutor database connection string is missing.");

            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync(cancellationToken);

            await EnsureWalletRelationalAsync(conn, null, userId, today, nowUtc, cancellationToken);
            return await ReadWalletStatusRelationalAsync(conn, null, userId, today, nextResetUtc, cancellationToken);
        }

        var userLock = InMemoryUserLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        await userLock.WaitAsync(cancellationToken);
        try
        {
            var wallet = await GetOrCreateWalletInMemoryAsync(userId, today, nowUtc, cancellationToken);
            var (effFree, effBonusEarned) = EffectiveDailyValues(wallet, today);
            return BuildStatus(DailyFreeLimit, effFree, wallet.Balance, effBonusEarned, nextResetUtc, isGuest: false);
        }
        finally
        {
            userLock.Release();
        }
    }

    public async Task<AiQuotaConsumeResult> TryConsumeTurnAsync(
        Guid userId,
        string reason,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId không hợp lệ.", nameof(userId));
        }

        var effectiveKey = string.IsNullOrWhiteSpace(idempotencyKey)
            ? $"consume_{userId:N}_{Guid.NewGuid():N}"
            : idempotencyKey.Trim();
        var effectiveReason = string.IsNullOrWhiteSpace(reason) ? "consume_ai" : reason.Trim();
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var today = GetVietnamDate(nowUtc);
        var nextResetUtc = GetNextVietnamMidnightUtc(nowUtc);

        if (_isRelational)
        {
            return await TryConsumeRelationalAsync(userId, effectiveReason, effectiveKey, today, nowUtc, nextResetUtc, cancellationToken);
        }

        return await TryConsumeInMemoryAsync(userId, effectiveReason, effectiveKey, today, nowUtc, nextResetUtc, cancellationToken);
    }

    public async Task<AiQuotaStatusDto> RefundTurnAsync(
        Guid userId,
        bool consumedFromDailyFree,
        string originalIdempotencyKey,
        string reason = "refund_gemini_error",
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return await GetQuotaAsync(userId, cancellationToken);
        }

        var refundKey = string.IsNullOrWhiteSpace(originalIdempotencyKey)
            ? $"refund_{userId:N}_{Guid.NewGuid():N}"
            : $"refund_{originalIdempotencyKey.Trim()}";
        if (refundKey.Length > 160)
        {
            refundKey = refundKey[..160];
        }

        var effectiveReason = string.IsNullOrWhiteSpace(reason) ? "refund_gemini_error" : reason.Trim();
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var today = GetVietnamDate(nowUtc);
        var nextResetUtc = GetNextVietnamMidnightUtc(nowUtc);

        if (_isRelational)
        {
            var connStr = _dbContext.Database.GetConnectionString()
                ?? throw new InvalidOperationException("AITutor database connection string is missing.");

            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync(cancellationToken);

            // Auto-commit wallet creation first so concurrent transactions never deadlock on parent INSERT + FK lock
            await EnsureWalletRelationalAsync(conn, null, userId, today, nowUtc, cancellationToken);

            await using var tx = await conn.BeginTransactionAsync(cancellationToken);

            // Lock parent wallet row FIRST (FOR UPDATE) before inserting child FK row to prevent FOR KEY SHARE -> FOR UPDATE deadlock
            await LockWalletRowForUpdateAsync(conn, tx, userId, cancellationToken);

            // Idempotent refund transaction insert
            var inserted = await TryInsertTransactionRelationalAsync(
                conn, tx, userId, amount: 1, effectiveReason, refundKey, nowUtc, cancellationToken);

            if (inserted)
            {
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                if (consumedFromDailyFree)
                {
                    cmd.CommandText = @"
                        UPDATE ai_credit_wallets
                        SET daily_free_remaining = CASE
                                WHEN last_reset_date = @today THEN LEAST(@dailyLimit, daily_free_remaining + 1)
                                ELSE @dailyLimit
                            END,
                            daily_bonus_earned = CASE
                                WHEN last_reset_date < @today THEN 0
                                ELSE daily_bonus_earned
                            END,
                            last_reset_date = @today,
                            updated_at = @nowUtc
                        WHERE user_id = @userId;";
                }
                else
                {
                    cmd.CommandText = @"
                        UPDATE ai_credit_wallets
                        SET balance = balance + 1,
                            updated_at = @nowUtc
                        WHERE user_id = @userId;";
                }

                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@today", today);
                cmd.Parameters.AddWithValue("@dailyLimit", DailyFreeLimit);
                cmd.Parameters.AddWithValue("@nowUtc", nowUtc);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
            return await ReadWalletStatusRelationalAsync(conn, null, userId, today, nextResetUtc, cancellationToken);
        }

        var userLock = InMemoryUserLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        await userLock.WaitAsync(cancellationToken);
        try
        {
            var wallet = await GetOrCreateWalletInMemoryAsync(userId, today, nowUtc, cancellationToken);
            var exists = await _dbContext.AiCreditTransactions
                .AsNoTracking()
                .AnyAsync(t => t.IdempotencyKey == refundKey, cancellationToken);

            if (!exists)
            {
                ApplyDailyResetIfNeeded(wallet, today);
                if (consumedFromDailyFree)
                {
                    wallet.DailyFreeRemaining = Math.Min(DailyFreeLimit, wallet.DailyFreeRemaining + 1);
                }
                else
                {
                    wallet.Balance += 1;
                }
                wallet.UpdatedAt = nowUtc;
                _dbContext.AiCreditWallets.Update(wallet);

                _dbContext.AiCreditTransactions.Add(new AiCreditTransaction
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Amount = 1,
                    Reason = effectiveReason,
                    IdempotencyKey = refundKey,
                    CreatedAt = nowUtc
                });
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var (effFree, effBonusEarned) = EffectiveDailyValues(wallet, today);
            return BuildStatus(DailyFreeLimit, effFree, wallet.Balance, effBonusEarned, nextResetUtc, isGuest: false);
        }
        finally
        {
            userLock.Release();
        }
    }

    public async Task<AiQuotaGrantResult> GrantBonusCreditsAsync(
        Guid userId,
        int amount,
        string reason,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId không hợp lệ.", nameof(userId));
        }

        if (amount <= 0)
        {
            var status = await GetQuotaAsync(userId, cancellationToken);
            return new AiQuotaGrantResult(false, 0, false, false, status);
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException("IdempotencyKey là bắt buộc khi cộng lượt thưởng.", nameof(idempotencyKey));
        }

        var cleanKey = idempotencyKey.Trim();
        if (cleanKey.Length > 160)
        {
            cleanKey = cleanKey[..160];
        }

        var cleanReason = string.IsNullOrWhiteSpace(reason) ? "reward_bonus" : reason.Trim();
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var today = GetVietnamDate(nowUtc);
        var nextResetUtc = GetNextVietnamMidnightUtc(nowUtc);

        if (_isRelational)
        {
            return await GrantBonusRelationalAsync(userId, amount, cleanReason, cleanKey, today, nowUtc, nextResetUtc, cancellationToken);
        }

        return await GrantBonusInMemoryAsync(userId, amount, cleanReason, cleanKey, today, nowUtc, nextResetUtc, cancellationToken);
    }

    public Task<AiQuotaStatusDto> GetGuestQuotaAsync(
        string clientIp,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var today = GetVietnamDate(nowUtc);
        var nextResetUtc = GetNextVietnamMidnightUtc(nowUtc);
        var key = BuildGuestCacheKey(clientIp, today);

        var used = _memoryCache.TryGetValue<int>(key, out var val) ? Math.Clamp(val, 0, GuestDailyLimit) : 0;
        var remaining = Math.Max(0, GuestDailyLimit - used);

        return Task.FromResult(BuildStatus(GuestDailyLimit, remaining, 0, 0, nextResetUtc, isGuest: true));
    }

    public async Task<AiQuotaConsumeResult> TryConsumeGuestTurnAsync(
        string clientIp,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var today = GetVietnamDate(nowUtc);
        var nextResetUtc = GetNextVietnamMidnightUtc(nowUtc);
        var normIp = NormalizeIp(clientIp);
        var cacheKey = BuildGuestCacheKey(normIp, today);
        var ipLock = GuestIpLocks.GetOrAdd(normIp, _ => new SemaphoreSlim(1, 1));

        await ipLock.WaitAsync(cancellationToken);
        try
        {
            var used = _memoryCache.TryGetValue<int>(cacheKey, out var val) ? Math.Max(0, val) : 0;
            if (used >= GuestDailyLimit)
            {
                var exhausted = BuildStatus(GuestDailyLimit, 0, 0, 0, nextResetUtc, isGuest: true);
                return new AiQuotaConsumeResult(
                    Allowed: false,
                    AlreadyProcessed: false,
                    ConsumedFromDailyFree: false,
                    IdempotencyKey: $"guest_{normIp}_{today:yyyyMMdd}_{used}",
                    ErrorCode: QuotaExceededCode,
                    Message: $"Bạn đã dùng hết {GuestDailyLimit} lượt trải nghiệm LingLing AI miễn phí hôm nay. Đăng nhập bằng Mezon để nhận {DailyFreeLimit} lượt miễn phí mỗi ngày và kiếm thêm lượt từ Game/Quiz/Flashcard!",
                    Quota: exhausted);
            }

            var nextUsed = used + 1;
            var ttl = nextResetUtc - nowUtc;
            if (ttl < TimeSpan.FromSeconds(5))
            {
                ttl = TimeSpan.FromSeconds(5);
            }

            _memoryCache.Set(cacheKey, nextUsed, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            });

            var remaining = Math.Max(0, GuestDailyLimit - nextUsed);
            var status = BuildStatus(GuestDailyLimit, remaining, 0, 0, nextResetUtc, isGuest: true);
            return new AiQuotaConsumeResult(
                Allowed: true,
                AlreadyProcessed: false,
                ConsumedFromDailyFree: true,
                IdempotencyKey: $"guest_{normIp}_{today:yyyyMMdd}_{nextUsed}",
                ErrorCode: null,
                Message: null,
                Quota: status);
        }
        finally
        {
            ipLock.Release();
        }
    }

    public async Task<AiQuotaStatusDto> RefundGuestTurnAsync(
        string clientIp,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var today = GetVietnamDate(nowUtc);
        var nextResetUtc = GetNextVietnamMidnightUtc(nowUtc);
        var normIp = NormalizeIp(clientIp);
        var cacheKey = BuildGuestCacheKey(normIp, today);
        var ipLock = GuestIpLocks.GetOrAdd(normIp, _ => new SemaphoreSlim(1, 1));

        await ipLock.WaitAsync(cancellationToken);
        try
        {
            var used = _memoryCache.TryGetValue<int>(cacheKey, out var val) ? Math.Max(0, val) : 0;
            var nextUsed = Math.Max(0, used - 1);
            var ttl = nextResetUtc - nowUtc;
            if (ttl < TimeSpan.FromSeconds(5))
            {
                ttl = TimeSpan.FromSeconds(5);
            }
            _memoryCache.Set(cacheKey, nextUsed, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            });

            var remaining = Math.Max(0, GuestDailyLimit - nextUsed);
            return BuildStatus(GuestDailyLimit, remaining, 0, 0, nextResetUtc, isGuest: true);
        }
        finally
        {
            ipLock.Release();
        }
    }

    // =====================================================
    // RELATIONAL (POSTGRESQL) ATOMIC SQL IMPLEMENTATIONS
    // =====================================================

    private async Task<AiQuotaConsumeResult> TryConsumeRelationalAsync(
        Guid userId,
        string reason,
        string idempotencyKey,
        DateOnly today,
        DateTime nowUtc,
        DateTime nextResetUtc,
        CancellationToken cancellationToken)
    {
        var connStr = _dbContext.Database.GetConnectionString()
            ?? throw new InvalidOperationException("AITutor database connection string is missing.");

        await using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync(cancellationToken);

        // Ensure wallet exists outside tx so concurrent first-time calls never deadlock on parent PK creation
        await EnsureWalletRelationalAsync(conn, null, userId, today, nowUtc, cancellationToken);

        await using var tx = await conn.BeginTransactionAsync(cancellationToken);

        // Acquire exclusive FOR UPDATE lock on parent wallet row BEFORE inserting FK child row (prevents 40P01 deadlock)
        await LockWalletRowForUpdateAsync(conn, tx, userId, cancellationToken);

        // 1. Idempotency guard via UNIQUE constraint on ai_credit_transactions.idempotency_key
        var inserted = await TryInsertTransactionRelationalAsync(
            conn, tx, userId, amount: -1, reason, idempotencyKey, nowUtc, cancellationToken);

        if (!inserted)
        {
            await tx.CommitAsync(cancellationToken);
            var existingStatus = await ReadWalletStatusRelationalAsync(conn, null, userId, today, nextResetUtc, cancellationToken);
            return new AiQuotaConsumeResult(
                Allowed: true,
                AlreadyProcessed: true,
                ConsumedFromDailyFree: false,
                IdempotencyKey: idempotencyKey,
                ErrorCode: null,
                Message: null,
                Quota: existingStatus);
        }

        // 2. Single atomic SQL UPDATE with row lock + daily GMT+7 reset + non-negative check
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
            WITH old_w AS (
                SELECT user_id, daily_free_remaining AS old_free, balance AS old_bal, last_reset_date AS old_date
                FROM ai_credit_wallets
                WHERE user_id = @userId
                FOR UPDATE
            )
            UPDATE ai_credit_wallets w
            SET
                daily_free_remaining = CASE
                    WHEN old_w.old_date < @today THEN @dailyLimit - 1
                    WHEN old_w.old_free > 0 THEN old_w.old_free - 1
                    ELSE old_w.old_free
                END,
                balance = CASE
                    WHEN old_w.old_date < @today THEN old_w.old_bal
                    WHEN old_w.old_free > 0 THEN old_w.old_bal
                    ELSE old_w.old_bal - 1
                END,
                daily_bonus_earned = CASE
                    WHEN old_w.old_date < @today THEN 0
                    ELSE w.daily_bonus_earned
                END,
                last_reset_date = @today,
                updated_at = @nowUtc
            FROM old_w
            WHERE w.user_id = old_w.user_id
              AND (old_w.old_date < @today OR old_w.old_free > 0 OR old_w.old_bal > 0)
            RETURNING
                w.daily_free_remaining,
                w.balance,
                w.daily_bonus_earned,
                (old_w.old_date < @today OR old_w.old_free > 0) AS used_free;";

        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@today", today);
        cmd.Parameters.AddWithValue("@dailyLimit", DailyFreeLimit);
        cmd.Parameters.AddWithValue("@nowUtc", nowUtc);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var freeRemaining = reader.GetInt32(0);
            var balance = reader.GetInt32(1);
            var bonusEarned = reader.GetInt32(2);
            var usedFree = reader.GetBoolean(3);
            await reader.CloseAsync();

            await tx.CommitAsync(cancellationToken);
            var status = BuildStatus(DailyFreeLimit, freeRemaining, balance, bonusEarned, nextResetUtc, isGuest: false);
            return new AiQuotaConsumeResult(
                Allowed: true,
                AlreadyProcessed: false,
                ConsumedFromDailyFree: usedFree,
                IdempotencyKey: idempotencyKey,
                ErrorCode: null,
                Message: null,
                Quota: status);
        }

        await reader.CloseAsync();
        await tx.RollbackAsync(cancellationToken);

        var exhaustedStatus = await ReadWalletStatusRelationalAsync(conn, null, userId, today, nextResetUtc, cancellationToken);
        return new AiQuotaConsumeResult(
            Allowed: false,
            AlreadyProcessed: false,
            ConsumedFromDailyFree: false,
            IdempotencyKey: idempotencyKey,
            ErrorCode: QuotaExceededCode,
            Message: "Bạn đã dùng hết lượt hỏi LingLing AI hôm nay. Hãy chơi Game Center (đạt >=50%), làm Quiz (đạt >=60%) hoặc ôn 5 thẻ Flashcard để kiếm thêm lượt thưởng, hoặc quay lại lúc 00:00 (GMT+7) nhé!",
            Quota: exhaustedStatus);
    }

    private async Task<AiQuotaGrantResult> GrantBonusRelationalAsync(
        Guid userId,
        int amount,
        string reason,
        string idempotencyKey,
        DateOnly today,
        DateTime nowUtc,
        DateTime nextResetUtc,
        CancellationToken cancellationToken)
    {
        var connStr = _dbContext.Database.GetConnectionString()
            ?? throw new InvalidOperationException("AITutor database connection string is missing.");

        await using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync(cancellationToken);

        await EnsureWalletRelationalAsync(conn, null, userId, today, nowUtc, cancellationToken);

        await using var tx = await conn.BeginTransactionAsync(cancellationToken);

        // Acquire exclusive FOR UPDATE lock on parent wallet row BEFORE inserting FK child row (prevents 40P01 deadlock)
        await LockWalletRowForUpdateAsync(conn, tx, userId, cancellationToken);

        var txId = Guid.NewGuid();
        var inserted = await TryInsertTransactionRelationalAsync(
            conn, tx, userId, amount, reason, idempotencyKey, nowUtc, cancellationToken, txId);

        if (!inserted)
        {
            await tx.CommitAsync(cancellationToken);
            var current = await ReadWalletStatusRelationalAsync(conn, null, userId, today, nextResetUtc, cancellationToken);
            return new AiQuotaGrantResult(
                Granted: false,
                CreditsAdded: 0,
                AlreadyGranted: true,
                DailyCapReached: current.DailyBonusEarned >= DailyBonusCap,
                Quota: current);
        }

        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
            WITH old_w AS (
                SELECT user_id, balance AS old_bal, daily_free_remaining AS old_free, daily_bonus_earned AS old_bonus, last_reset_date AS old_date
                FROM ai_credit_wallets
                WHERE user_id = @userId
                FOR UPDATE
            ),
            calc AS (
                SELECT
                    user_id,
                    old_bal,
                    CASE WHEN old_date < @today THEN @dailyLimit ELSE old_free END AS eff_free,
                    CASE WHEN old_date < @today THEN 0 ELSE old_bonus END AS eff_bonus
                FROM old_w
            )
            UPDATE ai_credit_wallets w
            SET
                daily_free_remaining = calc.eff_free,
                daily_bonus_earned = calc.eff_bonus + LEAST(@amount, GREATEST(0, @dailyBonusCap - calc.eff_bonus)),
                balance = calc.old_bal + LEAST(@amount, GREATEST(0, @dailyBonusCap - calc.eff_bonus)),
                last_reset_date = @today,
                updated_at = @nowUtc
            FROM calc
            WHERE w.user_id = calc.user_id
              AND calc.eff_bonus < @dailyBonusCap
            RETURNING
                w.daily_free_remaining,
                w.balance,
                w.daily_bonus_earned,
                (w.balance - calc.old_bal) AS added;";

        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@today", today);
        cmd.Parameters.AddWithValue("@dailyLimit", DailyFreeLimit);
        cmd.Parameters.AddWithValue("@dailyBonusCap", DailyBonusCap);
        cmd.Parameters.AddWithValue("@amount", amount);
        cmd.Parameters.AddWithValue("@nowUtc", nowUtc);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var freeRemaining = reader.GetInt32(0);
            var balance = reader.GetInt32(1);
            var bonusEarned = reader.GetInt32(2);
            var added = reader.GetInt32(3);
            await reader.CloseAsync();

            if (added != amount)
            {
                await using var fixCmd = conn.CreateCommand();
                fixCmd.Transaction = tx;
                fixCmd.CommandText = "UPDATE ai_credit_transactions SET amount = @added WHERE id = @txId;";
                fixCmd.Parameters.AddWithValue("@added", added);
                fixCmd.Parameters.AddWithValue("@txId", txId);
                await fixCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
            var status = BuildStatus(DailyFreeLimit, freeRemaining, balance, bonusEarned, nextResetUtc, isGuest: false);
            return new AiQuotaGrantResult(
                Granted: added > 0,
                CreditsAdded: added,
                AlreadyGranted: false,
                DailyCapReached: bonusEarned >= DailyBonusCap,
                Quota: status);
        }

        await reader.CloseAsync();
        await tx.RollbackAsync(cancellationToken);

        var capStatus = await ReadWalletStatusRelationalAsync(conn, null, userId, today, nextResetUtc, cancellationToken);
        return new AiQuotaGrantResult(
            Granted: false,
            CreditsAdded: 0,
            AlreadyGranted: false,
            DailyCapReached: true,
            Quota: capStatus);
    }

    private static async Task LockWalletRowForUpdateAsync(
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT 1 FROM ai_credit_wallets WHERE user_id = @userId FOR UPDATE;";
        cmd.Parameters.AddWithValue("@userId", userId);
        await cmd.ExecuteScalarAsync(cancellationToken);
    }

    private async Task EnsureWalletRelationalAsync(
        NpgsqlConnection conn,
        NpgsqlTransaction? tx,
        Guid userId,
        DateOnly today,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
            INSERT INTO ai_credit_wallets (user_id, balance, daily_free_remaining, last_reset_date, daily_bonus_earned, created_at, updated_at)
            VALUES (@userId, 0, @dailyLimit, @today, 0, @nowUtc, @nowUtc)
            ON CONFLICT (user_id) DO NOTHING;";
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@dailyLimit", DailyFreeLimit);
        cmd.Parameters.AddWithValue("@today", today);
        cmd.Parameters.AddWithValue("@nowUtc", nowUtc);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> TryInsertTransactionRelationalAsync(
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        Guid userId,
        int amount,
        string reason,
        string idempotencyKey,
        DateTime nowUtc,
        CancellationToken cancellationToken,
        Guid? explicitId = null)
    {
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
            INSERT INTO ai_credit_transactions (id, user_id, amount, reason, idempotency_key, created_at)
            VALUES (@id, @userId, @amount, @reason, @key, @nowUtc)
            ON CONFLICT (idempotency_key) DO NOTHING
            RETURNING id;";
        cmd.Parameters.AddWithValue("@id", explicitId ?? Guid.NewGuid());
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@amount", amount);
        cmd.Parameters.AddWithValue("@reason", reason);
        cmd.Parameters.AddWithValue("@key", idempotencyKey);
        cmd.Parameters.AddWithValue("@nowUtc", nowUtc);

        var scalar = await cmd.ExecuteScalarAsync(cancellationToken);
        return scalar != null && scalar != DBNull.Value;
    }

    private async Task<AiQuotaStatusDto> ReadWalletStatusRelationalAsync(
        NpgsqlConnection conn,
        NpgsqlTransaction? tx,
        Guid userId,
        DateOnly today,
        DateTime nextResetUtc,
        CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT daily_free_remaining, balance, last_reset_date, daily_bonus_earned
            FROM ai_credit_wallets
            WHERE user_id = @userId;";
        cmd.Parameters.AddWithValue("@userId", userId);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var rawFree = reader.GetInt32(0);
            var balance = reader.GetInt32(1);
            var lastResetField = reader.GetFieldValue<DateOnly>(2);
            var rawBonusEarned = reader.GetInt32(3);

            var effFree = lastResetField < today ? DailyFreeLimit : rawFree;
            var effBonus = lastResetField < today ? 0 : rawBonusEarned;
            return BuildStatus(DailyFreeLimit, effFree, balance, effBonus, nextResetUtc, isGuest: false);
        }

        return BuildStatus(DailyFreeLimit, DailyFreeLimit, 0, 0, nextResetUtc, isGuest: false);
    }

    // =====================================================
    // IN-MEMORY ATOMIC IMPLEMENTATIONS (FOR UNIT TESTS)
    // =====================================================

    private async Task<AiQuotaConsumeResult> TryConsumeInMemoryAsync(
        Guid userId,
        string reason,
        string idempotencyKey,
        DateOnly today,
        DateTime nowUtc,
        DateTime nextResetUtc,
        CancellationToken cancellationToken)
    {
        var userLock = InMemoryUserLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        await userLock.WaitAsync(cancellationToken);
        try
        {
            var wallet = await GetOrCreateWalletInMemoryAsync(userId, today, nowUtc, cancellationToken);

            var existingTx = await _dbContext.AiCreditTransactions
                .AsNoTracking()
                .AnyAsync(t => t.IdempotencyKey == idempotencyKey, cancellationToken);

            if (existingTx)
            {
                var (ef, eb) = EffectiveDailyValues(wallet, today);
                return new AiQuotaConsumeResult(
                    Allowed: true,
                    AlreadyProcessed: true,
                    ConsumedFromDailyFree: false,
                    IdempotencyKey: idempotencyKey,
                    ErrorCode: null,
                    Message: null,
                    Quota: BuildStatus(DailyFreeLimit, ef, wallet.Balance, eb, nextResetUtc, isGuest: false));
            }

            ApplyDailyResetIfNeeded(wallet, today);

            if (wallet.DailyFreeRemaining <= 0 && wallet.Balance <= 0)
            {
                var exhausted = BuildStatus(DailyFreeLimit, 0, 0, wallet.DailyBonusEarned, nextResetUtc, isGuest: false);
                return new AiQuotaConsumeResult(
                    Allowed: false,
                    AlreadyProcessed: false,
                    ConsumedFromDailyFree: false,
                    IdempotencyKey: idempotencyKey,
                    ErrorCode: QuotaExceededCode,
                    Message: "Bạn đã dùng hết lượt hỏi LingLing AI hôm nay. Hãy chơi Game Center (đạt >=50%), làm Quiz (đạt >=60%) hoặc ôn 5 thẻ Flashcard để kiếm thêm lượt thưởng, hoặc quay lại lúc 00:00 (GMT+7) nhé!",
                    Quota: exhausted);
            }

            bool usedFree;
            if (wallet.DailyFreeRemaining > 0)
            {
                wallet.DailyFreeRemaining -= 1;
                usedFree = true;
            }
            else
            {
                wallet.Balance -= 1;
                usedFree = false;
            }

            wallet.UpdatedAt = nowUtc;
            _dbContext.AiCreditWallets.Update(wallet);
            _dbContext.AiCreditTransactions.Add(new AiCreditTransaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Amount = -1,
                Reason = reason,
                IdempotencyKey = idempotencyKey,
                CreatedAt = nowUtc
            });

            await _dbContext.SaveChangesAsync(cancellationToken);

            var status = BuildStatus(DailyFreeLimit, wallet.DailyFreeRemaining, wallet.Balance, wallet.DailyBonusEarned, nextResetUtc, isGuest: false);
            return new AiQuotaConsumeResult(
                Allowed: true,
                AlreadyProcessed: false,
                ConsumedFromDailyFree: usedFree,
                IdempotencyKey: idempotencyKey,
                ErrorCode: null,
                Message: null,
                Quota: status);
        }
        finally
        {
            userLock.Release();
        }
    }

    private async Task<AiQuotaGrantResult> GrantBonusInMemoryAsync(
        Guid userId,
        int amount,
        string reason,
        string idempotencyKey,
        DateOnly today,
        DateTime nowUtc,
        DateTime nextResetUtc,
        CancellationToken cancellationToken)
    {
        var userLock = InMemoryUserLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        await userLock.WaitAsync(cancellationToken);
        try
        {
            var wallet = await GetOrCreateWalletInMemoryAsync(userId, today, nowUtc, cancellationToken);

            var existingTx = await _dbContext.AiCreditTransactions
                .AsNoTracking()
                .AnyAsync(t => t.IdempotencyKey == idempotencyKey, cancellationToken);

            if (existingTx)
            {
                var (ef, eb) = EffectiveDailyValues(wallet, today);
                var existingStatus = BuildStatus(DailyFreeLimit, ef, wallet.Balance, eb, nextResetUtc, isGuest: false);
                return new AiQuotaGrantResult(false, 0, true, eb >= DailyBonusCap, existingStatus);
            }

            ApplyDailyResetIfNeeded(wallet, today);

            var remainingCap = Math.Max(0, DailyBonusCap - wallet.DailyBonusEarned);
            if (remainingCap <= 0)
            {
                var capStatus = BuildStatus(DailyFreeLimit, wallet.DailyFreeRemaining, wallet.Balance, wallet.DailyBonusEarned, nextResetUtc, isGuest: false);
                return new AiQuotaGrantResult(false, 0, false, true, capStatus);
            }

            var toAdd = Math.Min(amount, remainingCap);
            wallet.Balance += toAdd;
            wallet.DailyBonusEarned += toAdd;
            wallet.UpdatedAt = nowUtc;
            _dbContext.AiCreditWallets.Update(wallet);

            _dbContext.AiCreditTransactions.Add(new AiCreditTransaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Amount = toAdd,
                Reason = reason,
                IdempotencyKey = idempotencyKey,
                CreatedAt = nowUtc
            });

            await _dbContext.SaveChangesAsync(cancellationToken);

            var status = BuildStatus(DailyFreeLimit, wallet.DailyFreeRemaining, wallet.Balance, wallet.DailyBonusEarned, nextResetUtc, isGuest: false);
            return new AiQuotaGrantResult(true, toAdd, false, wallet.DailyBonusEarned >= DailyBonusCap, status);
        }
        finally
        {
            userLock.Release();
        }
    }

    private async Task<AiCreditWallet> GetOrCreateWalletInMemoryAsync(
        Guid userId,
        DateOnly today,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var wallet = await _dbContext.AiCreditWallets
            .AsTracking()
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

        if (wallet != null)
        {
            return wallet;
        }

        wallet = new AiCreditWallet
        {
            UserId = userId,
            Balance = 0,
            DailyFreeRemaining = DailyFreeLimit,
            LastResetDate = today,
            DailyBonusEarned = 0,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc
        };
        _dbContext.AiCreditWallets.Add(wallet);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return wallet;
    }

    private void ApplyDailyResetIfNeeded(AiCreditWallet wallet, DateOnly today)
    {
        if (wallet.LastResetDate < today)
        {
            wallet.DailyFreeRemaining = DailyFreeLimit;
            wallet.DailyBonusEarned = 0;
            wallet.LastResetDate = today;
        }
    }

    private (int EffectiveFree, int EffectiveBonusEarned) EffectiveDailyValues(AiCreditWallet wallet, DateOnly today)
    {
        if (wallet.LastResetDate < today)
        {
            return (DailyFreeLimit, 0);
        }
        return (wallet.DailyFreeRemaining, wallet.DailyBonusEarned);
    }

    private AiQuotaStatusDto BuildStatus(
        int dailyLimit,
        int dailyRemaining,
        int bonusCredits,
        int dailyBonusEarned,
        DateTime nextResetUtc,
        bool isGuest)
    {
        var clampedFree = Math.Clamp(dailyRemaining, 0, dailyLimit);
        var clampedBonus = Math.Max(0, bonusCredits);
        var dailyUsed = Math.Max(0, dailyLimit - clampedFree);
        return new AiQuotaStatusDto(
            DailyFreeLimit: dailyLimit,
            DailyUsed: dailyUsed,
            DailyRemaining: clampedFree,
            BonusCredits: clampedBonus,
            TotalAvailable: clampedFree + clampedBonus,
            DailyBonusEarned: isGuest ? 0 : Math.Max(0, dailyBonusEarned),
            DailyBonusCap: isGuest ? 0 : DailyBonusCap,
            NextResetAtUtc: nextResetUtc,
            IsGuest: isGuest
        );
    }

    private static string NormalizeIp(string? rawIp)
    {
        if (string.IsNullOrWhiteSpace(rawIp))
        {
            return "unknown";
        }
        var trimmed = rawIp.Trim();
        if (trimmed.StartsWith("::ffff:", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed["::ffff:".Length..];
        }
        if (trimmed == "::1")
        {
            return "127.0.0.1";
        }
        return trimmed;
    }

    private static string BuildGuestCacheKey(string? ip, DateOnly today) =>
        $"ai_guest_quota:{today:yyyy-MM-dd}:{NormalizeIp(ip)}";
}
