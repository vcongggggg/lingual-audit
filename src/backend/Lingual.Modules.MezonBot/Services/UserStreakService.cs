using System;
using System.Collections.Concurrent;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;
using Lingual.Modules.MezonBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lingual.Modules.MezonBot.Services;

public class UserStreakService : IUserStreakService
{
    private readonly MezonBotDbContext _dbContext;
    private readonly ILogger<UserStreakService> _logger;

    // Bộ nhớ in-memory phục vụ cho Unit Test (khi DbContext không phải Relational)
    private static readonly ConcurrentDictionary<string, (int CurrentDays, int BestDays, DateOnly? LastDate, int FreezeBalance)> InMemoryStreaks = new();

    public UserStreakService(
        MezonBotDbContext dbContext,
        ILogger<UserStreakService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<DailyCheckinResultDto> ProcessDailyCheckinAsync(string mezonUserId, string displayName)
    {
        // Múi giờ Việt Nam GMT+7
        var nowUtc = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(nowUtc.AddHours(7));

        if (!_dbContext.Database.IsRelational())
        {
            return ProcessInMemoryCheckin(mezonUserId, today);
        }

        try
        {
            var conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync();
            }

            var userId = await EnsureUserExistsAsync(mezonUserId, displayName);

            // 1. Đọc thông tin chuỗi hiện tại của người dùng
            int currentDays = 0;
            int bestDays = 0;
            DateOnly? lastActiveDate = null;
            int freezeBalance = 0;
            bool exists = false;

            using (var selectCmd = conn.CreateCommand())
            {
                selectCmd.CommandText = "SELECT current_days, best_days, last_active_date, freeze_balance FROM user_streaks WHERE user_id = @userId;";
                var p = selectCmd.CreateParameter();
                p.ParameterName = "@userId";
                p.Value = userId;
                selectCmd.Parameters.Add(p);

                using var reader = await selectCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    exists = true;
                    currentDays = reader.GetInt32(0);
                    bestDays = reader.GetInt32(1);
                    if (!reader.IsDBNull(2))
                    {
                        lastActiveDate = DateOnly.FromDateTime(reader.GetDateTime(2));
                    }
                    freezeBalance = reader.GetInt32(3);
                }
            }

            // 2. Kiểm tra nếu hôm nay đã điểm danh rồi
            if (exists && lastActiveDate == today)
            {
                return new DailyCheckinResultDto(
                    IsSuccess: true,
                    AlreadyCheckedInToday: true,
                    CurrentDays: currentDays,
                    BestDays: bestDays,
                    XpAwarded: 0,
                    FreezeBalance: freezeBalance,
                    FreezeUsed: false,
                    Message: "Hôm nay bạn đã điểm danh giữ streak rồi! Hãy quay lại vào ngày mai nhé."
                );
            }

            const int dailyXp = 20;
            var dailyJson = JsonSerializer.Serialize(new
            {
                xp = dailyXp,
                type = "daily_checkin",
                checked_in_at = nowUtc.ToString("o")
            });

            bool freezeUsed = false;
            int newCurrentDays;
            int newBestDays;

            if (!exists)
            {
                // Lần đầu tiên điểm danh
                newCurrentDays = 1;
                newBestDays = 1;

                using var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = @"
                    INSERT INTO user_streaks (user_id, current_days, best_days, last_active_date, freeze_balance, freeze_history, daily_activity, updated_at)
                    VALUES (@userId, @currentDays, @bestDays, @lastDate, 0, '[]'::jsonb, jsonb_build_object(@dateKey, @dailyJson::jsonb), now())
                    ON CONFLICT (user_id) DO UPDATE SET
                        current_days = EXCLUDED.current_days,
                        best_days = GREATEST(user_streaks.best_days, EXCLUDED.best_days),
                        last_active_date = EXCLUDED.last_active_date,
                        daily_activity = user_streaks.daily_activity || jsonb_build_object(@dateKey, @dailyJson::jsonb),
                        updated_at = now();";

                AddParam(insertCmd, "@userId", userId);
                AddParam(insertCmd, "@currentDays", newCurrentDays);
                AddParam(insertCmd, "@bestDays", newBestDays);
                AddParam(insertCmd, "@lastDate", today.ToDateTime(TimeOnly.MinValue));
                AddParam(insertCmd, "@dateKey", today.ToString("yyyy-MM-dd"));
                AddParam(insertCmd, "@dailyJson", dailyJson);

                await insertCmd.ExecuteNonQueryAsync();
            }
            else
            {
                // Đã có bản ghi: kiểm tra khoảng cách ngày
                var yesterday = today.AddDays(-1);

                if (lastActiveDate == yesterday)
                {
                    // Nối dài chuỗi liên tiếp hoàn hảo!
                    newCurrentDays = currentDays + 1;
                    newBestDays = Math.Max(bestDays, newCurrentDays);

                    using var updateCmd = conn.CreateCommand();
                    updateCmd.CommandText = @"
                        UPDATE user_streaks
                        SET current_days = @currentDays,
                            best_days = @bestDays,
                            last_active_date = @lastDate,
                            daily_activity = daily_activity || jsonb_build_object(@dateKey, @dailyJson::jsonb),
                            updated_at = now()
                        WHERE user_id = @userId;";

                    AddParam(updateCmd, "@userId", userId);
                    AddParam(updateCmd, "@currentDays", newCurrentDays);
                    AddParam(updateCmd, "@bestDays", newBestDays);
                    AddParam(updateCmd, "@lastDate", today.ToDateTime(TimeOnly.MinValue));
                    AddParam(updateCmd, "@dateKey", today.ToString("yyyy-MM-dd"));
                    AddParam(updateCmd, "@dailyJson", dailyJson);

                    await updateCmd.ExecuteNonQueryAsync();
                }
                else if (freezeBalance > 0 && lastActiveDate == today.AddDays(-2))
                {
                    // Lỡ 1 ngày nhưng còn khiên Streak Freeze!
                    freezeUsed = true;
                    freezeBalance -= 1;
                    newCurrentDays = currentDays + 1;
                    newBestDays = Math.Max(bestDays, newCurrentDays);

                    var freezeEventJson = JsonSerializer.Serialize(new
                    {
                        used_at = nowUtc.ToString("o"),
                        reason = "daily_checkin_protection",
                        saved_date = yesterday.ToString("yyyy-MM-dd")
                    });

                    using var updateCmd = conn.CreateCommand();
                    updateCmd.CommandText = @"
                        UPDATE user_streaks
                        SET current_days = @currentDays,
                            best_days = @bestDays,
                            last_active_date = @lastDate,
                            freeze_balance = freeze_balance - 1,
                            freeze_history = freeze_history || jsonb_build_array(@freezeEvent::jsonb),
                            daily_activity = daily_activity || jsonb_build_object(@dateKey, @dailyJson::jsonb),
                            updated_at = now()
                        WHERE user_id = @userId AND freeze_balance > 0;";

                    AddParam(updateCmd, "@userId", userId);
                    AddParam(updateCmd, "@currentDays", newCurrentDays);
                    AddParam(updateCmd, "@bestDays", newBestDays);
                    AddParam(updateCmd, "@lastDate", today.ToDateTime(TimeOnly.MinValue));
                    AddParam(updateCmd, "@freezeEvent", freezeEventJson);
                    AddParam(updateCmd, "@dateKey", today.ToString("yyyy-MM-dd"));
                    AddParam(updateCmd, "@dailyJson", dailyJson);

                    await updateCmd.ExecuteNonQueryAsync();
                }
                else
                {
                    // Chuỗi bị đứt do lỡ từ 2 ngày trở lên hoặc hết khiên bảo vệ
                    newCurrentDays = 1;
                    newBestDays = bestDays;

                    using var updateCmd = conn.CreateCommand();
                    updateCmd.CommandText = @"
                        UPDATE user_streaks
                        SET current_days = 1,
                            last_active_date = @lastDate,
                            daily_activity = daily_activity || jsonb_build_object(@dateKey, @dailyJson::jsonb),
                            updated_at = now()
                        WHERE user_id = @userId;";

                    AddParam(updateCmd, "@userId", userId);
                    AddParam(updateCmd, "@lastDate", today.ToDateTime(TimeOnly.MinValue));
                    AddParam(updateCmd, "@dateKey", today.ToString("yyyy-MM-dd"));
                    AddParam(updateCmd, "@dailyJson", dailyJson);

                    await updateCmd.ExecuteNonQueryAsync();
                }
            }

            // 3. Cộng +20 XP vào xp_ledger với Idempotency Key theo ngày
            await AwardDailyXpAsync(conn, userId, today, dailyXp);

            _logger.LogInformation("🔥 [UserStreak] User {UserId} ({MezonId}) điểm danh thành công! Chuỗi: {Current} ngày (Kỷ lục: {Best})",
                userId, mezonUserId, newCurrentDays, newBestDays);

            return new DailyCheckinResultDto(
                IsSuccess: true,
                AlreadyCheckedInToday: false,
                CurrentDays: newCurrentDays,
                BestDays: newBestDays,
                XpAwarded: dailyXp,
                FreezeBalance: freezeBalance,
                FreezeUsed: freezeUsed,
                Message: "Điểm danh hàng ngày thành công! Giữ vững ngọn lửa học tập nhé."
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [UserStreak] Lỗi khi xử lý điểm danh cho Mezon User {MezonUserId}", mezonUserId);
            return new DailyCheckinResultDto(
                IsSuccess: false,
                AlreadyCheckedInToday: false,
                CurrentDays: 0,
                BestDays: 0,
                XpAwarded: 0,
                FreezeBalance: 0,
                FreezeUsed: false,
                Message: "Đã có lỗi xảy ra trong quá trình ghi nhận điểm danh."
            );
        }
    }

    public async Task<UserStreakDto?> GetStreakAsync(string mezonUserId)
    {
        var nowUtc = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(nowUtc.AddHours(7));

        if (!_dbContext.Database.IsRelational())
        {
            if (InMemoryStreaks.TryGetValue(mezonUserId, out var val))
            {
                return new UserStreakDto(val.CurrentDays, val.BestDays, val.LastDate, val.FreezeBalance, val.LastDate == today);
            }
            return null;
        }

        try
        {
            var conn = _dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync();
            }

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT s.current_days, s.best_days, s.last_active_date, s.freeze_balance
                FROM user_streaks s
                INNER JOIN users u ON u.id = s.user_id
                WHERE u.mezon_user_id = @mezonUserId;";

            var p = cmd.CreateParameter();
            p.ParameterName = "@mezonUserId";
            p.Value = mezonUserId;
            cmd.Parameters.Add(p);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var cur = reader.GetInt32(0);
                var best = reader.GetInt32(1);
                DateOnly? lastDate = null;
                if (!reader.IsDBNull(2))
                {
                    lastDate = DateOnly.FromDateTime(reader.GetDateTime(2));
                }
                var freeze = reader.GetInt32(3);

                return new UserStreakDto(
                    CurrentDays: cur,
                    BestDays: best,
                    LastActiveDate: lastDate,
                    FreezeBalance: freeze,
                    IsActiveToday: lastDate == today
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [UserStreak] Lỗi khi tra cứu chuỗi streak cho Mezon User {MezonUserId}", mezonUserId);
        }

        return null;
    }

    private static void AddParam(System.Data.Common.DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }

    private async Task<Guid> EnsureUserExistsAsync(string mezonUserId, string displayName)
    {
        var conn = _dbContext.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync();
        }
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO users (id, mezon_user_id, display_name, email, role, created_at, updated_at)
            VALUES (gen_random_uuid(), @mezonUserId, @displayName, @mezonUserId || '@mezon.ai', 'learner', now(), now())
            ON CONFLICT (mezon_user_id) DO UPDATE SET display_name = EXCLUDED.display_name
            RETURNING id;";

        AddParam(cmd, "@mezonUserId", mezonUserId);
        AddParam(cmd, "@displayName", string.IsNullOrWhiteSpace(displayName) ? "Learner" : displayName);

        var scalar = await cmd.ExecuteScalarAsync();
        if (scalar is Guid g) return g;
        if (scalar != null && Guid.TryParse(scalar.ToString(), out var parsed)) return parsed;

        return Guid.NewGuid();
    }

    private async Task AwardDailyXpAsync(System.Data.Common.DbConnection conn, Guid userId, DateOnly today, int xpDelta)
    {
        try
        {
            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync();
            }

            var idempotencyKey = $"daily_checkin_{userId}_{today:yyyyMMdd}";
            var metadata = JsonSerializer.Serialize(new
            {
                type = "daily_checkin",
                date = today.ToString("yyyy-MM-dd"),
                xp = xpDelta
            });

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO xp_ledger (id, user_id, source_type, source_id, xp_delta, awarded_at, idempotency_key, metadata)
                VALUES (gen_random_uuid(), @userId, 'daily_checkin', gen_random_uuid(), @xpDelta, now(), @idempotencyKey, @metadata::jsonb)
                ON CONFLICT (idempotency_key) DO NOTHING;";

            AddParam(cmd, "@userId", userId);
            AddParam(cmd, "@xpDelta", xpDelta);
            AddParam(cmd, "@idempotencyKey", idempotencyKey);
            AddParam(cmd, "@metadata", metadata);

            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ [UserStreak] Không thể ghi nhận XP điểm danh vào xp_ledger");
        }
    }

    private static DailyCheckinResultDto ProcessInMemoryCheckin(string mezonUserId, DateOnly today)
    {
        if (InMemoryStreaks.TryGetValue(mezonUserId, out var val))
        {
            if (val.LastDate == today)
            {
                return new DailyCheckinResultDto(
                    IsSuccess: true,
                    AlreadyCheckedInToday: true,
                    CurrentDays: val.CurrentDays,
                    BestDays: val.BestDays,
                    XpAwarded: 0,
                    FreezeBalance: val.FreezeBalance,
                    FreezeUsed: false,
                    Message: "Hôm nay bạn đã điểm danh giữ streak rồi!"
                );
            }

            var isConsecutive = val.LastDate == today.AddDays(-1);
            var newCur = isConsecutive ? val.CurrentDays + 1 : 1;
            var newBest = Math.Max(val.BestDays, newCur);
            InMemoryStreaks[mezonUserId] = (newCur, newBest, today, val.FreezeBalance);

            return new DailyCheckinResultDto(
                IsSuccess: true,
                AlreadyCheckedInToday: false,
                CurrentDays: newCur,
                BestDays: newBest,
                XpAwarded: 20,
                FreezeBalance: val.FreezeBalance,
                FreezeUsed: false,
                Message: "Điểm danh hàng ngày thành công!"
            );
        }
        else
        {
            InMemoryStreaks[mezonUserId] = (1, 1, today, 0);
            return new DailyCheckinResultDto(
                IsSuccess: true,
                AlreadyCheckedInToday: false,
                CurrentDays: 1,
                BestDays: 1,
                XpAwarded: 20,
                FreezeBalance: 0,
                FreezeUsed: false,
                Message: "Bắt đầu chuỗi streak mới thành công!"
            );
        }
    }
}

