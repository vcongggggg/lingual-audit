using System;
using System.Security.Claims;
using Lingual.Infrastructure.Endpoint;
using Lingual.Modules.Gamification.Contracts;
using Lingual.Modules.Gamification.Contracts.DTOs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lingual.Modules.Gamification.Endpoints;

public class GameEndpoints : IEndpointBase
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/games")
            .WithTags("Games");

        // 1. GET /api/v1/games/data?type=&difficulty=
        group.MapGet("/data", async (
            [FromQuery] string? type,
            [FromQuery] string? difficulty,
            HttpContext httpContext,
            [FromServices] IGameService gameService,
            CancellationToken ct) =>
        {
            var userId = GetCurrentUserId(httpContext);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            try
            {
                var data = await gameService.GetGameDataAsync(type, difficulty, userId.Value, ct);
                return Results.Ok(data);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { success = false, message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { success = false, message = ex.Message });
            }
        })
        .WithName("GetGameData")
        .WithSummary("Lấy dữ liệu màn chơi cho Game Center")
        .WithDescription("Khởi tạo phiên chơi game, tải từ vựng/câu hỏi ngẫu nhiên và bảo mật đáp án server-side.")
        .Produces<GameDataResponseDto>(StatusCodes.Status200OK)
        .RequireAuthorization();

        // 2. POST /api/v1/games/submit
        group.MapPost("/submit", async (
            [FromBody] GameSubmitRequestDto request,
            HttpContext httpContext,
            [FromServices] IGameService gameService,
            CancellationToken ct) =>
        {
            var userId = GetCurrentUserId(httpContext);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var displayName = httpContext.User.FindFirstValue(ClaimTypes.Name);

            try
            {
                var result = await gameService.SubmitGameAsync(request, userId.Value, displayName, ct);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { success = false, message = ex.Message });
            }
        })
        .WithName("SubmitGameResult")
        .WithSummary("Nộp kết quả màn chơi và tính điểm server-side")
        .WithDescription("Chấm điểm chống gian lận (thời gian tối thiểu 2s, kiểm tra đáp án theo phiên) và cộng thưởng XP.")
        .Produces<GameSubmitResultDto>(StatusCodes.Status200OK)
        .RequireAuthorization();

        // 3. POST /api/v1/games/wordle/guess
        group.MapPost("/wordle/guess", async (
            [FromBody] WordleGuessRequestDto request,
            HttpContext httpContext,
            [FromServices] IGameService gameService,
            CancellationToken ct) =>
        {
            var userId = GetCurrentUserId(httpContext);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            try
            {
                var result = await gameService.EvaluateWordleGuessAsync(request, userId.Value, ct);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { success = false, message = ex.Message });
            }
        })
        .WithName("EvaluateWordleGuess")
        .WithSummary("Đánh giá lượt đoán Wordle từng chữ cái")
        .WithDescription("Đánh giá độ chính xác của từ 5 chữ cái (correct/present/absent) và bảo mật từ bí mật.")
        .Produces<WordleGuessResponseDto>(StatusCodes.Status200OK)
        .RequireAuthorization();

        // 4. GET /api/v1/games/leaderboard?type=&limit=20
        group.MapGet("/leaderboard", async (
            [FromQuery] string? type,
            [FromQuery] int limit = 20,
            [FromServices] IGameService gameService = null!,
            CancellationToken ct = default) =>
        {
            var result = await gameService.GetLeaderboardAsync(type, limit, ct);
            return Results.Ok(result);
        })
        .WithName("GetGameLeaderboard")
        .WithSummary("Lấy bảng xếp hạng điểm game")
        .WithDescription("Bảng xếp hạng người chơi thực theo từng loại trò chơi hoặc tổng hợp.")
        .Produces<GameLeaderboardResponseDto>(StatusCodes.Status200OK)
        .RequireAuthorization();

        // 5. GET /api/v1/games/stats/me
        group.MapGet("/stats/me", async (
            HttpContext httpContext,
            [FromServices] IGameService gameService,
            CancellationToken ct) =>
        {
            var userId = GetCurrentUserId(httpContext);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var result = await gameService.GetUserStatsAsync(userId.Value, ct);
            return Results.Ok(result);
        })
        .WithName("GetMyGameStats")
        .WithSummary("Lấy thống kê cá nhân qua các trò chơi")
        .WithDescription("Truy vấn kỷ lục điểm cao, tỉ lệ chính xác và tổng XP kiếm được của người dùng hiện tại.")
        .Produces<UserGameStatsResponseDto>(StatusCodes.Status200OK)
        .RequireAuthorization();
    }

    private static Guid? GetCurrentUserId(HttpContext? context)
    {
        if (context == null) return null;
        var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var guid) ? guid : null;
    }
}
