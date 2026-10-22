using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using StackExchange.Redis;

namespace Lingual.Api.Controllers;

[ApiController]
[Route("api/v1/content")]
public sealed class ContentController(IConfiguration configuration, IConnectionMultiplexer redis) : ControllerBase
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing.");
    private const string DemoUserKey = "lingual-local-demo-user";
    private const string LeaderboardKey = "leaderboard:all-time";

    [HttpGet("vocabulary")]
    public async Task<IActionResult> GetVocabulary(
        [FromQuery] int limit = 50,
        [FromQuery(Name = "source_language")] string sourceLanguage = "en",
        [FromQuery(Name = "target_language")] string targetLanguage = "vi",
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 500);
        const string sql = """
            SELECT id, term, normalized_term, source_language, target_language, ipa,
                   part_of_speech, meaning, pronunciation_audio_url, image_url, examples::text
            FROM vocabulary_items
            WHERE status = 'published' AND source_language = @source AND target_language = @target
            ORDER BY normalized_term
            LIMIT @limit
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("source", sourceLanguage);
        command.Parameters.AddWithValue("target", targetLanguage);
        command.Parameters.AddWithValue("limit", limit);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var words = new List<object>();
        while (await reader.ReadAsync(cancellationToken))
        {
            words.Add(new
            {
                id = reader.GetGuid(0).ToString(),
                term = reader.GetString(1),
                normalized_term = reader.GetString(2),
                source_language = reader.GetString(3),
                target_language = reader.GetString(4),
                ipa = reader.IsDBNull(5) ? null : reader.GetString(5),
                part_of_speech = reader.IsDBNull(6) ? null : reader.GetString(6),
                meaning = reader.GetString(7),
                pronunciation_audio_url = reader.IsDBNull(8) ? null : reader.GetString(8),
                image_url = reader.IsDBNull(9) ? null : reader.GetString(9),
                examples = JsonSerializer.Deserialize<JsonElement>(reader.GetString(10)),
            });
        }

        return Ok(new { total = words.Count, words });
    }

    [HttpGet("quizzes")]
    public async Task<IActionResult> GetQuizQuestions(
        [FromQuery] int limit = 10,
        [FromQuery] string? level = null,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 300);
        const string sql = """
            SELECT id, sort_order, level, difficulty, question_type, prompt,
                   options::text, explanation, points
            FROM quiz_questions
            WHERE status = 'published'
              AND question_type = 'multiple_choice'
              AND (CAST(@level AS varchar) IS NULL OR level = CAST(@level AS varchar))
            ORDER BY sort_order, id
            LIMIT @limit
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("level", (object?)level ?? DBNull.Value);
        command.Parameters.AddWithValue("limit", limit);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var questions = new List<object>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var options = JsonSerializer.Deserialize<JsonElement>(reader.GetString(6));
            var correctAnswer = options.EnumerateArray()
                .FirstOrDefault(option => option.TryGetProperty("is_correct", out var isCorrect) && isCorrect.GetBoolean());
            var correctKey = correctAnswer.ValueKind == JsonValueKind.Object && correctAnswer.TryGetProperty("key", out var key)
                ? key.GetString()
                : null;

            questions.Add(new
            {
                id = reader.GetGuid(0).ToString(),
                sort_order = reader.GetInt16(1),
                level = reader.IsDBNull(2) ? "A1" : reader.GetString(2),
                difficulty = reader.IsDBNull(3) ? 1 : reader.GetInt16(3),
                question_type = reader.GetString(4),
                prompt = reader.GetString(5),
                options,
                correct_answer = correctKey,
                explanation = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                points = reader.GetInt16(8),
            });
        }

        return Ok(new { total = questions.Count, questions });
    }

    [HttpPost("reviews")]
    public async Task<IActionResult> SaveReview([FromBody] ReviewRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.VocabularyId, out var vocabularyId) || request.Rating is not ("again" or "hard" or "good" or "easy") ||
            request.Repetitions < 0 || request.IntervalDays < 0 || request.EaseFactor is < 1.3m or > 5m || string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return BadRequest(new { error = "Invalid review payload" });

        var xp = request.Rating switch { "again" => 5, "hard" => 15, "good" => 20, _ => 25 };
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        if (await HasIdempotencyKey(connection, transaction, request.IdempotencyKey, cancellationToken))
        {
            await transaction.CommitAsync(cancellationToken);
            return Ok(new { saved = true, xp = 0 });
        }
        var userId = await EnsureDemoUser(connection, transaction, cancellationToken);
        var history = JsonSerializer.Serialize(new { rating = request.Rating, quality = request.Quality, time_spent_ms = request.TimeSpentMs, reviewed_at = DateTimeOffset.UtcNow });
        const string srsSql = """
            INSERT INTO srs_cards(user_id, vocabulary_id, repetitions, interval_days, ease_factor, next_due_at, last_reviewed_at, lapses, review_history)
            VALUES (@user, @vocab, @repetitions, @interval, @ease, @due, now(), CASE WHEN @rating = 'again' THEN 1 ELSE 0 END, jsonb_build_array(@history::jsonb))
            ON CONFLICT (user_id, vocabulary_id) DO UPDATE SET
              repetitions = EXCLUDED.repetitions, interval_days = EXCLUDED.interval_days, ease_factor = EXCLUDED.ease_factor,
              next_due_at = EXCLUDED.next_due_at, last_reviewed_at = now(),
              lapses = srs_cards.lapses + CASE WHEN @rating = 'again' THEN 1 ELSE 0 END,
              review_history = srs_cards.review_history || EXCLUDED.review_history
            """;
        await using (var command = new NpgsqlCommand(srsSql, connection, transaction))
        {
            command.Parameters.AddWithValue("user", userId); command.Parameters.AddWithValue("vocab", vocabularyId);
            command.Parameters.AddWithValue("repetitions", request.Repetitions); command.Parameters.AddWithValue("interval", request.IntervalDays);
            command.Parameters.AddWithValue("ease", request.EaseFactor); command.Parameters.AddWithValue("due", request.NextDueAt);
            command.Parameters.AddWithValue("rating", request.Rating); command.Parameters.AddWithValue("history", history);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        var added = await AddXp(connection, transaction, userId, xp, "srs_review", request.IdempotencyKey, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        if (added) await redis.GetDatabase().SortedSetIncrementAsync(LeaderboardKey, DemoUserKey, xp);
        return Ok(new { saved = true, xp = added ? xp : 0 });
    }

    [HttpPost("quiz-attempts")]
    public async Task<IActionResult> SaveQuizAttempt([FromBody] QuizAttemptRequest request, CancellationToken cancellationToken)
    {
        if (request.Answers is null || request.Answers.Count is < 1 or > 300 || string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return BadRequest(new { error = "Invalid quiz attempt" });
        var answersJson = JsonSerializer.Serialize(request.Answers);
        var xp = Math.Clamp(request.Xp, 0, 10000);
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        if (await HasIdempotencyKey(connection, transaction, request.IdempotencyKey, cancellationToken))
        {
            await transaction.CommitAsync(cancellationToken);
            return Ok(new { saved = true, xp = 0 });
        }
        var userId = await EnsureDemoUser(connection, transaction, cancellationToken);
        Guid quizId;
        await using (var command = new NpgsqlCommand("INSERT INTO quizzes(code,title,quiz_type,total_questions) VALUES ('local-practice','Local practice','practice',0) ON CONFLICT(code) DO UPDATE SET title=EXCLUDED.title RETURNING id", connection, transaction))
            quizId = (Guid)(await command.ExecuteScalarAsync(cancellationToken))!;
        var correctCount = request.Answers.Count(answer => answer.IsCorrect);
        const string attemptSql = "INSERT INTO quiz_attempts(quiz_id,user_id,status,score,max_score,correct_count,answers_detail,submitted_at) VALUES (@quiz,@user,'submitted',@score,@max,@correct,@answers::jsonb,now())";
        await using (var command = new NpgsqlCommand(attemptSql, connection, transaction))
        {
            command.Parameters.AddWithValue("quiz", quizId); command.Parameters.AddWithValue("user", userId);
            command.Parameters.AddWithValue("score", correctCount); command.Parameters.AddWithValue("max", request.Answers.Count);
            command.Parameters.AddWithValue("correct", correctCount); command.Parameters.AddWithValue("answers", answersJson);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        var added = await AddXp(connection, transaction, userId, xp, "clan_quiz", request.IdempotencyKey, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        if (added) await redis.GetDatabase().SortedSetIncrementAsync(LeaderboardKey, DemoUserKey, xp);
        return Ok(new { saved = true, xp = added ? xp : 0, correct_answers = correctCount });
    }

    [HttpGet("leaderboard")]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int limit = 20, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 100);
        var db = redis.GetDatabase();
        var entries = await db.SortedSetRangeByRankWithScoresAsync(LeaderboardKey, 0, limit - 1, Order.Descending);
        if (entries.Length == 0)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            const string sql = "SELECT u.mezon_user_id, COALESCE(u.display_name,'Learner'), SUM(x.xp_delta) FROM xp_ledger x JOIN users u ON u.id=x.user_id GROUP BY u.mezon_user_id,u.display_name ORDER BY 3 DESC LIMIT @limit";
            await using var command = new NpgsqlCommand(sql, connection); command.Parameters.AddWithValue("limit", limit);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var seed = new List<(string member, double score)>();
            while (await reader.ReadAsync(cancellationToken)) seed.Add((reader.GetString(0), reader.GetDouble(2)));
            if (seed.Count > 0) await db.SortedSetAddAsync(LeaderboardKey, seed.Select(x => new SortedSetEntry(x.member, x.score)).ToArray());
            entries = await db.SortedSetRangeByRankWithScoresAsync(LeaderboardKey, 0, limit - 1, Order.Descending);
        }
        return Ok(new { updated_at = DateTimeOffset.UtcNow, entries = entries.Select((e, i) => new { rank = i + 1, user_id = (string)e.Element!, name = (string)e.Element! == DemoUserKey ? "You" : (string)e.Element!, xp = e.Score }) });
    }

    private static async Task<Guid> EnsureDemoUser(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = "INSERT INTO users(mezon_user_id,display_name) VALUES (@key,'Learner') ON CONFLICT(mezon_user_id) DO UPDATE SET last_seen_at=now() RETURNING id";
        await using var command = new NpgsqlCommand(sql, connection, transaction); command.Parameters.AddWithValue("key", DemoUserKey);
        return (Guid)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task<bool> AddXp(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid userId, int xp, string source, string key, CancellationToken cancellationToken)
    {
        if (xp <= 0) return false;
        const string sql = "INSERT INTO xp_ledger(user_id,source_type,xp_delta,idempotency_key) VALUES (@user,@source,@xp,@key) ON CONFLICT(idempotency_key) DO NOTHING";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("user", userId); command.Parameters.AddWithValue("source", source);
        command.Parameters.AddWithValue("xp", xp); command.Parameters.AddWithValue("key", key);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static async Task<bool> HasIdempotencyKey(NpgsqlConnection connection, NpgsqlTransaction transaction, string key, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM xp_ledger WHERE idempotency_key=@key)", connection, transaction);
        command.Parameters.AddWithValue("key", key);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    public sealed record ReviewRequest(string VocabularyId, string Rating, int Quality, int Repetitions, decimal IntervalDays, decimal EaseFactor, DateTimeOffset NextDueAt, int TimeSpentMs, string IdempotencyKey);
    public sealed record QuizAnswer(string QuestionId, string? SelectedKey, bool IsCorrect);
    public sealed record QuizAttemptRequest(List<QuizAnswer> Answers, int Xp, string IdempotencyKey);
}
