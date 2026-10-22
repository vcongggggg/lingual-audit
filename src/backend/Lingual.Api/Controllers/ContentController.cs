using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Lingual.Api.Controllers;

[ApiController]
[Route("api/v1/content")]
public sealed class ContentController(IConfiguration configuration) : ControllerBase
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing.");

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
}
