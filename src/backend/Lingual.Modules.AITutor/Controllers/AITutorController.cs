using System.Security.Claims;
using Lingual.Modules.AITutor.Contracts;
using Lingual.Modules.AITutor.Contracts.DTOs;
using Lingual.Modules.AITutor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Lingual.Modules.AITutor.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/ai")]
public class AITutorController : ControllerBase
{
    private const string GuestSentinelUserId = "00000000-0000-0000-0000-000000000001";

    private readonly IAITutorService _aiTutorService;
    private readonly IAiQuotaService _aiQuotaService;
    private readonly ILogger<AITutorController> _logger;

    public AITutorController(
        IAITutorService aiTutorService,
        IAiQuotaService aiQuotaService,
        ILogger<AITutorController> logger)
    {
        _aiTutorService = aiTutorService;
        _aiQuotaService = aiQuotaService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/v1/ai/quota — Trả về số lượt AI còn lại của người dùng đã đăng nhập (5 lượt miễn phí/ngày GMT+7 + lượt thưởng).
    /// </summary>
    [HttpGet("quota")]
    public async Task<ActionResult<AiQuotaStatusDto>> GetQuota(CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "Yêu cầu đăng nhập hợp lệ để xem lượt AI." });
        }

        var status = await _aiQuotaService.GetQuotaAsync(userId.Value, cancellationToken);
        return Ok(status);
    }

    /// <summary>
    /// POST /api/v1/ai/grammar-check — Yêu cầu JWT; userId lấy duy nhất từ claims; trừ 1 lượt nguyên tử trước khi gọi Gemini và hoàn lượt nếu Gemini lỗi.
    /// </summary>
    [HttpPost("grammar-check")]
    public async Task<ActionResult<GrammarAnalysisResponse>> CheckGrammar(
        [FromBody] GrammarAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "Yêu cầu đăng nhập hợp lệ để sử dụng trợ lý ngữ pháp LingLing AI." });
        }

        if (request == null || string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new { message = "Vui lòng cung cấp câu tiếng Anh cần kiểm tra." });
        }

        var idempotencyKey = Request.Headers["X-Idempotency-Key"].FirstOrDefault();
        var consume = await _aiQuotaService.TryConsumeTurnAsync(
            userId.Value,
            reason: "consume_grammar",
            idempotencyKey: idempotencyKey,
            cancellationToken: cancellationToken);

        if (!consume.Allowed)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                code = consume.ErrorCode ?? AiQuotaService.QuotaExceededCode,
                message = consume.Message,
                isGuest = false,
                quota = consume.Quota
            });
        }

        try
        {
            var result = await _aiTutorService.AnalyzeGrammarAsync(request, cancellationToken);
            if (result.ProviderFailed)
            {
                var refundedQuota = await _aiQuotaService.RefundTurnAsync(
                    userId.Value,
                    consume.ConsumedFromDailyFree,
                    consume.IdempotencyKey,
                    "refund_gemini_error",
                    cancellationToken);

                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    code = "AI_PROVIDER_UNAVAILABLE",
                    message = result.Explanation,
                    quota = refundedQuota,
                    refunded = true
                });
            }

            return Ok(result with { Quota = consume.Quota });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [AITutorController] Lỗi gọi Gemini grammar-check cho user {UserId}, tiến hành hoàn lượt.", userId.Value);
            var refundedQuota = await _aiQuotaService.RefundTurnAsync(
                userId.Value,
                consume.ConsumedFromDailyFree,
                consume.IdempotencyKey,
                "refund_gemini_exception",
                cancellationToken);

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                code = "AI_PROVIDER_UNAVAILABLE",
                message = "Kết nối tới LingLing AI tạm thời gián đoạn. Lượt của bạn đã được hoàn lại.",
                quota = refundedQuota,
                refunded = true
            });
        }
    }

    /// <summary>
    /// GET /api/v1/ai/scenarios — Danh sách kịch bản đóng vai cho người dùng đã đăng nhập.
    /// </summary>
    [HttpGet("scenarios")]
    public async Task<ActionResult<IReadOnlyList<ScenarioDto>>> GetScenarios(
        CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "Yêu cầu đăng nhập hợp lệ." });
        }

        var result = await _aiTutorService.GetAvailableScenariosAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/v1/ai/roleplay — Yêu cầu JWT; userId lấy duy nhất từ claims (bỏ qua mọi mezonUserId client gửi).
    /// </summary>
    [HttpPost("roleplay")]
    public async Task<ActionResult<RoleplayTurnResponse>> ProcessRoleplay(
        [FromBody] RoleplayTurnRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "Yêu cầu đăng nhập hợp lệ để đóng vai cùng LingLing AI." });
        }

        if (request == null || string.IsNullOrWhiteSpace(request.UserMessage))
        {
            return BadRequest(new { message = "Vui lòng nhập câu thoại tiếng Anh để tiếp tục đối thoại." });
        }

        var idempotencyKey = Request.Headers["X-Idempotency-Key"].FirstOrDefault();
        var consume = await _aiQuotaService.TryConsumeTurnAsync(
            userId.Value,
            reason: "consume_roleplay",
            idempotencyKey: idempotencyKey,
            cancellationToken: cancellationToken);

        if (!consume.Allowed)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                code = consume.ErrorCode ?? AiQuotaService.QuotaExceededCode,
                message = consume.Message,
                isGuest = false,
                quota = consume.Quota
            });
        }

        // BẢO MẬT: Ghi đè MezonUserId bằng đúng userId từ JWT claims, không bao giờ tin id client gửi.
        var sanitizedRequest = request with { MezonUserId = userId.Value.ToString() };

        try
        {
            var result = await _aiTutorService.ProcessRoleplayTurnAsync(sanitizedRequest, cancellationToken);
            if (result.ProviderFailed)
            {
                var refundedQuota = await _aiQuotaService.RefundTurnAsync(
                    userId.Value,
                    consume.ConsumedFromDailyFree,
                    consume.IdempotencyKey,
                    "refund_gemini_error",
                    cancellationToken);

                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    code = "AI_PROVIDER_UNAVAILABLE",
                    message = "LingLing AI tạm thời chưa phản hồi được. Lượt của bạn đã được hoàn lại.",
                    quota = refundedQuota,
                    refunded = true
                });
            }

            return Ok(result with { Quota = consume.Quota });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [AITutorController] Lỗi gọi Gemini roleplay cho user {UserId}, tiến hành hoàn lượt.", userId.Value);
            var refundedQuota = await _aiQuotaService.RefundTurnAsync(
                userId.Value,
                consume.ConsumedFromDailyFree,
                consume.IdempotencyKey,
                "refund_gemini_exception",
                cancellationToken);

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                code = "AI_PROVIDER_UNAVAILABLE",
                message = "Kết nối tới LingLing AI tạm thời gián đoạn. Lượt của bạn đã được hoàn lại.",
                quota = refundedQuota,
                refunded = true
            });
        }
    }

    // =========================================================================
    // GUEST ENDPOINTS (CHƯA ĐĂNG NHẬP TRÊN /tutor: TỐI ĐA 2 LƯỢT/IP/NGÀY GMT+7)
    // =========================================================================

    [AllowAnonymous]
    [HttpGet("guest/scenarios")]
    public async Task<ActionResult<IReadOnlyList<ScenarioDto>>> GetGuestScenarios(
        CancellationToken cancellationToken)
    {
        var result = await _aiTutorService.GetAvailableScenariosAsync(cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("guest/quota")]
    public async Task<ActionResult<AiQuotaStatusDto>> GetGuestQuota(CancellationToken cancellationToken)
    {
        var clientIp = ResolveClientIp();
        var status = await _aiQuotaService.GetGuestQuotaAsync(clientIp, cancellationToken);
        return Ok(status);
    }

    [AllowAnonymous]
    [HttpPost("guest/grammar-check")]
    public async Task<ActionResult<GrammarAnalysisResponse>> CheckGrammarAsGuest(
        [FromBody] GrammarAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new { message = "Vui lòng cung cấp câu tiếng Anh cần kiểm tra." });
        }

        var clientIp = ResolveClientIp();
        var consume = await _aiQuotaService.TryConsumeGuestTurnAsync(clientIp, "consume_guest_grammar", cancellationToken);
        if (!consume.Allowed)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                code = consume.ErrorCode ?? AiQuotaService.QuotaExceededCode,
                message = consume.Message,
                isGuest = true,
                quota = consume.Quota
            });
        }

        try
        {
            var result = await _aiTutorService.AnalyzeGrammarAsync(request, cancellationToken);
            if (result.ProviderFailed)
            {
                var refundedQuota = await _aiQuotaService.RefundGuestTurnAsync(clientIp, cancellationToken);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    code = "AI_PROVIDER_UNAVAILABLE",
                    message = result.Explanation,
                    isGuest = true,
                    quota = refundedQuota,
                    refunded = true
                });
            }

            return Ok(result with { Quota = consume.Quota });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [AITutorController] Lỗi gọi Gemini guest grammar-check (IP {Ip}), hoàn lượt.", clientIp);
            var refundedQuota = await _aiQuotaService.RefundGuestTurnAsync(clientIp, cancellationToken);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                code = "AI_PROVIDER_UNAVAILABLE",
                message = "Kết nối tới LingLing AI tạm thời gián đoạn. Lượt trải nghiệm của bạn đã được hoàn lại.",
                isGuest = true,
                quota = refundedQuota,
                refunded = true
            });
        }
    }

    [AllowAnonymous]
    [HttpPost("guest/roleplay")]
    public async Task<ActionResult<RoleplayTurnResponse>> ProcessRoleplayAsGuest(
        [FromBody] RoleplayTurnRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.UserMessage))
        {
            return BadRequest(new { message = "Vui lòng nhập câu thoại tiếng Anh để tiếp tục đối thoại." });
        }

        var clientIp = ResolveClientIp();
        var consume = await _aiQuotaService.TryConsumeGuestTurnAsync(clientIp, "consume_guest_roleplay", cancellationToken);
        if (!consume.Allowed)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                code = consume.ErrorCode ?? AiQuotaService.QuotaExceededCode,
                message = consume.Message,
                isGuest = true,
                quota = consume.Quota
            });
        }

        // BẢO MẬT: Không bao giờ chấp nhận userId/mezonUserId từ guest client
        var sanitizedRequest = request with { MezonUserId = GuestSentinelUserId };

        try
        {
            var result = await _aiTutorService.ProcessRoleplayTurnAsync(sanitizedRequest, cancellationToken);
            if (result.ProviderFailed)
            {
                var refundedQuota = await _aiQuotaService.RefundGuestTurnAsync(clientIp, cancellationToken);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    code = "AI_PROVIDER_UNAVAILABLE",
                    message = "LingLing AI tạm thời chưa phản hồi được. Lượt trải nghiệm của bạn đã được hoàn lại.",
                    isGuest = true,
                    quota = refundedQuota,
                    refunded = true
                });
            }

            return Ok(result with { Quota = consume.Quota });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [AITutorController] Lỗi gọi Gemini guest roleplay (IP {Ip}), hoàn lượt.", clientIp);
            var refundedQuota = await _aiQuotaService.RefundGuestTurnAsync(clientIp, cancellationToken);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                code = "AI_PROVIDER_UNAVAILABLE",
                message = "Kết nối tới LingLing AI tạm thời gián đoạn. Lượt trải nghiệm của bạn đã được hoàn lại.",
                isGuest = true,
                quota = refundedQuota,
                refunded = true
            });
        }
    }

    private Guid? GetAuthenticatedUserId()
    {
        if (User?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var guid) ? guid : null;
    }

    private string ResolveClientIp()
    {
        var remoteIp = HttpContext?.Connection?.RemoteIpAddress?.ToString();
        var forwarded = Request?.Headers["X-Forwarded-For"].FirstOrDefault();

        // Only trust X-Forwarded-For when arriving from loopback or private proxy hops (Next.js proxy / Render reverse proxy)
        if (!string.IsNullOrWhiteSpace(forwarded) && IsTrustedProxyHop(remoteIp))
        {
            var firstHop = forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(firstHop))
            {
                return firstHop;
            }
        }

        return string.IsNullOrWhiteSpace(remoteIp) ? "127.0.0.1" : remoteIp;
    }

    private static bool IsTrustedProxyHop(string? remoteIp)
    {
        if (string.IsNullOrWhiteSpace(remoteIp)) return true;
        var ip = remoteIp.StartsWith("::ffff:", StringComparison.OrdinalIgnoreCase)
            ? remoteIp["::ffff:".Length..]
            : remoteIp;

        return ip == "127.0.0.1"
            || ip == "::1"
            || ip.StartsWith("10.", StringComparison.Ordinal)
            || ip.StartsWith("172.", StringComparison.Ordinal)
            || ip.StartsWith("192.168.", StringComparison.Ordinal)
            || ip.StartsWith("100.64.", StringComparison.Ordinal);
    }
}
