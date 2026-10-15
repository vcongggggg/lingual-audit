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

        var endpoint = $"{_options.BaseUrl.TrimEnd('/')}/models/{_options.Model}:generateContent?key={_options.ApiKey}";

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

        try
        {
            _logger.LogInformation("🤖 [GeminiAITutor] Đang phân tích câu '{Text}' bằng model {Model}...", request.Text, _options.Model);

            var response = await _httpClient.PostAsJsonAsync(endpoint, payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("❌ [GeminiAITutor] Gọi Gemini API thất bại (HTTP {StatusCode}): {Error}", response.StatusCode, errorBody);
                return CreateFallbackResponse(request.Text, "Máy chủ AI tạm thời bận hoặc gặp sự cố kết nối. Bạn hãy thử lại sau ít giây nhé!");
            }

            var result = await response.Content.ReadFromJsonAsync<GeminiApiResponse>(cancellationToken: cancellationToken);
            var rawJsonText = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrWhiteSpace(rawJsonText))
            {
                _logger.LogWarning("⚠️ [GeminiAITutor] Gemini trả về nội dung rỗng.");
                return CreateFallbackResponse(request.Text, "LingLing chưa thể phân tích câu này. Bạn hãy thử diễn đạt lại nhé!");
            }

            // Làm sạch chuỗi JSON nếu có bao quanh bởi markdown code blocks
            var cleanJson = CleanJsonString(rawJsonText);
            var parsed = JsonSerializer.Deserialize<GeminiAnalysisJsonModel>(cleanJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsed == null)
            {
                return CreateFallbackResponse(request.Text, "Không thể giải mã kết quả phản hồi từ mô hình AI.");
            }

            _logger.LogInformation("✅ [GeminiAITutor] Phân tích hoàn tất: IsCorrect={IsCorrect}, Corrected='{Corrected}'",
                parsed.IsCorrect, parsed.CorrectedText);

            return new GrammarAnalysisResponse(
                IsCorrect: parsed.IsCorrect,
                OriginalText: request.Text,
                CorrectedText: parsed.CorrectedText ?? request.Text,
                Explanation: parsed.Explanation ?? "Không có giải thích chi tiết.",
                Tips: parsed.Tips ?? "Hãy tiếp tục luyện tập đều đặn mỗi ngày!"
            );
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("🛑 [GeminiAITutor] Yêu cầu phân tích bị hủy.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [GeminiAITutor] Ngoại lệ khi phân tích ngữ pháp với Gemini API");
            return CreateFallbackResponse(request.Text, "Đã xảy ra lỗi trong quá trình xử lý câu hỏi của bạn.");
        }
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
