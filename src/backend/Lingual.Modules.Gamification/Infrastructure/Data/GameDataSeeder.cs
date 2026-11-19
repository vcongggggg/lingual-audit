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
        // 1. Ensure Word Pool has words
        if (!await db.GameWordPools.AnyAsync(cancellationToken))
        {
            logger?.LogInformation("Seeding game_word_pools fallback words...");
            // Minimal fallback pool if empty
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

        // 2. Ensure Game Questions has records
        if (!await db.GameQuestions.AnyAsync(cancellationToken))
        {
            logger?.LogInformation("Seeding game_questions fallback...");
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
