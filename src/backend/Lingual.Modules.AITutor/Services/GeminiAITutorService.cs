using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Lingual.Modules.AITutor.Contracts;
using Lingual.Modules.AITutor.Contracts.DTOs;
using Lingual.Modules.AITutor.Domain;
using Lingual.Modules.AITutor.Infrastructure.Data;
using Lingual.Modules.AITutor.Options;

namespace Lingual.Modules.AITutor.Services;

public class GeminiAITutorService : IAITutorService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GeminiAITutorService> _logger;
    private static readonly object _inMemoryLock = new();

    private const string GrammarSystemPrompt =
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

    public static readonly List<ScenarioDto> PredefinedScenarios = new()
    {
        new ScenarioDto(
            Guid.Parse("11111111-1111-1111-1111-111111111101"),
            "cafe_order",
            "Gọi đồ uống tại Quán Cà phê (London)",
            "Luyện giao tiếp gọi cà phê/trà, chọn size, loại sữa và thanh toán với nhân viên pha chế (Barista) tại London.",
            "A1",
            "Barista LingLing",
            "Hi there! Welcome to The London Roast. What drink can I get started for you today?"
        ),
        new ScenarioDto(
            Guid.Parse("22222222-2222-2222-2222-222222222202"),
            "airport_checkin",
            "Làm thủ tục tại Sân bay Quốc tế",
            "Luyện hội thoại làm thủ tục check-in, cân hành lý và hỏi cửa khởi hành tại sân bay Heathrow.",
            "A2",
            "Ground Crew LingLing",
            "Good morning! Welcome to London Heathrow Airport. May I see your passport and flight booking, please?"
        ),
        new ScenarioDto(
            Guid.Parse("33333333-3333-3333-3333-333333333303"),
            "job_interview",
            "Phỏng vấn xin việc & Giới thiệu bản thân",
            "Luyện trả lời câu hỏi phỏng vấn cơ bản về kinh nghiệm, điểm mạnh và định hướng nghề nghiệp.",
            "B1",
            "Interviewer LingLing",
            "Hello! Welcome to our interview today. To start off, could you tell me a little bit about yourself and your background?"
        )
    };

    public GeminiAITutorService(
        HttpClient httpClient,
        IOptions<GeminiOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<GeminiAITutorService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _scopeFactory = scopeFactory;
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
                Tips: "Vui lòng kiểm tra lại thiết lập Gemini:ApiKey trong user-secrets hoặc appsettings.Local.json."
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
                parts = new[] { new { text = GrammarSystemPrompt } }
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

        var candidateModels = GetCandidateModels();

        foreach (var model in candidateModels)
        {
            var endpoint = $"{_options.BaseUrl.TrimEnd('/')}/models/{model}:generateContent";
            const int maxAttemptsPerModel = 2;

            for (var attempt = 1; attempt <= maxAttemptsPerModel; attempt++)
            {
                try
                {
                    _logger.LogInformation("🤖 [GeminiAITutor] Đang phân tích câu (độ dài {Length} ký tự) bằng model {Model} (Lần thử {Attempt})...",
                        request.Text?.Length ?? 0, model, attempt);

                    using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
                    httpRequest.Headers.Add("x-goog-api-key", _options.ApiKey);
                    httpRequest.Content = JsonContent.Create(payload);

                    var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<GeminiApiResponse>(cancellationToken: cancellationToken);
                        var rawJsonText = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                        if (string.IsNullOrWhiteSpace(rawJsonText))
                        {
                            _logger.LogWarning("⚠️ [GeminiAITutor] Model {Model} trả về nội dung rỗng.", model);
                            break;
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
                    else
                    {
                        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogWarning("⚠️ [GeminiAITutor] Model {Model} trả về HTTP {StatusCode}: {ErrorBody}",
                            model, response.StatusCode, errorBody);

                        if ((int)response.StatusCode is 404 or 400)
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex) when (attempt < maxAttemptsPerModel)
                {
                    _logger.LogWarning(ex, "⚠️ [GeminiAITutor] Lỗi kết nối khi gọi model {Model} (Lần thử {Attempt}). Đang thử lại...", model, attempt);
                    await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ [GeminiAITutor] Thất bại khi gọi model {Model} ở lần thử {Attempt}.", model, attempt);
                    break;
                }
            }
        }

        return CreateFallbackResponse(request.Text, "Mascot AI LingLing đang bận ôn tập từ vựng, tạm thời chưa phân tích được câu này.");
    }

    public async Task<IReadOnlyList<ScenarioDto>> GetAvailableScenariosAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<AITutorDbContext>();
            if (dbContext != null)
            {
                var dbScenarios = await dbContext.AiScenarios
                    .Where(s => s.Status == "published")
                    .OrderBy(s => s.Code)
                    .ToListAsync(cancellationToken);

                if (dbScenarios.Any())
                {
                    return dbScenarios.Select(s =>
                    {
                        var predefined = PredefinedScenarios.FirstOrDefault(p => p.Code == s.Code);
                        return new ScenarioDto(
                            s.Id,
                            s.Code,
                            s.Title,
                            s.Description ?? string.Empty,
                            s.Level ?? "A1",
                            predefined?.CharacterRole ?? "Mascot LingLing",
                            predefined?.GreetingMessage ?? "Hi there! Let's practice English together!"
                        );
                    }).ToList();
                }

                // Nếu database chưa có, nạp tự động (seed) các kịch bản chuẩn
                foreach (var p in PredefinedScenarios)
                {
                    dbContext.AiScenarios.Add(new AiScenario
                    {
                        Id = p.Id,
                        Code = p.Code,
                        Title = p.Title,
                        Description = p.Description,
                        Level = p.Level,
                        SystemPromptKey = $"roleplay_{p.Code}",
                        Status = "published"
                    });
                }
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ [GeminiAITutor] Không thể truy vấn database cho scenarios. Dùng danh sách mặc định.");
        }

        return PredefinedScenarios;
    }

    public async Task<RoleplayTurnResponse> ProcessRoleplayTurnAsync(
        RoleplayTurnRequest request,
        CancellationToken cancellationToken = default)
    {
        var scenario = PredefinedScenarios.FirstOrDefault(s => s.Code.Equals(request.ScenarioCode, StringComparison.OrdinalIgnoreCase))
            ?? PredefinedScenarios[0];

        var conversationId = request.ConversationId ?? Guid.NewGuid();

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            var fallbackReply = $"[Offline Mode] {scenario.CharacterRole}: That sounds great! Could you tell me more about that?";
            await SaveConversationTurnAsync(conversationId, scenario.Id, request.MezonUserId, request.UserMessage, fallbackReply, "offline", totalTokens: 0, cancellationToken: cancellationToken);

            return new RoleplayTurnResponse(
                ConversationId: conversationId,
                ScenarioCode: scenario.Code,
                ScenarioTitle: scenario.Title,
                CharacterRole: scenario.CharacterRole,
                Reply: fallbackReply,
                HasFeedback: false,
                FeedbackCorrection: null,
                Explanation: null
            );
        }

        var systemPrompt =
            $"You are {scenario.CharacterRole}, a friendly and supportive character in an English conversation roleplay scenario.\n" +
            $"Scenario context: {scenario.Title} - {scenario.Description}\n" +
            $"Learner CEFR level: {scenario.Level}\n\n" +
            "INSTRUCTIONS FOR YOUR RESPONSE:\n" +
            "1. Reply naturally in character, speaking natural, conversational English suitable for the learner's level (2-3 sentences max). Always ask a friendly question to keep the conversation flowing.\n" +
            "2. Analyze the learner's message for grammar mistakes, unnatural phrasing, wrong prepositions, or incorrect vocabulary.\n" +
            "3. If the learner made a mistake, set has_feedback: true, and generate feedback_correction strictly in Vietnamese formatted as:\n" +
            "   \"💡 Bạn nên nói: \\\"{corrected_sentence}\\\" thay vì \\\"{wrong_sentence}\\\" nhé!\"\n" +
            "   Also provide a short explanation in Vietnamese in explanation.\n" +
            "4. If the learner's sentence is already accurate and natural, set has_feedback: false, feedback_correction: null, explanation: null.\n\n" +
            "OUTPUT FORMAT (STRICT JSON ONLY):\n" +
            "{\n" +
            "  \"reply\": \"<character reply in English>\",\n" +
            "  \"has_feedback\": true/false,\n" +
            "  \"feedback_correction\": \"💡 Bạn nên nói: \\\"...\\\" thay vì \\\"...\\\" nhé!\",\n" +
            "  \"explanation\": \"<short grammar explanation in Vietnamese>\"\n" +
            "}";

        var userContent = $"Học viên gửi câu thoại: \"{request.UserMessage.Trim()}\"";

        // Đọc lịch sử các lượt trước của Conversation này (tối đa 6 messages gần nhất = 3 turns)
        var historyTurns = await LoadConversationHistoryAsync(conversationId, request.MezonUserId, maxTurns: 6, cancellationToken);

        // Xây dựng contents array cho Gemini: gồm lịch sử (user/model) và lượt user hiện tại
        var contentsList = new List<object>();

        foreach (var turn in historyTurns)
        {
            var role = turn.Role.Equals("user", StringComparison.OrdinalIgnoreCase) ? "user" : "model";
            contentsList.Add(new
            {
                role = role,
                parts = new[] { new { text = turn.Text } }
            });
        }

        // Lượt user hiện tại
        contentsList.Add(new
        {
            role = "user",
            parts = new[] { new { text = userContent } }
        });

        var payload = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = systemPrompt } }
            },
            contents = contentsList,
            generationConfig = new
            {
                temperature = 0.4,
                maxOutputTokens = 1000,
                responseMimeType = "application/json"
            }
        };

        var candidateModels = GetCandidateModels();

        foreach (var model in candidateModels)
        {
            var endpoint = $"{_options.BaseUrl.TrimEnd('/')}/models/{model}:generateContent";

            try
            {
                _logger.LogInformation("🎭 [GeminiAITutor] Xử lý lượt Roleplay ({Scenario}, Conv: {ConvId}) bằng model {Model} (Lịch sử: {HistoryCount} lượt)...",
                    scenario.Code, conversationId, model, historyTurns.Count);

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
                httpRequest.Headers.Add("x-goog-api-key", _options.ApiKey);
                httpRequest.Content = JsonContent.Create(payload);

                var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<GeminiApiResponse>(cancellationToken: cancellationToken);
                    var rawJsonText = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                    if (!string.IsNullOrWhiteSpace(rawJsonText))
                    {
                        var cleanJson = CleanJsonString(rawJsonText);
                        var parsed = JsonSerializer.Deserialize<GeminiRoleplayJsonModel>(cleanJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (parsed != null && !string.IsNullOrWhiteSpace(parsed.Reply))
                        {
                            var totalTokens = result?.UsageMetadata?.TotalTokenCount ?? (request.UserMessage.Length / 4 + parsed.Reply.Length / 4 + 10);

                            // Lưu hội thoại vào database: APPEND lượt mới vào Messages
                            await SaveConversationTurnAsync(conversationId, scenario.Id, request.MezonUserId, request.UserMessage, parsed.Reply, model, totalTokens, cancellationToken);

                            return new RoleplayTurnResponse(
                                ConversationId: conversationId,
                                ScenarioCode: scenario.Code,
                                ScenarioTitle: scenario.Title,
                                CharacterRole: scenario.CharacterRole,
                                Reply: parsed.Reply,
                                HasFeedback: parsed.HasFeedback,
                                FeedbackCorrection: parsed.FeedbackCorrection,
                                Explanation: parsed.Explanation
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ [GeminiAITutor] Thử thách model {Model} cho roleplay thất bại.", model);
            }
        }

        // Fallback response
        var fallback = $"Thank you! I understand: \"{request.UserMessage}\". Let's continue our conversation!";
        await SaveConversationTurnAsync(conversationId, scenario.Id, request.MezonUserId, request.UserMessage, fallback, "fallback", totalTokens: 0, cancellationToken: cancellationToken);

        return new RoleplayTurnResponse(
            ConversationId: conversationId,
            ScenarioCode: scenario.Code,
            ScenarioTitle: scenario.Title,
            CharacterRole: scenario.CharacterRole,
            Reply: fallback,
            HasFeedback: false,
            FeedbackCorrection: null,
            Explanation: null
        );
    }

    private async Task<List<ConversationTurnMessage>> LoadConversationHistoryAsync(
        Guid conversationId,
        string? mezonUserId,
        int maxTurns,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<AITutorDbContext>();
            if (dbContext == null) return new();

            var conv = await dbContext.AiConversations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

            if (conv == null || string.IsNullOrWhiteSpace(conv.Messages) || conv.Messages == "[]")
            {
                return new();
            }

            // Kiểm tra quyền sở hữu cuộc trò chuyện nếu mezonUserId được cung cấp
            if (!string.IsNullOrWhiteSpace(mezonUserId))
            {
                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes($"mezon_user_{mezonUserId}"));
                var callerUserId = new Guid(hash.AsSpan(0, 16));
                var defaultGuestId = Guid.Parse("00000000-0000-0000-0000-000000000001");

                if (conv.UserId != callerUserId && conv.UserId != defaultGuestId && conv.UserId != Guid.Empty)
                {
                    _logger.LogWarning("⚠️ [GeminiAITutor] Người dùng {UserId} không có quyền truy cập hội thoại {ConvId}.", mezonUserId, conversationId);
                    return new();
                }
            }

            var list = JsonSerializer.Deserialize<List<ConversationTurnMessage>>(conv.Messages, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new();

            if (list.Count > maxTurns)
            {
                return list.Skip(list.Count - maxTurns).ToList();
            }

            return list;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ [GeminiAITutor] Không thể tải lịch sử hội thoại {ConversationId}", conversationId);
            return new();
        }
    }

    private async Task SaveConversationTurnAsync(
        Guid conversationId,
        Guid scenarioId,
        string? mezonUserId,
        string userMessage,
        string assistantReply,
        string modelName,
        int totalTokens = 0,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<AITutorDbContext>();
            if (dbContext == null)
            {
                return;
            }

            var userId = Guid.Empty;
            if (!string.IsNullOrWhiteSpace(mezonUserId))
            {
                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes($"mezon_user_{mezonUserId}"));
                userId = new Guid(hash.AsSpan(0, 16));
            }
            if (userId == Guid.Empty)
            {
                userId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            }

            var newTurns = new[]
            {
                new ConversationTurnMessage { Role = "user", Text = userMessage, Time = DateTime.UtcNow },
                new ConversationTurnMessage { Role = "model", Text = assistantReply, Time = DateTime.UtcNow }
            };
            var newTurnsJson = JsonSerializer.Serialize(newTurns);

            if (dbContext.Database.IsRelational())
            {
                // Concurrency an toàn trên PostgreSQL: Sử dụng toán tử JSONB concatenation nguyên tử (||)
                var affected = await dbContext.Database.ExecuteSqlRawAsync(
                    "UPDATE ai_conversations SET messages = messages || {0}::jsonb, model_name = {1}, total_tokens = total_tokens + {2}, ended_at = now() WHERE id = {3}",
                    newTurnsJson, modelName, totalTokens, conversationId, cancellationToken);

                if (affected == 0)
                {
                    var conv = new AiConversation
                    {
                        Id = conversationId,
                        UserId = userId,
                        ScenarioId = scenarioId,
                        ConversationType = "roleplay",
                        Title = $"Roleplay {conversationId.ToString()[..8]}",
                        Status = "active",
                        Messages = newTurnsJson,
                        ModelName = modelName,
                        TotalTokens = totalTokens,
                        StartedAt = DateTime.UtcNow,
                        EndedAt = DateTime.UtcNow
                    };
                    dbContext.AiConversations.Add(conv);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }
            else
            {
                // InMemory database (cho unit tests): Lock đồng bộ để bảo vệ concurrency
                lock (_inMemoryLock)
                {
                    var conv = dbContext.AiConversations.FirstOrDefault(c => c.Id == conversationId);
                    if (conv == null)
                    {
                        conv = new AiConversation
                        {
                            Id = conversationId,
                            UserId = userId,
                            ScenarioId = scenarioId,
                            ConversationType = "roleplay",
                            Title = $"Roleplay {conversationId.ToString()[..8]}",
                            Status = "active",
                            Messages = newTurnsJson,
                            ModelName = modelName,
                            TotalTokens = totalTokens,
                            StartedAt = DateTime.UtcNow,
                            EndedAt = DateTime.UtcNow
                        };
                        dbContext.AiConversations.Add(conv);
                    }
                    else
                    {
                        var list = JsonSerializer.Deserialize<List<ConversationTurnMessage>>(conv.Messages) ?? new();
                        list.AddRange(newTurns);
                        conv.Messages = JsonSerializer.Serialize(list);
                        conv.ModelName = modelName;
                        conv.TotalTokens += totalTokens;
                        conv.EndedAt = DateTime.UtcNow;
                    }
                    dbContext.SaveChanges();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ [GeminiAITutor] Không thể lưu lịch sử hội thoại roleplay vào database.");
        }
    }

    private List<string> GetCandidateModels() =>
        new List<string>
        {
            _options.Model,
            "gemini-flash-lite-latest",
            "gemini-3.5-flash-lite",
            "gemini-3.1-flash-lite-preview"
        }.Where(m => !string.IsNullOrWhiteSpace(m)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private static string CleanJsonString(string rawJson)
    {
        var trimmed = rawJson.Trim();

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

    public class ConversationTurnMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("time")]
        public DateTime Time { get; set; } = DateTime.UtcNow;
    }

    private class GeminiApiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }

        [JsonPropertyName("usageMetadata")]
        public UsageMetadata? UsageMetadata { get; set; }
    }

    private class UsageMetadata
    {
        [JsonPropertyName("promptTokenCount")]
        public int? PromptTokenCount { get; set; }

        [JsonPropertyName("candidatesTokenCount")]
        public int? CandidatesTokenCount { get; set; }

        [JsonPropertyName("totalTokenCount")]
        public int? TotalTokenCount { get; set; }
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

    private class GeminiRoleplayJsonModel
    {
        [JsonPropertyName("reply")]
        public string? Reply { get; set; }

        [JsonPropertyName("has_feedback")]
        public bool HasFeedback { get; set; }

        [JsonPropertyName("feedback_correction")]
        public string? FeedbackCorrection { get; set; }

        [JsonPropertyName("explanation")]
        public string? Explanation { get; set; }
    }
}
