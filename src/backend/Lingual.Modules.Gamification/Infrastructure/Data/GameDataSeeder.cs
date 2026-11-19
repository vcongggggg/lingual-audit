using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lingual.Modules.Gamification.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lingual.Modules.Gamification.Infrastructure.Data;

public static class GameDataSeeder
{
    public static async Task SeedAsync(GamificationDbContext db, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        var seedDir = ResolveSeedDirectory(logger);

        // 1. Seed GameWordPools (500 items)
        if (!await db.GameWordPools.AnyAsync(cancellationToken))
        {
            var seeded = false;
            if (seedDir != null)
            {
                var wordPoolPath = Path.Combine(seedDir, "game-word-pools.json");
                if (File.Exists(wordPoolPath))
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(wordPoolPath, cancellationToken);
                        using var doc = JsonDocument.Parse(json);
                        var pool = new List<GameWordPool>();

                        foreach (var elem in doc.RootElement.EnumerateArray())
                        {
                            var id = elem.GetProperty("id").GetString() ?? Guid.NewGuid().ToString("N");
                            var target = elem.GetProperty("targetText").GetString() ?? "";
                            var trans = elem.GetProperty("translation").GetString() ?? "";
                            var phonetic = elem.TryGetProperty("phonetic", out var ph) ? ph.GetString() : null;
                            var topic = elem.TryGetProperty("topic", out var top) ? top.GetString() ?? "general" : "general";
                            var topicVi = elem.TryGetProperty("topicVi", out var topVi) ? topVi.GetString() : null;
                            var cefr = elem.TryGetProperty("cefrLevel", out var lvl) ? lvl.GetString() ?? "A1" : "A1";
                            var ex = elem.TryGetProperty("exampleSentence", out var exProp) ? exProp.GetString() : null;
                            var exVi = elem.TryGetProperty("exampleTranslation", out var exViProp) ? exViProp.GetString() : null;
                            var source = elem.TryGetProperty("source", out var sProp) ? sProp.GetString() ?? "ai-generated" : "ai-generated";
                            var verified = elem.TryGetProperty("verified", out var vProp) && vProp.GetBoolean();

                            pool.Add(new GameWordPool
                            {
                                Id = id,
                                TargetText = target,
                                Translation = trans,
                                Phonetic = phonetic,
                                Topic = topic,
                                TopicVi = topicVi,
                                CefrLevel = cefr,
                                ExampleSentence = ex,
                                ExampleTranslation = exVi,
                                Source = source,
                                Verified = verified,
                                CreatedAt = DateTime.UtcNow
                            });
                        }

                        if (pool.Count > 0)
                        {
                            await db.GameWordPools.AddRangeAsync(pool, cancellationToken);
                            await db.SaveChangesAsync(cancellationToken);
                            logger?.LogInformation("Seeded {Count} real items into game_word_pools from game-word-pools.json", pool.Count);
                            seeded = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, "Failed to parse game-word-pools.json. Falling back to default.");
                    }
                }
            }

            if (!seeded)
            {
                logger?.LogWarning("Using fallback hard-coded 8 words for game_word_pools.");
                var fallbackPool = new List<GameWordPool>
                {
                    new() { Id = "gm-fb-001", TargetText = "elephant", Translation = "con voi", Phonetic = "/ˈelɪfənt/", Topic = "animals", TopicVi = "Động vật", CefrLevel = "A1", Source = "ai-generated", Verified = false },
                    new() { Id = "gm-fb-002", TargetText = "lion", Translation = "con sư tử", Phonetic = "/ˈlaɪən/", Topic = "animals", TopicVi = "Động vật", CefrLevel = "A1", Source = "ai-generated", Verified = false },
                    new() { Id = "gm-fb-003", TargetText = "tiger", Translation = "con hổ", Phonetic = "/ˈtaɪɡər/", Topic = "animals", TopicVi = "Động vật", CefrLevel = "A1", Source = "ai-generated", Verified = false },
                    new() { Id = "gm-fb-004", TargetText = "monkey", Translation = "con khỉ", Phonetic = "/ˈmʌŋki/", Topic = "animals", TopicVi = "Động vật", CefrLevel = "A1", Source = "ai-generated", Verified = false },
                    new() { Id = "gm-fb-005", TargetText = "coffee", Translation = "cà phê", Phonetic = "/ˈkɔːfi/", Topic = "food", TopicVi = "Thực phẩm", CefrLevel = "A1", Source = "ai-generated", Verified = false },
                    new() { Id = "gm-fb-006", TargetText = "routine", Translation = "thói quen", Phonetic = "/ruːˈtiːn/", Topic = "daily", TopicVi = "Hằng ngày", CefrLevel = "A2", Source = "ai-generated", Verified = false },
                    new() { Id = "gm-fb-007", TargetText = "commute", Translation = "đi làm", Phonetic = "/kəˈmjuːt/", Topic = "daily", TopicVi = "Hằng ngày", CefrLevel = "B1", Source = "ai-generated", Verified = false },
                    new() { Id = "gm-fb-008", TargetText = "airport", Translation = "sân bay", Phonetic = "/ˈerpɔːrt/", Topic = "travel", TopicVi = "Du lịch", CefrLevel = "A1", Source = "ai-generated", Verified = false }
                };
                await db.GameWordPools.AddRangeAsync(fallbackPool, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        // 2. Seed GameQuestions (fill_blitz ~300, sentence_scramble 20, lingo_wordle 10)
        var hasFillBlitz = await db.GameQuestions.AnyAsync(q => q.GameType == "fill_blitz", cancellationToken);
        var hasScramble = await db.GameQuestions.AnyAsync(q => q.GameType == "sentence_scramble", cancellationToken);
        var hasWordle = await db.GameQuestions.AnyAsync(q => q.GameType == "lingo_wordle", cancellationToken);

        if (!hasFillBlitz || !hasScramble || !hasWordle)
        {
            var questionsToAdd = new List<GameQuestion>();

            if (seedDir != null)
            {
                // A. fill_blitz from daily-vocab-questions.json
                if (!hasFillBlitz)
                {
                    var fillPath = Path.Combine(seedDir, "daily-vocab-questions.json");
                    if (File.Exists(fillPath))
                    {
                        try
                        {
                            var json = await File.ReadAllTextAsync(fillPath, cancellationToken);
                            using var doc = JsonDocument.Parse(json);
                            foreach (var elem in doc.RootElement.EnumerateArray())
                            {
                                var id = elem.GetProperty("id").GetString() ?? Guid.NewGuid().ToString("N");
                                var prompt = elem.GetProperty("question").GetString() ?? "";
                                var answer = elem.GetProperty("correctAnswer").GetString() ?? "";
                                var level = elem.TryGetProperty("cefrLevel", out var lProp) ? lProp.GetString() ?? "A2" : "A2";
                                var expl = elem.TryGetProperty("explanation", out var eProp) ? eProp.GetString() : null;

                                var options = new List<string>();
                                if (elem.TryGetProperty("options", out var optsProp))
                                {
                                    foreach (var o in optsProp.EnumerateArray())
                                    {
                                        var optStr = o.GetString();
                                        if (!string.IsNullOrWhiteSpace(optStr)) options.Add(optStr);
                                    }
                                }

                                questionsToAdd.Add(new GameQuestion
                                {
                                    Id = $"fb_{id}",
                                    GameType = "fill_blitz",
                                    Prompt = prompt,
                                    Answer = answer,
                                    ChoicesJson = JsonSerializer.Serialize(options),
                                    Level = level,
                                    Source = "dailyVocabQuestion",
                                    SourceRef = expl,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Failed to parse daily-vocab-questions.json.");
                        }
                    }
                }

                // B. sentence_scramble from sentence-scrambles.json
                if (!hasScramble)
                {
                    var scramblePath = Path.Combine(seedDir, "sentence-scrambles.json");
                    if (File.Exists(scramblePath))
                    {
                        try
                        {
                            var json = await File.ReadAllTextAsync(scramblePath, cancellationToken);
                            using var doc = JsonDocument.Parse(json);
                            foreach (var elem in doc.RootElement.EnumerateArray())
                            {
                                var id = elem.GetProperty("id").GetString() ?? Guid.NewGuid().ToString("N");
                                var prompt = elem.GetProperty("prompt").GetString() ?? "";
                                var answer = elem.GetProperty("answer").GetString() ?? "";
                                var level = elem.TryGetProperty("level", out var lProp) ? lProp.GetString() ?? "A2" : "A2";

                                var tokens = new List<string>();
                                if (elem.TryGetProperty("tokens", out var tProp))
                                {
                                    foreach (var t in tProp.EnumerateArray())
                                    {
                                        var tStr = t.GetString();
                                        if (!string.IsNullOrWhiteSpace(tStr)) tokens.Add(tStr);
                                    }
                                }

                                questionsToAdd.Add(new GameQuestion
                                {
                                    Id = id,
                                    GameType = "sentence_scramble",
                                    Prompt = prompt,
                                    Answer = answer,
                                    ChoicesJson = JsonSerializer.Serialize(tokens),
                                    Level = level,
                                    Source = "universalVocabulary",
                                    SourceRef = prompt,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Failed to parse sentence-scrambles.json.");
                        }
                    }
                }

                // C. lingo_wordle from wordle-words.json
                if (!hasWordle)
                {
                    var wordlePath = Path.Combine(seedDir, "wordle-words.json");
                    if (File.Exists(wordlePath))
                    {
                        try
                        {
                            var json = await File.ReadAllTextAsync(wordlePath, cancellationToken);
                            using var doc = JsonDocument.Parse(json);
                            foreach (var elem in doc.RootElement.EnumerateArray())
                            {
                                var word = elem.GetProperty("word").GetString() ?? "";
                                var hint = elem.GetProperty("hint").GetString() ?? "";
                                var meaningVi = elem.GetProperty("meaningVi").GetString() ?? "";
                                var category = elem.TryGetProperty("category", out var cProp) ? cProp.GetString() ?? "General" : "General";

                                questionsToAdd.Add(new GameQuestion
                                {
                                    Id = $"wdl_{word.ToLowerInvariant()}",
                                    GameType = "lingo_wordle",
                                    Prompt = hint,
                                    Answer = word.ToUpperInvariant(),
                                    ChoicesJson = JsonSerializer.Serialize(new { category, meaningVi }),
                                    Level = "B2",
                                    Source = "universalVocabulary",
                                    SourceRef = meaningVi,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Failed to parse wordle-words.json.");
                        }
                    }
                }
            }

            if (questionsToAdd.Count > 0)
            {
                await db.GameQuestions.AddRangeAsync(questionsToAdd, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
                logger?.LogInformation("Seeded {Count} real items into game_questions (fill_blitz, sentence_scramble, lingo_wordle)", questionsToAdd.Count);
            }
            else if (!await db.GameQuestions.AnyAsync(cancellationToken))
            {
                logger?.LogWarning("Using fallback hard-coded 2 questions for game_questions.");
                var fallbackQuestions = new List<GameQuestion>
                {
                    new()
                    {
                        Id = "fb-wordle-01",
                        GameType = "lingo_wordle",
                        Prompt = "Tính từ chỉ người có trí tuệ nhạy bén hoặc thiết bị hiện đại",
                        Answer = "SMART",
                        Level = "B1",
                        Source = "universalVocabulary",
                        SourceRef = "Thông minh, khéo léo",
                        ChoicesJson = "{\"category\":\"Personality\",\"meaningVi\":\"Thông minh, khéo léo\"}"
                    },
                    new()
                    {
                        Id = "fb-scramble-01",
                        GameType = "sentence_scramble",
                        Prompt = "Tôi thích đọc sách vào buổi tối",
                        Answer = "I like to read books in the evening",
                        Level = "A2",
                        Source = "universalVocabulary",
                        ChoicesJson = "[\"in\",\"to\",\"I\",\"evening\",\"read\",\"books\",\"the\",\"like\"]"
                    }
                };
                await db.GameQuestions.AddRangeAsync(fallbackQuestions, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private static string? ResolveSeedDirectory(ILogger? logger)
    {
        var candidates = new List<string>
        {
            Path.Combine(AppContext.BaseDirectory, "Infrastructure", "Data", "SeedData"),
            Path.Combine(AppContext.BaseDirectory, "SeedData"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "backend", "Lingual.Modules.Gamification", "Infrastructure", "Data", "SeedData"),
            Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Data", "SeedData"),
            @"C:\Study\HocKy6\MezonCampusStudio\src\backend\Lingual.Modules.Gamification\Infrastructure\Data\SeedData"
        };

        foreach (var dir in candidates)
        {
            if (Directory.Exists(dir))
            {
                return dir;
            }
        }

        logger?.LogWarning("Could not locate SeedData directory in candidate paths: {Paths}", string.Join(", ", candidates));
        return null;
    }
}
