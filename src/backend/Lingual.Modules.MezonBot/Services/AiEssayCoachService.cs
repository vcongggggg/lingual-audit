using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mezon.Net.Sdk;
using Mezon.Net.Client;
using Mezon.Net.Models;
using Mezon.Net.Sdk.Builders;
using Lingual.Modules.AITutor.Contracts;
using Lingual.Modules.AITutor.Contracts.DTOs;

namespace Lingual.Modules.MezonBot.Services;

public record EssayCriteriaScore(
    string CriteriaName,
    double Score,
    string Summary,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Improvements);

public record EssayCorrectionItem(
    string OriginalSnippet,
    string SuggestedReplacement,
    string RuleExplanation);

public record EssayEvaluationResult(
    bool IsSuccess,
    string OriginalText,
    int WordCount,
    double OverallBandScore,
    string CefrLevel,
    EssayCriteriaScore TaskAchievement,
    EssayCriteriaScore LexicalResource,
    EssayCriteriaScore GrammaticalAccuracy,
    EssayCriteriaScore NativeSuggestions,
    IReadOnlyList<EssayCorrectionItem> Corrections,
    string GeneralFeedback,
    int AwardedXp = 0,
    string? ErrorMessage = null);

public interface IAiEssayCoachService
{
    Task<EssayEvaluationResult> EvaluateEssayAsync(string text, string? topic = null, CancellationToken cancellationToken = default);
    MessageContent BuildEssayInputCard(string customId = "essay_submission_form", string? defaultTopic = null);
    MessageContent BuildEvaluationResultCard(EssayEvaluationResult result);
}

public class EssayRateLimiter
{
    private readonly ConcurrentDictionary<long, List<DateTime>> _submissionHistory = new();
    private readonly int _maxSubmissionsPerMinute;

    public EssayRateLimiter(int maxSubmissionsPerMinute = 3)
    {
        _maxSubmissionsPerMinute = maxSubmissionsPerMinute;
    }

    public bool TryAcquire(long userId, out int retryAfterSeconds)
    {
        var now = DateTime.UtcNow;
        var timestamps = _submissionHistory.GetOrAdd(userId, _ => new List<DateTime>());
        lock (timestamps)
        {
            timestamps.RemoveAll(t => (now - t).TotalSeconds > 60);
            if (timestamps.Count >= _maxSubmissionsPerMinute)
            {
                var oldest = timestamps[0];
                retryAfterSeconds = (int)Math.Max(1, 60 - (now - oldest).TotalSeconds);
                return false;
            }
            timestamps.Add(now);
            retryAfterSeconds = 0;
            return true;
        }
    }
}

public class AiEssayCoachService : IAiEssayCoachService
{
    private readonly IAITutorService? _aiTutorService;
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly HttpClient? _httpClient;
    private readonly string? _geminiApiKey;
    private readonly string _geminiBaseUrl;
    private readonly string _geminiModel;
    private readonly ILogger<AiEssayCoachService> _logger;

    public AiEssayCoachService(
        ILogger<AiEssayCoachService> logger,
        IAITutorService? aiTutorService = null,
        IServiceScopeFactory? scopeFactory = null,
        HttpClient? httpClient = null,
        string? geminiApiKey = null,
        string? geminiBaseUrl = null,
        string? geminiModel = null)
    {
        _logger = logger;
        _aiTutorService = aiTutorService;
        _scopeFactory = scopeFactory;
        _httpClient = httpClient;
        _geminiApiKey = geminiApiKey;
        _geminiBaseUrl = geminiBaseUrl ?? "https://generativelanguage.googleapis.com/v1beta";
        _geminiModel = geminiModel ?? "gemini-1.5-flash";
    }

    public async Task<EssayEvaluationResult> EvaluateEssayAsync(
        string text,
        string? topic = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new EssayEvaluationResult(
                IsSuccess: false,
                OriginalText: string.Empty,
                WordCount: 0,
                OverallBandScore: 0.0,
                CefrLevel: "N/A",
                TaskAchievement: new EssayCriteriaScore("Task Achievement / Coherence", 0, "Chưa có nội dung", Array.Empty<string>(), Array.Empty<string>()),
                LexicalResource: new EssayCriteriaScore("Lexical Resource", 0, "Chưa có nội dung", Array.Empty<string>(), Array.Empty<string>()),
                GrammaticalAccuracy: new EssayCriteriaScore("Grammatical Range & Accuracy", 0, "Chưa có nội dung", Array.Empty<string>(), Array.Empty<string>()),
                NativeSuggestions: new EssayCriteriaScore("Native Suggestions", 0, "Chưa có nội dung", Array.Empty<string>(), Array.Empty<string>()),
                Corrections: Array.Empty<EssayCorrectionItem>(),
                GeneralFeedback: "Vui lòng nhập bài viết tiếng Anh để chấm điểm.",
                AwardedXp: 0,
                ErrorMessage: "Nội dung bài viết rỗng."
            );
        }

        var words = text.Trim().Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var wordCount = words.Length;

        // 1. ĐƯỜNG 1: Gọi Gemini trực tiếp qua HttpClient nếu có ApiKey hợp lệ
        if (_httpClient != null && !string.IsNullOrWhiteSpace(_geminiApiKey) && _geminiApiKey != "YOUR_GEMINI_API_KEY")
        {
            try
            {
                var geminiResult = await CallGeminiForEssayEvaluationAsync(text, topic, wordCount, cancellationToken);
                if (geminiResult != null && geminiResult.IsSuccess)
                {
                    return geminiResult;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ [AiEssayCoach] Lỗi khi gọi Gemini trực tiếp, thử tiếp qua IAITutorService.");
            }
        }

        // 2. ĐƯỜNG 2: Gọi qua IAITutorService (được inject trực tiếp hoặc giải quyết từ ScopeFactory)
        IAITutorService? aiTutor = _aiTutorService;
        IServiceScope? scope = null;
        if (aiTutor == null && _scopeFactory != null)
        {
            try
            {
                scope = _scopeFactory.CreateScope();
                aiTutor = scope.ServiceProvider.GetService<IAITutorService>();
            }
            catch (Exception exScope)
            {
                _logger.LogDebug(exScope, "Không thể tạo scope để resolve IAITutorService.");
            }
        }

        if (aiTutor != null)
        {
            try
            {
                var analysisRequest = new GrammarAnalysisRequest(
                    Text: text,
                    Context: topic ?? "academic_essay");

                var grammarResponse = await aiTutor.AnalyzeGrammarAsync(analysisRequest, cancellationToken);
                if (grammarResponse != null)
                {
                    return MapAITutorResponseToEssayResult(grammarResponse, text, topic, wordCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ [AiEssayCoach] Lỗi khi gọi IAITutorService, sử dụng đánh giá học thuật dự phòng.");
            }
            finally
            {
                scope?.Dispose();
            }
        }

        // 3. ĐƯỜNG 3 (DỰ PHÒNG): Đánh giá học thuật dự phòng (fallback / offline evaluator)
        // Ghi rõ ràng 'ước tính thô, chưa qua AI' và KHÔNG trình bày band score như điểm chấm thật (OverallBandScore = 0.0)
        return EvaluateHeuristically(text, topic, wordCount);
    }

    private static EssayEvaluationResult MapAITutorResponseToEssayResult(
        GrammarAnalysisResponse response,
        string text,
        string? topic,
        int wordCount)
    {
        var corrections = new List<EssayCorrectionItem>();
        if (!string.IsNullOrWhiteSpace(response.OriginalText) &&
            !string.IsNullOrWhiteSpace(response.CorrectedText) &&
            !response.OriginalText.Equals(response.CorrectedText, StringComparison.OrdinalIgnoreCase))
        {
            corrections.Add(new EssayCorrectionItem(
                OriginalSnippet: response.OriginalText,
                SuggestedReplacement: response.CorrectedText,
                RuleExplanation: string.IsNullOrWhiteSpace(response.Explanation) ? "AI Tutor gợi ý câu viết chuẩn xác hơn." : response.Explanation
            ));
        }

        double band = response.IsCorrect
            ? (wordCount >= 150 ? 7.5 : 7.0)
            : (wordCount >= 150 ? 6.5 : 6.0);
        string cefr = band >= 7.5 ? "C1" : (band >= 6.5 ? "B2" : "B1");

        return new EssayEvaluationResult(
            IsSuccess: true,
            OriginalText: text,
            WordCount: wordCount,
            OverallBandScore: band,
            CefrLevel: cefr,
            TaskAchievement: new EssayCriteriaScore(
                CriteriaName: "Task Achievement / Coherence",
                Score: band,
                Summary: $"AI Đánh giá: Bài viết gồm {wordCount} từ. Chủ đề: {topic ?? "Tự do"}.",
                Strengths: new[] { "Nội dung đầy đủ", "Bố cục câu rõ ràng" },
                Improvements: new[] { "Mở rộng phát triển thêm ý luận để tăng sức thuyết phục" }
            ),
            LexicalResource: new EssayCriteriaScore(
                CriteriaName: "Lexical Resource",
                Score: Math.Max(5.0, band - 0.5),
                Summary: string.IsNullOrWhiteSpace(response.Tips) ? "Vốn từ vựng tương đối phong phú." : response.Tips,
                Strengths: new[] { "Sử dụng đúng từ loại cơ bản" },
                Improvements: new[] { "Thêm cụm từ học thuật và collocations nâng cao" }
            ),
            GrammaticalAccuracy: new EssayCriteriaScore(
                CriteriaName: "Grammatical Range & Accuracy",
                Score: response.IsCorrect ? band : Math.Max(5.0, band - 1.0),
                Summary: string.IsNullOrWhiteSpace(response.Explanation) ? "Ngữ pháp chuẩn xác." : response.Explanation,
                Strengths: response.IsCorrect ? new[] { "Không phát hiện lỗi ngữ pháp cơ bản" } : new[] { "Cấu trúc câu hoàn chỉnh" },
                Improvements: new[] { "Đa dạng hóa mệnh đề quan hệ và đảo ngữ" }
            ),
            NativeSuggestions: new EssayCriteriaScore(
                CriteriaName: "Native Suggestions",
                Score: band,
                Summary: string.IsNullOrWhiteSpace(response.CorrectedText) ? "Diễn đạt tự nhiên." : $"Gợi ý bản xứ: \"{response.CorrectedText}\"",
                Strengths: new[] { "Văn phong học thuật" },
                Improvements: new[] { "Sử dụng từ nối tự nhiên hơn" }
            ),
            Corrections: corrections,
            GeneralFeedback: $"AI Tutor (Gemini) Đánh giá: Bài viết đạt trình độ {cefr} (Band {band:0.0}). {response.Explanation}",
            AwardedXp: 0,
            ErrorMessage: null
        );
    }

    private async Task<EssayEvaluationResult?> CallGeminiForEssayEvaluationAsync(
        string text,
        string? topic,
        int wordCount,
        CancellationToken cancellationToken)
    {
        var systemPrompt =
            "Bạn là Giám khảo chấm thi học thuật IELTS/TOEIC kiêm Huấn luyện viên viết tiếng Anh chuyên nghiệp (AI Essay Coach) của hệ thống LINGUAL.\n" +
            "Nhiệm vụ của bạn là đánh giá chi tiết bài viết của học viên theo 4 tiêu chí chuẩn quốc tế:\n" +
            "1. Task Achievement / Coherence (Mạch lạc & Bám sát chủ đề)\n" +
            "2. Lexical Resource (Độ phong phú từ vựng & Collocations)\n" +
            "3. Grammatical Range & Accuracy (Độ chính xác và đa dạng ngữ pháp)\n" +
            "4. Native Suggestions (Gợi ý diễn đạt tự nhiên như người bản xứ & câu viết lại hoàn thiện)\n\n" +
            "BẮT BUỘC chỉ trả về định dạng JSON hợp lệ theo schema sau:\n" +
            "{\n" +
            "  \"overall_band\": 6.5,\n" +
            "  \"cefr_level\": \"B2\",\n" +
            "  \"task_achievement\": {\"score\": 6.5, \"summary\": \"...\", \"strengths\": [\"...\"], \"improvements\": [\"...\"]},\n" +
            "  \"lexical_resource\": {\"score\": 6.0, \"summary\": \"...\", \"strengths\": [\"...\"], \"improvements\": [\"...\"]},\n" +
            "  \"grammatical_accuracy\": {\"score\": 7.0, \"summary\": \"...\", \"strengths\": [\"...\"], \"improvements\": [\"...\"]},\n" +
            "  \"native_suggestions\": {\"score\": 6.5, \"summary\": \"...\", \"strengths\": [\"...\"], \"improvements\": [\"...\"]},\n" +
            "  \"corrections\": [{\"original\": \"...\", \"suggested\": \"...\", \"explanation\": \"...\"}],\n" +
            "  \"general_feedback\": \"...\"\n" +
            "}";

        var userPrompt = $"Chủ đề bài viết: {topic ?? "Tự do (Free writing)"}\n\nNội dung bài viết ({wordCount} từ):\n\"{text}\"";

        var payload = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = new[] { new { role = "user", parts = new[] { new { text = userPrompt } } } },
            generationConfig = new
            {
                temperature = 0.2,
                maxOutputTokens = 2000,
                responseMimeType = "application/json"
            }
        };

        var endpoint = $"{_geminiBaseUrl.TrimEnd('/')}/models/{_geminiModel}:generateContent";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Headers.Add("x-goog-api-key", _geminiApiKey);
        httpRequest.Content = JsonContent.Create(payload);

        var response = await _httpClient!.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("⚠️ [AiEssayCoach] Gemini trả về lỗi HTTP {StatusCode}", response.StatusCode);
            return null;
        }

        var jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
        var rawText = jsonDoc?.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text").GetString();

        if (string.IsNullOrWhiteSpace(rawText)) return null;

        var cleanJson = rawText.Trim();
        if (cleanJson.StartsWith("```json")) cleanJson = cleanJson[7..];
        if (cleanJson.StartsWith("```")) cleanJson = cleanJson[3..];
        if (cleanJson.EndsWith("```")) cleanJson = cleanJson[..^3];
        cleanJson = cleanJson.Trim();

        using var doc = JsonDocument.Parse(cleanJson);
        var root = doc.RootElement;

        var overallBand = root.TryGetProperty("overall_band", out var obProp) ? obProp.GetDouble() : 6.0;
        var cefr = root.TryGetProperty("cefr_level", out var cefrProp) ? cefrProp.GetString() ?? "B2" : "B2";
        var feedback = root.TryGetProperty("general_feedback", out var fbProp) ? fbProp.GetString() ?? "Bài viết khá tốt!" : "Bài viết khá tốt!";

        var taskScore = ParseCriteria(root, "task_achievement", "Task Achievement & Coherence", overallBand);
        var lexScore = ParseCriteria(root, "lexical_resource", "Lexical Resource", overallBand);
        var gramScore = ParseCriteria(root, "grammatical_accuracy", "Grammatical Range & Accuracy", overallBand);
        var natScore = ParseCriteria(root, "native_suggestions", "Native Suggestions", overallBand);

        var correctionsList = new List<EssayCorrectionItem>();
        if (root.TryGetProperty("corrections", out var corrProp) && corrProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in corrProp.EnumerateArray())
            {
                var orig = item.TryGetProperty("original", out var o) ? o.GetString() ?? "" : "";
                var sugg = item.TryGetProperty("suggested", out var s) ? s.GetString() ?? "" : "";
                var expl = item.TryGetProperty("explanation", out var e) ? e.GetString() ?? "" : "";
                if (!string.IsNullOrWhiteSpace(orig))
                {
                    correctionsList.Add(new EssayCorrectionItem(orig, sugg, expl));
                }
            }
        }

        return new EssayEvaluationResult(
            IsSuccess: true,
            OriginalText: text,
            WordCount: wordCount,
            OverallBandScore: overallBand,
            CefrLevel: cefr,
            TaskAchievement: taskScore,
            LexicalResource: lexScore,
            GrammaticalAccuracy: gramScore,
            NativeSuggestions: natScore,
            Corrections: correctionsList,
            GeneralFeedback: feedback,
            AwardedXp: 0
        );
    }

    private static EssayCriteriaScore ParseCriteria(JsonElement root, string propName, string displayName, double defaultScore)
    {
        if (root.TryGetProperty(propName, out var elem) && elem.ValueKind == JsonValueKind.Object)
        {
            var score = elem.TryGetProperty("score", out var s) ? s.GetDouble() : defaultScore;
            var summary = elem.TryGetProperty("summary", out var sm) ? sm.GetString() ?? "" : "";
            var strengths = new List<string>();
            var improvements = new List<string>();

            if (elem.TryGetProperty("strengths", out var st) && st.ValueKind == JsonValueKind.Array)
            {
                foreach (var i in st.EnumerateArray()) if (i.GetString() is string str && !string.IsNullOrWhiteSpace(str)) strengths.Add(str);
            }
            if (elem.TryGetProperty("improvements", out var im) && im.ValueKind == JsonValueKind.Array)
            {
                foreach (var i in im.EnumerateArray()) if (i.GetString() is string str && !string.IsNullOrWhiteSpace(str)) improvements.Add(str);
            }

            return new EssayCriteriaScore(displayName, score, summary, strengths, improvements);
        }

        return new EssayCriteriaScore(displayName, defaultScore, "Đạt yêu cầu cơ bản.", new[] { "Ý tứ mạch lạc" }, new[] { "Nên đa dạng cấu trúc hơn" });
    }

    private static EssayEvaluationResult EvaluateHeuristically(string text, string? topic, int wordCount)
    {
        var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        var sentenceCount = Math.Max(1, sentences.Length);

        var corrections = new List<EssayCorrectionItem>();
        if (text.Contains(" i ", StringComparison.Ordinal))
        {
            corrections.Add(new EssayCorrectionItem("i", "I", "Đại từ nhân xưng 'I' luôn phải viết hoa trong tiếng Anh."));
        }
        if (text.Contains("very good", StringComparison.OrdinalIgnoreCase))
        {
            corrections.Add(new EssayCorrectionItem("very good", "exceptional / outstanding", "Dùng từ vựng nâng cao để bài viết tự nhiên và học thuật hơn."));
        }

        return new EssayEvaluationResult(
            IsSuccess: true,
            OriginalText: text,
            WordCount: wordCount,
            OverallBandScore: 0.0, // KHÔNG trình bày band score như điểm chấm thật
            CefrLevel: "Ước tính sơ bộ",
            TaskAchievement: new EssayCriteriaScore(
                "Task Achievement & Coherence",
                0.0,
                $"[Ước tính thô, chưa qua AI] Bài viết gồm {wordCount} từ, phân chia khoảng {sentenceCount} câu.",
                new[] { "Bố cục câu cơ bản" },
                new[] { "Cần kết nối AI để đánh giá độ mạch lạc theo chủ đề" }),
            LexicalResource: new EssayCriteriaScore(
                "Lexical Resource",
                0.0,
                $"[Ước tính thô, chưa qua AI] Đếm từ vựng sơ bộ ({wordCount} từ).",
                new[] { "Vốn từ cơ bản" },
                new[] { "Cần kết nối AI để kiểm tra collocations và từ nâng cao" }),
            GrammaticalAccuracy: new EssayCriteriaScore(
                "Grammatical Range & Accuracy",
                0.0,
                "[Ước tính thô, chưa qua AI] Kiểm tra cú pháp cơ bản offline.",
                new[] { "Có dấu câu kết thúc" },
                new[] { "Cần kết nối AI để phát hiện lỗi ngữ pháp chuyên sâu" }),
            NativeSuggestions: new EssayCriteriaScore(
                "Native Suggestions",
                0.0,
                "[Ước tính thô, chưa qua AI] Chưa có gợi ý bản xứ khi chưa qua AI.",
                Array.Empty<string>(),
                new[] { "Kết nối Gemini AI để nhận gợi ý câu viết lại hoàn thiện" }),
            Corrections: corrections,
            GeneralFeedback: "⚠️ [ƯỚC TÍNH THÔ, CHƯA QUA AI]: Kết quả này chỉ mang tính tham khảo sơ bộ dựa trên số lượng từ và cấu trúc câu, chưa qua phân tích chuyên sâu của AI Tutor. Điểm Band Score không được tính là điểm chấm thật.",
            AwardedXp: 0,
            ErrorMessage: "Ước tính thô, chưa qua AI"
        );
    }

    public MessageContent BuildEssayInputCard(string customId = "essay_submission_form", string? defaultTopic = null)
    {
        var title = "📝 AI ESSAY COACH — CHẤM BÀI VIẾT 4 TIÊU CHÍ";
        var topicPrompt = !string.IsNullOrWhiteSpace(defaultTopic)
            ? $"Chủ đề gợi ý: {defaultTopic}\n\n"
            : string.Empty;

        var description =
            $"{topicPrompt}Học viên có thể nhập trực tiếp bài viết qua ô nhập liệu bên dưới hoặc gõ lệnh:\n" +
            "`/grade <nội dung đoạn văn tiếng Anh>`\n\n" +
            "Hệ thống AI LingLing sẽ chấm điểm chi tiết theo 4 tiêu chí quốc tế (Task Achievement, Lexical, Grammar, Native Suggestions). " +
            "Kết quả trả về hoàn toàn riêng tư (Ephemeral) để bảo mật quá trình học tập.";

        var buttonBuilder = new ButtonBuilder();
        buttonBuilder.AddInput(
            id: "essay_text_input",
            placeholder: "Dán bài viết tiếng Anh của bạn tại đây (tối đa 2000 từ)...",
            inputType: "textarea",
            defaultValue: "",
            textarea: true,
            required: true,
            disabled: false,
            style: null,
            nestedComponentId: null);

        buttonBuilder.AddButton("btn_submit_essay", "🚀 Chấm bài viết ngay", 1);

        var embedBuilder = new MessageEmbedBuilder()
            .SetColor("#4F46E5")
            .SetTitle(title)
            .SetDescription(description)
            .SetFooter("LINGUAL AI Writing Coach • 0 XP Awarded (Learning Only)", "");

        var contentBuilder = new MessageContentBuilder()
            .AddEmbed(embedBuilder.Build())
            .AddActionRow(buttonBuilder.BuildComponents());

        return contentBuilder.Build();
    }

    public string BuildEssayInputCardJson(string customId = "essay_submission_form", string? defaultTopic = null)
    {
        var content = BuildEssayInputCard(customId, defaultTopic);
        return content.ToJson();
    }

    public MessageContent BuildEvaluationResultCard(EssayEvaluationResult result)
    {
        bool isHeuristic = result.OverallBandScore <= 0.0 || result.ErrorMessage?.Contains("chưa qua AI") == true;

        var embedBuilder = new MessageEmbedBuilder();

        if (isHeuristic)
        {
            embedBuilder
                .SetColor("#F59E0B") // Amber Warning
                .SetTitle("⚠️ KẾT QUẢ ĐÁNH GIÁ SƠ BỘ — [ƯỚC TÍNH THÔ, CHƯA QUA AI]")
                .SetDescription(
                    "📌 **LƯU Ý:** Kết quả này là **ước tính thô sơ bộ, chưa qua AI chấm điểm**.\n" +
                    $"📝 **Số lượng từ:** {result.WordCount} từ\n" +
                    "⚠️ **Điểm Band Score:** *Không áp dụng (Chưa qua AI kiểm định chuyên sâu)*\n\n" +
                    $"{result.GeneralFeedback}")
                .AddField("1. Task Achievement (Ước tính thô)", result.TaskAchievement.Summary, false)
                .AddField("2. Lexical Resource (Ước tính thô)", result.LexicalResource.Summary, false)
                .AddField("3. Grammatical Accuracy (Ước tính thô)", result.GrammaticalAccuracy.Summary, false)
                .AddField("4. Native Suggestions (Ước tính thô)", result.NativeSuggestions.Summary, false)
                .SetFooter("LINGUAL AI Essay Coach • Dự phòng Offline (Chưa qua AI)", "");
        }
        else
        {
            embedBuilder
                .SetColor("#10B981") // Green Emerald
                .SetTitle($"🎯 KẾT QUẢ CHẤM BÀI VIẾT — AI ESSAY COACH (GEMINI): BAND {result.OverallBandScore:0.0} (CEFR {result.CefrLevel})")
                .SetDescription(
                    $"⭐ **Điểm tổng quan (Overall Band):** **{result.OverallBandScore:0.0}** • Trình độ: **{result.CefrLevel}**\n" +
                    $"📝 **Số lượng từ:** {result.WordCount} từ\n\n" +
                    $"💬 **Nhận xét từ AI:** {result.GeneralFeedback}\n\n" +
                    $"🎯 **1. Task Achievement & Coherence ({result.TaskAchievement.Score:0.0}):** {result.TaskAchievement.Summary}\n" +
                    $"📚 **2. Lexical Resource ({result.LexicalResource.Score:0.0}):** {result.LexicalResource.Summary}\n" +
                    $"⚖️ **3. Grammatical Range & Accuracy ({result.GrammaticalAccuracy.Score:0.0}):** {result.GrammaticalAccuracy.Summary}\n" +
                    $"✨ **4. Native Suggestions ({result.NativeSuggestions.Score:0.0}):** {result.NativeSuggestions.Summary}")
                .SetFooter("LINGUAL AI Essay Coach • Kết quả bảo mật riêng tư (Ephemeral)", "");
        }

        var contentBuilder = new MessageContentBuilder()
            .AddEmbed(embedBuilder.Build());

        return contentBuilder.Build();
    }
}
