using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Lingual.Modules.AITutor.Contracts;
using Lingual.Modules.AITutor.Contracts.DTOs;
using Lingual.Modules.AITutor.Options;

namespace Lingual.Modules.AITutor.Services;

public class GeminiAITutorService : IAITutorService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiAITutorService> _logger;

    private const string SystemPrompt =
        "Bạn là LingLing, chú cú linh vật kiêm gia sư tiếng Anh thông minh và nhiệt tình của nền tảng LINGUAL trên Mezon.\n" +
        "Nhiệm vụ của bạn là phân tích ngữ pháp, từ vựng và mức độ tự nhiên của câu tiếng Anh do người học gửi đến.\n" +
        "Quy tắc phản hồi:\n" +
        "1. Nếu câu tiếng Anh của học viên đã chuẩn và tự nhiên: Đánh dấu is_correct: true, giữ nguyên câu hoặc gợi ý cách diễn đạt nâng cao hơn trong corrected_text, khen ngợi và giải thích ngắn gọn.\n" +
        "2. Nếu câu có lỗi (chia thì, giới từ, mạo từ, chính tả, cấu trúc): Đánh dấu is_correct: false, sửa lại câu chính xác trong corrected_text, giải thích rõ quy tắc ngữ pháp bằng tiếng Việt đơn giản, dễ nhớ trong explanation, và cung cấp mẹo ghi nhớ thực tế trong tips.\n" +
        "3. Nếu học viên chỉ gửi một từ vựng, cụm từ hoặc chủ đề ngữ pháp (ví dụ: 'past tense', 'passive voice', 'conditional sentences') thay vì câu hoàn chỉnh: Đánh dấu is_correct: true, cung cấp dạng thức chuẩn hoặc câu ví dụ tiêu biểu trong corrected_text, giải thích định nghĩa và cách dùng bằng tiếng Việt trong explanation, và chia sẻ mẹo ghi nhớ trong tips.\n" +
        "BẮT BUỘC chỉ trả về định dạng JSON hợp lệ với đúng 4 trường sau:\n" +
        "{\n" +
        "  \"is_correct\": true/false,\n" +
        "  \"corrected_text\": \"...\",\n" +
        "  \"explanation\": \"...\",\n" +
        "  \"tips\": \"...\"\n" +
        "}";

    public GeminiAITutorService(
        HttpClient httpClient,
        IOptions<GeminiOptions> options,
        ILogger<GeminiAITutorService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GrammarAnalysisResponse> AnalyzeGrammarAsync(
        GrammarAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning("⚠️ [GeminiAITutor] Chưa cấu hình Gemini:ApiKey. Trả về phản hồi dự phòng.");
            return new GrammarAnalysisResponse(
                IsCorrect: false,
                OriginalText: request.Text,
                CorrectedText: request.Text,
                Explanation: "Hệ thống AI LingLing chưa được cấu hình API Key trên máy chủ.",
                Tips: "Vui lòng kiểm tra lại thiết lập Gemini:ApiKey trong user-secrets."
            );
        }

        var userContent = $"Học viên gửi câu tiếng Anh: \"{request.Text.Trim()}\"";
        if (!string.IsNullOrWhiteSpace(request.Context))
        {
            userContent += $"\nNgữ cảnh: {request.Context.Trim()}";
        }

        var payload = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = SystemPrompt } }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = userContent } }
                }
            },
            generationConfig = new
            {
                temperature = 0.2,
                maxOutputTokens = 1000,
                responseMimeType = "application/json"
            }
        };

        // Danh sách các model để failover khi model chính gặp sự cố
        var candidateModels = new List<string>
        {
            _options.Model,
            "gemini-flash-lite-latest",
            "gemini-3.5-flash-lite",
            "gemini-3.1-flash-lite-preview"
        }.Where(m => !string.IsNullOrWhiteSpace(m)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        foreach (var model in candidateModels)
        {
            var endpoint = $"{_options.BaseUrl.TrimEnd('/')}/models/{model}:generateContent?key={_options.ApiKey}";
            const int maxAttemptsPerModel = 2; // Tối đa 2 lần thử cho mỗi model

            for (var attempt = 1; attempt <= maxAttemptsPerModel; attempt++)
            {
                try
                {
                    _logger.LogInformation("🤖 [GeminiAITutor] Đang phân tích câu '{Text}' bằng model {Model} (Lần thử {Attempt})...",
                        request.Text, model, attempt);

                    var response = await _httpClient.PostAsJsonAsync(endpoint, payload, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<GeminiApiResponse>(cancellationToken: cancellationToken);
                        var rawJsonText = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                        if (string.IsNullOrWhiteSpace(rawJsonText))
                        {
                            _logger.LogWarning("⚠️ [GeminiAITutor] Model {Model} trả về nội dung rỗng.", model);
                            break; // Chuyển sang model dự phòng tiếp theo
                        }

                        var cleanJson = CleanJsonString(rawJsonText);
                        var parsed = JsonSerializer.Deserialize<GeminiAnalysisJsonModel>(cleanJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (parsed != null)
                        {
                            _logger.LogInformation("✅ [GeminiAITutor] Phân tích hoàn tất bằng model {Model}: IsCorrect={IsCorrect}, Corrected='{Corrected}'",
                                model, parsed.IsCorrect, parsed.CorrectedText);

                            return new GrammarAnalysisResponse(
                                IsCorrect: parsed.IsCorrect,
                                OriginalText: request.Text,
                                CorrectedText: parsed.CorrectedText ?? request.Text,
                                Explanation: parsed.Explanation ?? "Không có giải thích chi tiết.",
                                Tips: parsed.Tips ?? "Hãy tiếp tục luyện tập đều đặn mỗi ngày!"
                            );
                        }
                    }

                    var statusCode = (int)response.StatusCode;
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("⚠️ [GeminiAITutor] Model {Model} thất bại (HTTP {StatusCode}): {Error}",
                        model, statusCode, errorBody);

                    // Nếu 503 (Server Busy) hoặc 429 (Rate Limit): Backoff rồi thử lại chính model này
                    if ((statusCode == 503 || statusCode == 429) && attempt < maxAttemptsPerModel)
                    {
                        var delayMs = 1200 * attempt;
                        _logger.LogInformation("⏳ [GeminiAITutor] Tự động thử lại model {Model} sau {DelayMs}ms...", model, delayMs);
                        await Task.Delay(delayMs, cancellationToken);
                        continue;
                    }

                    // Nếu mã lỗi khác hoặc đã hết lần thử -> thoát vòng lặp để chuyển model dự phòng tiếp theo
                    break;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("🛑 [GeminiAITutor] Yêu cầu phân tích bị hủy.");
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ [GeminiAITutor] Lỗi mạng khi gọi model {Model} (Lần thử {Attempt})", model, attempt);
                    if (attempt < maxAttemptsPerModel)
                    {
                        await Task.Delay(1000, cancellationToken);
                        continue;
                    }
                    break; // Chuyển sang model dự phòng tiếp theo
                }
            }
        }

        _logger.LogError("❌ [GeminiAITutor] Đã thử tất cả các model dự phòng và retry nhưng đều không thành công.");
        return CreateFallbackResponse(request.Text, "Máy chủ AI tạm thời bận hoặc gặp sự cố kết nối. Bạn hãy thử lại sau ít giây nhé!");
    }

    private static string CleanJsonString(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed["```json".Length..];
        }
        else if (trimmed.StartsWith("```"))
        {
            trimmed = trimmed["```".Length..];
        }

        if (trimmed.EndsWith("```"))
        {
            trimmed = trimmed[..^"```".Length];
        }

        return trimmed.Trim();
    }

    private static GrammarAnalysisResponse CreateFallbackResponse(string originalText, string message) =>
        new(
            IsCorrect: false,
            OriginalText: originalText,
            CorrectedText: originalText,
            Explanation: message,
            Tips: "Mẹo: Bạn có thể thử kiểm tra lại cấu trúc câu đơn giản hơn."
        );

    private class GeminiApiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }
    }

    private class Candidate
    {
        [JsonPropertyName("content")]
        public Content? Content { get; set; }
    }

    private class Content
    {
        [JsonPropertyName("parts")]
        public List<Part>? Parts { get; set; }
    }

    private class Part
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private class GeminiAnalysisJsonModel
    {
        [JsonPropertyName("is_correct")]
        public bool IsCorrect { get; set; }

        [JsonPropertyName("corrected_text")]
        public string? CorrectedText { get; set; }

        [JsonPropertyName("explanation")]
        public string? Explanation { get; set; }

        [JsonPropertyName("tips")]
        public string? Tips { get; set; }
    }
}
