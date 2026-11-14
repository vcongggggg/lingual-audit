using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Lingual.Modules.MezonBot.Domain;
using Lingual.Modules.MezonBot.Infrastructure.Data;

namespace Lingual.Modules.MezonBot.Services;

public class ClanQuizService : IClanQuizService
{
    private readonly MezonBotDbContext _dbContext;
    private readonly ILogger<ClanQuizService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ClanQuizService(
        MezonBotDbContext dbContext,
        ILogger<ClanQuizService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> HasActiveSessionAsync(long channelId, bool isPractice = false)
    {
        var channelKey = isPractice ? $"dm_{channelId}" : channelId.ToString();
        var now = DateTime.UtcNow;

        return await _dbContext.ClanQuizSessions
            .AnyAsync(s => s.ChannelMezonId == channelKey && s.Status == "open" && s.ClosesAt > now);
    }

    public async Task<ClanQuizSessionDto?> StartSessionAsync(
        long channelId,
        string? startedByMezonUserId = null,
        int durationSeconds = 30,
        bool isPractice = false,
        string? slotKey = null)
    {
        if (await HasActiveSessionAsync(channelId, isPractice))
        {
            _logger.LogWarning("Kênh/DM {ChannelId} đang có phiên đố vui mở. Bỏ qua tạo mới.", channelId);
            return null;
        }

        var question = await GetRandomQuestionAsync();
        if (question == null)
        {
            _logger.LogError("Không tìm thấy câu hỏi trắc nghiệm nào để khởi tạo Clan Quiz.");
            return null;
        }

        Guid? startedByGuid = null;
        if (!string.IsNullOrWhiteSpace(startedByMezonUserId))
        {
            startedByGuid = await EnsureUserExistsAsync(startedByMezonUserId, "Learner");
        }

        var now = DateTime.UtcNow;
        var channelKey = isPractice ? $"dm_{channelId}" : channelId.ToString();
        var session = new ClanQuizSession
        {
            Id = Guid.NewGuid(),
            ChannelMezonId = channelKey,
            QuestionId = question.Id,
            StartedBy = startedByGuid,
            Status = "open",
            OpenedAt = now,
            ClosesAt = now.AddSeconds(durationSeconds),
            Responses = "[]",
            SlotKey = slotKey
        };

        _dbContext.ClanQuizSessions.Add(session);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("🚀 [ClanQuiz] Đã tạo phiên Quiz {SessionId} (Slot: {SlotKey}, Practice: {IsPractice}) cho Kênh {ChannelId}. Hạn chót: {ClosesAt}",
            session.Id, slotKey, isPractice, channelId, session.ClosesAt);

        var parsedOptions = ParseOptions(question.Options);

        return new ClanQuizSessionDto
        {
            SessionId = session.Id,
            ChannelId = channelId,
            QuestionPrompt = question.Prompt,
            Level = question.Level,
            Difficulty = question.Difficulty,
            Options = parsedOptions.Select(o => new QuizOptionDto { Key = o.Key, Text = o.Text }).ToList(),
            DurationSeconds = durationSeconds,
            ClosesAt = session.ClosesAt,
            IsPractice = isPractice
        };
    }

    public async Task<QuizSubmissionResultDto> SubmitAnswerAsync(
        Guid sessionId,
        long channelId,
        string mezonUserId,
        string username,
        string selectedKey)
    {
        var session = await _dbContext.ClanQuizSessions
            .Include(s => s.Question)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
        {
            return new QuizSubmissionResultDto
            {
                Status = QuizSubmissionStatus.SessionNotFound,
                Message = "Không tìm thấy phiên đố vui này."
            };
        }

        var now = DateTime.UtcNow;
        if (session.Status != "open" || session.ClosesAt <= now)
        {
            return new QuizSubmissionResultDto
            {
                Status = QuizSubmissionStatus.SessionExpired,
                Message = "Phiên đố vui này đã kết thúc."
            };
        }

        var cleanKey = selectedKey.Trim().ToUpperInvariant();
        var options = ParseOptions(session.Question?.Options ?? "[]");
        var selectedOption = options.FirstOrDefault(o => o.Key.Equals(cleanKey, StringComparison.OrdinalIgnoreCase));

        if (selectedOption == null)
        {
            return new QuizSubmissionResultDto
            {
                Status = QuizSubmissionStatus.InvalidOption,
                Message = $"Lựa chọn '{cleanKey}' không hợp lệ."
            };
        }

        var isCorrect = selectedOption.IsCorrect;
        var responseMs = Math.Max(0, (int)(now - session.OpenedAt).TotalMilliseconds);
        var resolvedUserId = await EnsureUserExistsAsync(mezonUserId, username);

        // Chuẩn bị payload JSON lưu vào responses JSONB
        var responseItem = new InternalQuizResponse
        {
            UserId = resolvedUserId,
            MezonUserId = mezonUserId,
            Username = username,
            SelectedKey = cleanKey,
            IsCorrect = isCorrect,
            ResponseMs = responseMs,
            RespondedAt = now.ToString("o")
        };

        var responseJson = JsonSerializer.Serialize(responseItem, JsonOptions);
        var userCheckJson = JsonSerializer.Serialize(new[] { new { mezon_user_id = mezonUserId } }, JsonOptions);

        if (_dbContext.Database.IsRelational())
        {
            // Anti-Lost Update & Concurrency Rule (BẮT BUỘC):
            // Mảng JSONB responses là hot path đa-writer, sử dụng raw SQL nguyên tử
            // và kiểm tra NOT (responses @> userCheck) để chống một người bấm nhiều lần
            const string atomicUpdateSql = @"
                UPDATE clan_quiz_sessions
                SET responses = responses || {0}::jsonb,
                    winning_user_id = CASE WHEN {1} = true AND winning_user_id IS NULL THEN {2} ELSE winning_user_id END,
                    winning_response_ms = CASE WHEN {1} = true AND winning_response_ms IS NULL THEN {3} ELSE winning_response_ms END
                WHERE id = {4}
                  AND status = 'open'
                  AND closes_at > now()
                  AND NOT (responses @> {5}::jsonb);";

            var rowsAffected = await _dbContext.Database.ExecuteSqlRawAsync(
                atomicUpdateSql,
                responseJson,
                isCorrect,
                resolvedUserId,
                responseMs,
                sessionId,
                userCheckJson);

            if (rowsAffected == 0)
            {
                // Kiểm tra xem là do đã trả lời rồi hay do phiên đã đóng
                var refreshedSession = await _dbContext.ClanQuizSessions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == sessionId);

                if (refreshedSession != null && refreshedSession.Responses.Contains(mezonUserId))
                {
                    return new QuizSubmissionResultDto
                    {
                        Status = QuizSubmissionStatus.AlreadyAnswered,
                        Message = "Bạn đã gửi câu trả lời cho câu đố này rồi!"
                    };
                }

                return new QuizSubmissionResultDto
                {
                    Status = QuizSubmissionStatus.SessionExpired,
                    Message = "Thời gian trả lời câu đố đã kết thúc!"
                };
            }
        }
        else
        {
            // In-Memory fallback cho Unit Tests
            var existingResponses = ParseResponses(session.Responses);
            if (existingResponses.Any(r => r.MezonUserId == mezonUserId))
            {
                return new QuizSubmissionResultDto
                {
                    Status = QuizSubmissionStatus.AlreadyAnswered,
                    Message = "Bạn đã gửi câu trả lời cho câu đố này rồi!"
                };
            }

            existingResponses.Add(responseItem);
            session.Responses = JsonSerializer.Serialize(existingResponses, JsonOptions);

            if (isCorrect && session.WinningUserId == null)
            {
                session.WinningUserId = resolvedUserId;
                session.WinningResponseMs = responseMs;
            }

            await _dbContext.SaveChangesAsync();
        }

        // Tải lại để kiểm tra người này có phải winner đầu tiên không
        var updatedSession = await _dbContext.ClanQuizSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        var isPractice = session.ChannelMezonId.StartsWith("dm_");
        var isWinner = !isPractice && isCorrect && updatedSession?.WinningUserId == resolvedUserId && updatedSession?.WinningResponseMs == responseMs;
        var xpAwarded = 0;
        string message;

        var isScheduled = !string.IsNullOrWhiteSpace(session.SlotKey);

        if (isScheduled)
        {
            // === RULE LIÊN CLAN SCHEDULED QUIZ ===
            // 1 user CHỈ được nhận XP quiz theo lịch TỐI ĐA 1 LẦN cho mỗi (ngày + khung giờ) TRÊN TOÀN BỘ CÁC CLAN.
            // Nếu trả lời sai ở Clan A (0 XP), học viên vẫn có cơ hội nhận XP khi trả lời đúng ở Clan B.
            var existingParticipation = await _dbContext.ScheduledQuizParticipations
                .FirstOrDefaultAsync(p => p.SlotKey == session.SlotKey && (p.UserId == resolvedUserId || p.MezonUserId == mezonUserId));

            if (existingParticipation != null && existingParticipation.XpAwarded > 0)
            {
                // Người dùng ĐÃ NHẬN XP ở Clan khác trong khung giờ này
                xpAwarded = 0;
                message = "Bạn đã nhận XP khung giờ này ở clan khác, câu này chỉ luyện tập - 0 XP";
            }
            else
            {
                var candidateXp = isCorrect ? (isWinner ? 15 : 5) : 0;
                var globalIdempotencyKey = $"scheduled_quiz_{session.SlotKey}_{resolvedUserId}";

                if (candidateXp > 0)
                {
                    var awarded = await AwardScheduledXpAsync(resolvedUserId, sessionId, candidateXp, globalIdempotencyKey, cleanKey, responseMs);
                    if (awarded)
                    {
                        xpAwarded = candidateXp;
                        message = isWinner
                            ? $"🎉 Xuất sắc! Bạn là người trả lời ĐÚNG và NHANH NHẤT (+{xpAwarded} XP)!"
                            : $"✅ Chính xác! Bạn nhận được +{xpAwarded} XP.";
                    }
                    else
                    {
                        xpAwarded = 0;
                        message = "Bạn đã nhận XP khung giờ này ở clan khác, câu này chỉ luyện tập - 0 XP";
                    }
                }
                else
                {
                    xpAwarded = 0;
                    message = "❌ Tiếc quá, câu trả lời chưa chính xác!";
                }

                // Ghi nhận / cập nhật lượt tham gia vào scheduled_quiz_participations
                try
                {
                    if (existingParticipation != null)
                    {
                        existingParticipation.SessionId = sessionId;
                        existingParticipation.ChannelId = channelId;
                        existingParticipation.ClanId = channelId;
                        existingParticipation.IsCorrect = isCorrect;
                        existingParticipation.XpAwarded = xpAwarded;
                        existingParticipation.CreatedAt = now;
                    }
                    else
                    {
                        var participation = new ScheduledQuizParticipation
                        {
                            Id = Guid.NewGuid(),
                            SlotKey = session.SlotKey!,
                            UserId = resolvedUserId,
                            MezonUserId = mezonUserId,
                            ClanId = channelId,
                            ChannelId = channelId,
                            SessionId = sessionId,
                            IsCorrect = isCorrect,
                            XpAwarded = xpAwarded,
                            CreatedAt = now
                        };
                        _dbContext.ScheduledQuizParticipations.Add(participation);
                    }
                    await _dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ [ClanQuizService] Không thể ghi nhận participation cho User {UserId}", resolvedUserId);
                }
            }
        }
        else
        {
            if (isCorrect && !isPractice)
            {
                xpAwarded = isWinner ? 15 : 5;
                await AwardXpAsync(resolvedUserId, sessionId, xpAwarded, isWinner, cleanKey, responseMs);
            }

            if (isPractice)
            {
                message = isCorrect
                    ? $"✅ Chính xác! Bạn đã chọn đúng đáp án {cleanKey}. (Chế độ tự luyện tập DM: Không cộng điểm XP)"
                    : $"❌ Tiếc quá, đáp án {cleanKey} chưa chính xác!";
            }
            else
            {
                message = isCorrect
                    ? (isWinner ? "🎉 Xuất sắc! Bạn là người trả lời ĐÚNG và NHANH NHẤT (+15 XP)!" : "✅ Chính xác! Bạn nhận được +5 XP.")
                    : "❌ Tiếc quá, câu trả lời chưa chính xác!";
            }
        }

        if (!_dbContext.Database.IsRelational())
        {
            var inMemList = ParseResponses(session.Responses);
            var item = inMemList.FirstOrDefault(r => r.MezonUserId == mezonUserId);
            if (item != null)
            {
                item.XpAwarded = xpAwarded;
                session.Responses = JsonSerializer.Serialize(inMemList, JsonOptions);
                await _dbContext.SaveChangesAsync();
            }
        }

        return new QuizSubmissionResultDto
        {
            Status = QuizSubmissionStatus.Success,
            IsCorrect = isCorrect,
            IsWinner = isWinner,
            ResponseMs = responseMs,
            XpAwarded = xpAwarded,
            IsPractice = isPractice,
            Message = message
        };
    }

    public async Task<ClanQuizConcludeDto?> ConcludeSessionAsync(Guid sessionId)
    {
        if (_dbContext.Database.IsRelational())
        {
            // Đóng session bằng raw SQL atomic để ngăn chặn bất kỳ submission muộn nào
            const string closeSql = @"
                UPDATE clan_quiz_sessions
                SET status = 'closed',
                    closed_at = now()
                WHERE id = {0} AND status = 'open';";

            await _dbContext.Database.ExecuteSqlRawAsync(closeSql, sessionId);
        }
        else
        {
            var inMemSession = await _dbContext.ClanQuizSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
            if (inMemSession != null)
            {
                inMemSession.Status = "closed";
                inMemSession.ClosedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }
        }

        var session = await _dbContext.ClanQuizSessions
            .Include(s => s.Question)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null || session.Question == null)
        {
            return null;
        }

        var options = ParseOptions(session.Question.Options);
        var correctOption = options.FirstOrDefault(o => o.IsCorrect) ?? options.FirstOrDefault();
        var responses = ParseResponses(session.Responses);

        var correctResponses = responses.Count(r => r.IsCorrect);
        var totalResponses = responses.Count;

        // Xác định winner (nếu DB đã ghi nhận winning_user_id hoặc lấy người đúng có response_ms nhỏ nhất)
        var fastestCorrect = responses
            .Where(r => r.IsCorrect)
            .OrderBy(r => r.ResponseMs)
            .FirstOrDefault();

        var winnerMezonId = fastestCorrect?.MezonUserId;
        var winnerUsername = fastestCorrect?.Username;
        var winningMs = fastestCorrect?.ResponseMs;

        List<ScheduledQuizParticipation> scheduledParticipations = new();
        if (!string.IsNullOrWhiteSpace(session.SlotKey))
        {
            scheduledParticipations = await _dbContext.ScheduledQuizParticipations
                .AsNoTracking()
                .Where(p => p.SlotKey == session.SlotKey)
                .ToListAsync();
        }

        var isPractice = session.ChannelMezonId.StartsWith("dm_");
        var rawChannelId = session.ChannelMezonId.Replace("dm_", "");

        var participants = responses
            .OrderBy(r => r.IsCorrect ? 0 : 1)
            .ThenBy(r => r.ResponseMs)
            .Select(r =>
            {
                var pRec = scheduledParticipations.FirstOrDefault(p => p.MezonUserId == r.MezonUserId);
                int xp;
                if (!string.IsNullOrWhiteSpace(session.SlotKey))
                {
                    if (pRec != null)
                    {
                        xp = pRec.SessionId == sessionId ? pRec.XpAwarded : 0;
                    }
                    else
                    {
                        xp = r.XpAwarded;
                    }
                }
                else
                {
                    xp = r.XpAwarded > 0 ? r.XpAwarded : (!isPractice && r.IsCorrect ? (r.MezonUserId == winnerMezonId ? 15 : 5) : 0);
                }

                return new QuizParticipantSummaryDto
                {
                    MezonUserId = r.MezonUserId,
                    Username = r.Username,
                    SelectedKey = r.SelectedKey,
                    IsCorrect = r.IsCorrect,
                    ResponseMs = r.ResponseMs,
                    XpAwarded = xp
                };
            })
            .ToList();

        var winnerParticipant = participants.FirstOrDefault(p => p.MezonUserId == winnerMezonId);
        var winnerXp = winnerParticipant?.XpAwarded ?? 0;

        return new ClanQuizConcludeDto
        {
            SessionId = sessionId,
            ChannelId = long.TryParse(rawChannelId, out var cid) ? cid : 0,
            QuestionPrompt = session.Question.Prompt,
            CorrectKey = correctOption?.Key ?? "A",
            CorrectText = correctOption?.Text ?? string.Empty,
            Explanation = session.Question.Explanation,
            WinnerMezonUserId = winnerMezonId,
            WinnerUsername = winnerUsername,
            WinningResponseMs = winningMs,
            WinnerXpAwarded = winnerXp,
            TotalResponses = totalResponses,
            CorrectResponses = correctResponses,
            Participants = participants,
            IsPractice = isPractice,
            SlotKey = session.SlotKey
        };
    }

    private async Task<QuizQuestion?> GetRandomQuestionAsync()
    {
        QuizQuestion? question = null;
        var now = DateTime.UtcNow;
        var sevenDaysAgo = now.AddDays(-7);

        // Lấy danh sách questionId đang được sử dụng ở các phiên mở để tránh trùng lặp giữa các Clan cùng slot
        var activeQuestionIds = await _dbContext.ClanQuizSessions
            .Where(s => s.Status == "open" && s.ClosesAt > now)
            .Select(s => s.QuestionId)
            .ToListAsync();

        if (_dbContext.Database.IsRelational())
        {
            // 1. Ưu tiên câu hỏi chưa hỏi trong 7 ngày gần nhất và không trùng với phiên đang mở
            question = await _dbContext.QuizQuestions
                .Where(q => q.Status == "published"
                    && !activeQuestionIds.Contains(q.Id)
                    && (q.LastAskedAt == null || q.LastAskedAt < sevenDaysAgo))
                .OrderBy(r => EF.Functions.Random())
                .FirstOrDefaultAsync();

            if (question == null)
            {
                question = await _dbContext.QuizQuestions
                    .Where(q => q.Status == "published" && !activeQuestionIds.Contains(q.Id))
                    .OrderBy(q => q.LastAskedAt)
                    .FirstOrDefaultAsync();
            }
        }
        else
        {
            question = await _dbContext.QuizQuestions
                .Where(q => q.Status == "published"
                    && !activeQuestionIds.Contains(q.Id)
                    && (q.LastAskedAt == null || q.LastAskedAt < sevenDaysAgo))
                .FirstOrDefaultAsync();

            if (question == null)
            {
                question = await _dbContext.QuizQuestions
                    .Where(q => q.Status == "published" && !activeQuestionIds.Contains(q.Id))
                    .OrderBy(q => q.LastAskedAt)
                    .FirstOrDefaultAsync();
            }
        }

        if (question != null)
        {
            question.LastAskedAt = now;
            if (_dbContext.Database.IsRelational())
            {
                await _dbContext.QuizQuestions
                    .Where(q => q.Id == question.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.LastAskedAt, now));
            }
            else
            {
                await _dbContext.SaveChangesAsync();
            }
            return question;
        }

        // Fallback: Nếu cơ sở dữ liệu chưa seed, tự động tạo câu hỏi mẫu chuẩn CEFR A1
        _logger.LogWarning("Chưa có câu hỏi trong database. Sử dụng câu hỏi mẫu CEFR...");
        var fallbackQuestion = new QuizQuestion
        {
            Id = Guid.NewGuid(),
            QuestionType = "multiple_choice",
            Prompt = "I totally ________ with your idea.",
            Difficulty = 1,
            Level = "A1",
            Options = @"[
                {""key"":""A"",""text"":""work"",""is_correct"":false},
                {""key"":""B"",""text"":""understand"",""is_correct"":false},
                {""key"":""C"",""text"":""agree"",""is_correct"":true},
                {""key"":""D"",""text"":""write"",""is_correct"":false}
            ]",
            Explanation = "Đáp án đúng là C (agree - đồng ý). Dịch câu: 'Tôi hoàn toàn đồng ý với ý kiến của bạn.'.",
            Points = 10,
            Status = "published",
            LastAskedAt = now
        };

        _dbContext.QuizQuestions.Add(fallbackQuestion);
        await _dbContext.SaveChangesAsync();

        return fallbackQuestion;
    }

    private async Task<Guid> EnsureUserExistsAsync(string mezonUserId, string displayName)
    {
        if (_dbContext.Database.IsRelational())
        {
            try
            {
                var conn = _dbContext.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                {
                    await conn.OpenAsync();
                }

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO users (id, mezon_user_id, display_name, status, role, preferred_locale, timezone)
                    VALUES (gen_random_uuid(), @mezonUserId, @displayName, 'active', 'learner', 'vi-VN', 'Asia/Ho_Chi_Minh')
                    ON CONFLICT (mezon_user_id) DO UPDATE SET display_name = EXCLUDED.display_name
                    RETURNING id;";

                var pUserId = cmd.CreateParameter();
                pUserId.ParameterName = "@mezonUserId";
                pUserId.Value = mezonUserId;
                cmd.Parameters.Add(pUserId);

                var pName = cmd.CreateParameter();
                pName.ParameterName = "@displayName";
                pName.Value = string.IsNullOrWhiteSpace(displayName) ? "Learner" : displayName;
                cmd.Parameters.Add(pName);

                var scalar = await cmd.ExecuteScalarAsync();
                if (scalar is Guid g)
                {
                    return g;
                }
                if (scalar != null && Guid.TryParse(scalar.ToString(), out var parsed))
                {
                    return parsed;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ [ClanQuizService] Không thể upsert user cho mezonUserId {MezonUserId}. Sử dụng Guid xác định từ MD5 của mezonUserId.", mezonUserId);
            }
        }

        if (string.IsNullOrWhiteSpace(mezonUserId))
        {
            return Guid.NewGuid();
        }

        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(mezonUserId));
        return new Guid(hash);
    }

    private async Task AwardXpAsync(
        Guid userId,
        Guid sessionId,
        int xpDelta,
        bool isWinner,
        string selectedKey,
        int responseMs)
    {
        if (!_dbContext.Database.IsRelational())
        {
            return;
        }

        try
        {
            var idempotencyKey = $"clan_quiz_{(isWinner ? "win" : "ans")}_{sessionId}_{userId}";
            var metadata = JsonSerializer.Serialize(new
            {
                sessionId,
                isWinner,
                selectedKey,
                responseMs
            }, JsonOptions);

            const string awardXpSql = @"
                INSERT INTO xp_ledger (id, user_id, source_type, source_id, xp_delta, awarded_at, idempotency_key, metadata)
                VALUES (gen_random_uuid(), {0}, 'clan_quiz', {1}, {2}, now(), {3}, {4}::jsonb)
                ON CONFLICT (idempotency_key) DO NOTHING;";

            await _dbContext.Database.ExecuteSqlRawAsync(
                awardXpSql,
                userId,
                sessionId,
                xpDelta,
                idempotencyKey,
                metadata);

            _logger.LogInformation("🎖️ [ClanQuiz] Đã cộng {Xp} XP cho User {UserId} (Session: {SessionId})", xpDelta, userId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [ClanQuiz] Lỗi khi ghi nhận điểm XP vào xp_ledger");
        }
    }

    private async Task<bool> AwardScheduledXpAsync(
        Guid userId,
        Guid sessionId,
        int xpDelta,
        string globalIdempotencyKey,
        string selectedKey,
        int responseMs)
    {
        if (!_dbContext.Database.IsRelational())
        {
            return true;
        }

        try
        {
            var metadata = JsonSerializer.Serialize(new
            {
                sessionId,
                isWinner = (xpDelta == 15),
                selectedKey,
                responseMs
            }, JsonOptions);

            const string awardXpSql = @"
                INSERT INTO xp_ledger (id, user_id, source_type, source_id, xp_delta, awarded_at, idempotency_key, metadata)
                VALUES (gen_random_uuid(), {0}, 'clan_quiz', {1}, {2}, now(), {3}, {4}::jsonb)
                ON CONFLICT (idempotency_key) DO NOTHING;";

            var rowsAffected = await _dbContext.Database.ExecuteSqlRawAsync(
                awardXpSql,
                userId,
                sessionId,
                xpDelta,
                globalIdempotencyKey,
                metadata);

            if (rowsAffected > 0)
            {
                _logger.LogInformation("🎖️ [ScheduledQuiz] Đã cộng {Xp} XP cho User {UserId} (Session: {SessionId}, Key: {Key})", xpDelta, userId, sessionId, globalIdempotencyKey);
                return true;
            }

            _logger.LogWarning("⚠️ [ScheduledQuiz] User {UserId} đã nhận XP cho slot này (Key: {Key}). Không cộng thêm XP.", userId, globalIdempotencyKey);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [ScheduledQuiz] Lỗi khi cộng XP cho User {UserId}", userId);
            return false;
        }
    }

    private static List<InternalQuizOption> ParseOptions(string optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson))
        {
            return new List<InternalQuizOption>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<InternalQuizOption>>(optionsJson, JsonOptions) ?? new List<InternalQuizOption>();
        }
        catch
        {
            return new List<InternalQuizOption>();
        }
    }

    private static List<InternalQuizResponse> ParseResponses(string responsesJson)
    {
        if (string.IsNullOrWhiteSpace(responsesJson))
        {
            return new List<InternalQuizResponse>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<InternalQuizResponse>>(responsesJson, JsonOptions) ?? new List<InternalQuizResponse>();
        }
        catch
        {
            return new List<InternalQuizResponse>();
        }
    }

    private class InternalQuizOption
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("is_correct")]
        public bool IsCorrect { get; set; }
    }

    private class InternalQuizResponse
    {
        [JsonPropertyName("user_id")]
        public Guid UserId { get; set; }

        [JsonPropertyName("mezon_user_id")]
        public string MezonUserId { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("selected_key")]
        public string SelectedKey { get; set; } = string.Empty;

        [JsonPropertyName("is_correct")]
        public bool IsCorrect { get; set; }

        [JsonPropertyName("response_ms")]
        public int ResponseMs { get; set; }

        [JsonPropertyName("responded_at")]
        public string RespondedAt { get; set; } = string.Empty;

        [JsonPropertyName("xp_awarded")]
        public int XpAwarded { get; set; }
    }
}
