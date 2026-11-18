using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lingual.Modules.Gamification.Contracts;
using Lingual.Modules.Gamification.Contracts.DTOs;
using Lingual.Modules.Gamification.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lingual.Modules.Gamification;

public class GameHub : Hub
{
    public const int ReconnectGracePeriodSeconds = 10;
    public const int RoundDurationSeconds = 10;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly ILogger<GameHub> _logger;

    private static readonly object QueueLock = new();
    private static readonly List<QueuedPlayer> WaitingQueue = new();
    private static readonly ConcurrentDictionary<Guid, ActiveDuelRoom> ActiveRooms = new();
    private static readonly ConcurrentDictionary<string, (Guid MatchId, Guid UserId)> ConnectionPlayerMap = new();

    public sealed record QueuedPlayer(
        string ConnectionId,
        Guid UserId,
        string UserName,
        string ClanId,
        DateTime EnqueuedAt
    );

    public sealed class ActiveDuelRoom
    {
        public Guid MatchId { get; init; }
        public Guid ChallengerId { get; init; }
        public string ChallengerName { get; set; } = "Challenger";
        public string? ChallengerConnectionId { get; set; }
        public Guid OpponentId { get; init; }
        public string OpponentName { get; set; } = "Opponent";
        public string? OpponentConnectionId { get; set; }
        public bool IsBotMatch { get; init; }
        public int CurrentRound { get; set; } = 0;
        public int TotalRounds { get; init; } = 5;
        public DuelRoundQuestionDto? CurrentQuestion { get; set; }
        public DateTime RoundStartedAt { get; set; }
        public bool IsStarted { get; set; }
        public bool IsCompleted { get; set; }
        public decimal ChallengerScore { get; set; }
        public decimal OpponentScore { get; set; }
        public CancellationTokenSource? RoundTimerCts { get; set; }
        public CancellationTokenSource? ChallengerDisconnectCts { get; set; }
        public CancellationTokenSource? OpponentDisconnectCts { get; set; }
        public readonly object SyncRoot = new();
    }

    public GameHub(
        IServiceScopeFactory scopeFactory,
        IHubContext<GameHub> hubContext,
        ILogger<GameHub> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _logger = logger;
    }

    public static string GetRoomGroupName(Guid matchId) => $"duel_{matchId:N}";

    public async Task JoinDuelQueue(string userId, string clanId)
    {
        await JoinDuelQueueWithProfile(userId, $"Learner_{userId[..Math.Min(6, userId.Length)]}", clanId);
    }

    public async Task JoinDuelQueueWithProfile(string userId, string userName, string clanId)
    {
        var parsedUserId = ParseOrGenerateUserId(userId);
        var cleanName = string.IsNullOrWhiteSpace(userName) ? $"Player_{parsedUserId.ToString()[..6]}" : userName.Trim();

        await Groups.AddToGroupAsync(Context.ConnectionId, "DuelQueue");

        QueuedPlayer? matchedOpponent = null;
        lock (QueueLock)
        {
            WaitingQueue.RemoveAll(q => q.UserId == parsedUserId || q.ConnectionId == Context.ConnectionId);
            matchedOpponent = WaitingQueue.FirstOrDefault(q => q.UserId != parsedUserId);
            if (matchedOpponent != null)
            {
                WaitingQueue.Remove(matchedOpponent);
            }
            else
            {
                WaitingQueue.Add(new QueuedPlayer(Context.ConnectionId, parsedUserId, cleanName, clanId ?? "default", DateTime.UtcNow));
            }
        }

        if (matchedOpponent == null)
        {
            await Clients.Caller.SendAsync("QueueJoined", new
            {
                UserId = parsedUserId.ToString(),
                UserName = cleanName,
                Status = "Searching"
            });
            return;
        }

        // Đã tìm thấy đối thủ -> Tạo trận đấu 1vs1 ngay lập tức
        using var scope = _scopeFactory.CreateScope();
        var duelService = scope.ServiceProvider.GetRequiredService<IWordDuelService>();

        var match = await duelService.CreateChallengeAsync(new CreateDuelChallengeRequest(
            ChallengerId: matchedOpponent.UserId,
            ChallengerName: matchedOpponent.UserName,
            OpponentId: parsedUserId,
            OpponentName: cleanName,
            QuestionCount: 5,
            AutoAccept: true
        ));

        var roomGroup = GetRoomGroupName(match.Id);
        await Groups.RemoveFromGroupAsync(matchedOpponent.ConnectionId, "DuelQueue");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "DuelQueue");
        await Groups.AddToGroupAsync(matchedOpponent.ConnectionId, roomGroup);
        await Groups.AddToGroupAsync(Context.ConnectionId, roomGroup);

        var room = new ActiveDuelRoom
        {
            MatchId = match.Id,
            ChallengerId = match.ChallengerId,
            ChallengerName = match.ChallengerName,
            ChallengerConnectionId = matchedOpponent.ConnectionId,
            OpponentId = match.OpponentId,
            OpponentName = match.OpponentName,
            OpponentConnectionId = Context.ConnectionId,
            IsBotMatch = false,
            TotalRounds = match.QuestionCount
        };

        ActiveRooms[match.Id] = room;
        ConnectionPlayerMap[matchedOpponent.ConnectionId] = (match.Id, match.ChallengerId);
        ConnectionPlayerMap[Context.ConnectionId] = (match.Id, match.OpponentId);

        await _hubContext.Clients.Group(roomGroup).SendAsync("MatchFound", match);
        _ = RunMatchLifecycleAsync(match.Id);
    }

    public async Task LeaveDuelQueue(string userId)
    {
        var parsedUserId = ParseOrGenerateUserId(userId);
        lock (QueueLock)
        {
            WaitingQueue.RemoveAll(q => q.UserId == parsedUserId || q.ConnectionId == Context.ConnectionId);
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "DuelQueue");
        await Clients.Caller.SendAsync("QueueLeft", new { UserId = userId });
    }

    public async Task StartBotDuel(string userId, string userName)
    {
        var parsedUserId = ParseOrGenerateUserId(userId);
        var cleanName = string.IsNullOrWhiteSpace(userName) ? $"Player_{parsedUserId.ToString()[..6]}" : userName.Trim();

        lock (QueueLock)
        {
            WaitingQueue.RemoveAll(q => q.UserId == parsedUserId || q.ConnectionId == Context.ConnectionId);
        }

        using var scope = _scopeFactory.CreateScope();
        var duelService = scope.ServiceProvider.GetRequiredService<IWordDuelService>();

        var match = await duelService.CreateChallengeAsync(new CreateDuelChallengeRequest(
            ChallengerId: parsedUserId,
            ChallengerName: cleanName,
            OpponentId: WordDuelService.LingLingBotUserId,
            OpponentName: "LingLing AI",
            QuestionCount: 5,
            AutoAccept: true
        ));

        var roomGroup = GetRoomGroupName(match.Id);
        await Groups.AddToGroupAsync(Context.ConnectionId, roomGroup);

        var room = new ActiveDuelRoom
        {
            MatchId = match.Id,
            ChallengerId = match.ChallengerId,
            ChallengerName = match.ChallengerName,
            ChallengerConnectionId = Context.ConnectionId,
            OpponentId = WordDuelService.LingLingBotUserId,
            OpponentName = "LingLing AI",
            OpponentConnectionId = "BOT_CONNECTION",
            IsBotMatch = true,
            TotalRounds = match.QuestionCount
        };

        ActiveRooms[match.Id] = room;
        ConnectionPlayerMap[Context.ConnectionId] = (match.Id, match.ChallengerId);

        await Clients.Caller.SendAsync("MatchFound", match);
        _ = RunMatchLifecycleAsync(match.Id);
    }

    public async Task JoinMatchRoom(string matchId, string userId, string userName)
    {
        if (!Guid.TryParse(matchId, out var parsedMatchId))
        {
            await Clients.Caller.SendAsync("DuelError", new { Message = "Mã trận đấu không hợp lệ." });
            return;
        }

        var parsedUserId = ParseOrGenerateUserId(userId);
        var cleanName = string.IsNullOrWhiteSpace(userName) ? $"Player_{parsedUserId.ToString()[..6]}" : userName.Trim();

        using var scope = _scopeFactory.CreateScope();
        var duelService = scope.ServiceProvider.GetRequiredService<IWordDuelService>();
        var match = await duelService.GetMatchAsync(parsedMatchId);

        if (match == null)
        {
            await Clients.Caller.SendAsync("DuelError", new { Message = "Không tìm thấy phòng thách đấu này." });
            return;
        }

        var roomGroup = GetRoomGroupName(parsedMatchId);
        await Groups.AddToGroupAsync(Context.ConnectionId, roomGroup);

        var room = ActiveRooms.GetOrAdd(parsedMatchId, _ => new ActiveDuelRoom
        {
            MatchId = match.Id,
            ChallengerId = match.ChallengerId,
            ChallengerName = match.ChallengerName,
            OpponentId = match.OpponentId,
            OpponentName = match.OpponentName,
            IsBotMatch = match.IsBotMatch,
            TotalRounds = match.QuestionCount,
            ChallengerScore = match.ChallengerScore,
            OpponentScore = match.OpponentScore
        });

        var isReconnecting = false;
        var shouldStartMatch = false;

        lock (room.SyncRoot)
        {
            if (parsedUserId == room.ChallengerId)
            {
                if (room.ChallengerDisconnectCts != null)
                {
                    room.ChallengerDisconnectCts.Cancel();
                    room.ChallengerDisconnectCts.Dispose();
                    room.ChallengerDisconnectCts = null;
                    isReconnecting = true;
                }
                room.ChallengerConnectionId = Context.ConnectionId;
                room.ChallengerName = cleanName;
            }
            else if (parsedUserId == room.OpponentId || room.OpponentId == WordDuelService.OpenChallengePlaceholderUserId)
            {
                if (room.OpponentDisconnectCts != null)
                {
                    room.OpponentDisconnectCts.Cancel();
                    room.OpponentDisconnectCts.Dispose();
                    room.OpponentDisconnectCts = null;
                    isReconnecting = true;
                }
                room.OpponentConnectionId = Context.ConnectionId;
                room.OpponentName = cleanName;
            }

            ConnectionPlayerMap[Context.ConnectionId] = (parsedMatchId, parsedUserId);

            var bothConnected = !string.IsNullOrEmpty(room.ChallengerConnectionId) &&
                                (!string.IsNullOrEmpty(room.OpponentConnectionId) || room.IsBotMatch);

            if (bothConnected && !room.IsStarted && !room.IsCompleted)
            {
                room.IsStarted = true;
                shouldStartMatch = true;
            }
        }

        if (isReconnecting && room.IsStarted && !room.IsCompleted)
        {
            _logger.LogInformation("🔄 [GameHub] Người chơi {UserId} đã kết nối lại thành công vào trận {MatchId} trong thời gian ân hạn (<10s)!",
                parsedUserId, parsedMatchId);

            await Clients.OthersInGroup(roomGroup).SendAsync("OpponentReconnected", new
            {
                MatchId = parsedMatchId,
                UserId = parsedUserId,
                UserName = cleanName
            });

            var elapsedMs = (int)Math.Max(0, (DateTime.UtcNow - room.RoundStartedAt).TotalMilliseconds);
            var remainingSeconds = Math.Max(1, RoundDurationSeconds - (elapsedMs / 1000));

            await Clients.Caller.SendAsync("MatchStateSynced", new
            {
                Match = match,
                CurrentRound = room.CurrentRound,
                Question = room.CurrentQuestion,
                RemainingSeconds = remainingSeconds,
                ChallengerScore = room.ChallengerScore,
                OpponentScore = room.OpponentScore
            });
            return;
        }

        await Clients.Caller.SendAsync("RoomJoined", match);

        if (shouldStartMatch)
        {
            await _hubContext.Clients.Group(roomGroup).SendAsync("MatchFound", match);
            _ = RunMatchLifecycleAsync(parsedMatchId);
        }
    }

    public async Task SubmitAnswer(string roomId, string questionId, string selectedAnswer, int timeSpentMs)
    {
        if (ConnectionPlayerMap.TryGetValue(Context.ConnectionId, out var mapping))
        {
            await SubmitRoundAnswer(mapping.MatchId.ToString(), mapping.UserId.ToString(), questionId, selectedAnswer, timeSpentMs);
            return;
        }

        await Clients.Group(roomId).SendAsync("OpponentAnswered", new
        {
            PlayerConnectionId = Context.ConnectionId,
            QuestionId = questionId,
            TimeSpentMs = timeSpentMs
        });
    }

    public async Task SubmitRoundAnswer(
        string matchId,
        string userId,
        string questionId,
        string selectedKey,
        int timeSpentMs)
    {
        if (!Guid.TryParse(matchId, out var parsedMatchId) || !Guid.TryParse(questionId, out var parsedQuestionId))
        {
            return;
        }

        var parsedUserId = ParseOrGenerateUserId(userId);
        var roomGroup = GetRoomGroupName(parsedMatchId);

        using var scope = _scopeFactory.CreateScope();
        var duelService = scope.ServiceProvider.GetRequiredService<IWordDuelService>();

        var result = await duelService.SubmitAnswerAsync(new SubmitDuelAnswerRequest(
            MatchId: parsedMatchId,
            UserId: parsedUserId,
            QuestionId: parsedQuestionId,
            SelectedKey: selectedKey,
            ResponseMs: timeSpentMs
        ));

        await Clients.Caller.SendAsync("AnswerSubmitted", result);

        if (result.Accepted)
        {
            await Clients.OthersInGroup(roomGroup).SendAsync("OpponentAnswered", new
            {
                MatchId = parsedMatchId,
                UserId = parsedUserId,
                QuestionId = parsedQuestionId,
                RoundNumber = result.RoundNumber,
                TimeSpentMs = timeSpentMs
            });

            if (ActiveRooms.TryGetValue(parsedMatchId, out var room))
            {
                if (result.MatchSnapshot != null)
                {
                    room.ChallengerScore = result.MatchSnapshot.ChallengerScore;
                    room.OpponentScore = result.MatchSnapshot.OpponentScore;
                }

                if (result.BothAnsweredCurrentRound)
                {
                    room.RoundTimerCts?.Cancel();
                }
            }
        }
    }

    public async Task ForfeitMatch(string matchId, string userId)
    {
        if (!Guid.TryParse(matchId, out var parsedMatchId)) return;
        var parsedUserId = ParseOrGenerateUserId(userId);

        await ExecuteMatchForfeitAsync(parsedMatchId, parsedUserId, "player_forfeit");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        lock (QueueLock)
        {
            WaitingQueue.RemoveAll(q => q.ConnectionId == Context.ConnectionId);
        }

        if (ConnectionPlayerMap.TryRemove(Context.ConnectionId, out var mapping))
        {
            var (matchId, userId) = mapping;
            if (ActiveRooms.TryGetValue(matchId, out var room) && !room.IsCompleted)
            {
                var roomGroup = GetRoomGroupName(matchId);
                var cts = new CancellationTokenSource();

                lock (room.SyncRoot)
                {
                    if (userId == room.ChallengerId)
                    {
                        room.ChallengerDisconnectCts?.Cancel();
                        room.ChallengerDisconnectCts = cts;
                    }
                    else if (userId == room.OpponentId)
                    {
                        room.OpponentDisconnectCts?.Cancel();
                        room.OpponentDisconnectCts = cts;
                    }
                }

                _logger.LogWarning("⚠️ [GameHub] Người chơi {UserId} bị ngắt kết nối khỏi trận {MatchId}. Bắt đầu đếm ngược ân hạn {GraceSeconds}s...",
                    userId, matchId, ReconnectGracePeriodSeconds);

                await _hubContext.Clients.Group(roomGroup).SendAsync("OpponentDisconnected", new
                {
                    MatchId = matchId,
                    DisconnectedUserId = userId,
                    GracePeriodSeconds = ReconnectGracePeriodSeconds
                });

                // SP3-06: Đợi 10 giây cho phép kết nối lại; nếu quá 10 giây thì xử thua tự động (forfeit)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(ReconnectGracePeriodSeconds), cts.Token);
                        if (!cts.Token.IsCancellationRequested)
                        {
                            await ExecuteMatchForfeitAsync(matchId, userId, "disconnect_timeout");
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        // Đã kết nối lại thành công trong vòng <10s
                    }
                });
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task ExecuteMatchForfeitAsync(Guid matchId, Guid forfeitingUserId, string reason)
    {
        if (ActiveRooms.TryGetValue(matchId, out var room))
        {
            lock (room.SyncRoot)
            {
                if (room.IsCompleted) return;
                room.IsCompleted = true;
                room.RoundTimerCts?.Cancel();
            }
        }

        using var scope = _scopeFactory.CreateScope();
        var duelService = scope.ServiceProvider.GetRequiredService<IWordDuelService>();
        var completedMatch = await duelService.ForfeitMatchAsync(matchId, forfeitingUserId, reason);

        if (completedMatch != null)
        {
            var roomGroup = GetRoomGroupName(matchId);
            await _hubContext.Clients.Group(roomGroup).SendAsync("MatchCompleted", new
            {
                Reason = reason,
                ForfeitedByUserId = forfeitingUserId,
                Match = completedMatch
            });
        }

        CleanupRoom(matchId);
    }

    private async Task RunMatchLifecycleAsync(Guid matchId)
    {
        if (!ActiveRooms.TryGetValue(matchId, out var room)) return;
        var roomGroup = GetRoomGroupName(matchId);

        try
        {
            using (var startScope = _scopeFactory.CreateScope())
            {
                var duelService = startScope.ServiceProvider.GetRequiredService<IWordDuelService>();
                await duelService.StartMatchInProgressAsync(matchId);
            }

            await _hubContext.Clients.Group(roomGroup).SendAsync("MatchStarting", new
            {
                MatchId = matchId,
                CountdownSeconds = 3
            });

            await Task.Delay(TimeSpan.FromSeconds(3));

            for (var round = 1; round <= room.TotalRounds; round++)
            {
                if (room.IsCompleted) return;

                DuelRoundQuestionDto? question;
                using (var qScope = _scopeFactory.CreateScope())
                {
                    var duelService = qScope.ServiceProvider.GetRequiredService<IWordDuelService>();
                    question = await duelService.GetRoundQuestionAsync(matchId, round);
                }

                if (question == null) break;

                using var roundCts = new CancellationTokenSource();
                lock (room.SyncRoot)
                {
                    room.CurrentRound = round;
                    room.CurrentQuestion = question;
                    room.RoundStartedAt = DateTime.UtcNow;
                    room.RoundTimerCts = roundCts;
                }

                await _hubContext.Clients.Group(roomGroup).SendAsync("RoundStarted", question);

                // Nếu là đấu với Bot LingLing AI, mô phỏng Bot suy nghĩ và trả lời trong 2.5s - 5.5s
                if (room.IsBotMatch)
                {
                    _ = SimulateBotAnswerAsync(matchId, question, roundCts.Token);
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(RoundDurationSeconds), roundCts.Token);
                }
                catch (TaskCanceledException)
                {
                    // Cả 2 người chơi đã trả lời xong trước khi hết 10 giây
                }

                if (room.IsCompleted) return;

                // Tự động ghi nhận TIMEOUT (0 điểm) cho người chơi chưa kịp trả lời trong vòng này
                using (var timeoutScope = _scopeFactory.CreateScope())
                {
                    var duelService = timeoutScope.ServiceProvider.GetRequiredService<IWordDuelService>();
                    await duelService.SubmitAnswerAsync(new SubmitDuelAnswerRequest(
                        MatchId: matchId,
                        UserId: room.ChallengerId,
                        QuestionId: question.Id,
                        SelectedKey: "TIMEOUT",
                        ResponseMs: RoundDurationSeconds * 1000
                    ));

                    var opponentEndResult = await duelService.SubmitAnswerAsync(new SubmitDuelAnswerRequest(
                        MatchId: matchId,
                        UserId: room.OpponentId,
                        QuestionId: question.Id,
                        SelectedKey: "TIMEOUT",
                        ResponseMs: RoundDurationSeconds * 1000
                    ));

                    await _hubContext.Clients.Group(roomGroup).SendAsync("RoundEnded", new
                    {
                        MatchId = matchId,
                        RoundNumber = round,
                        QuestionId = question.Id,
                        CorrectKey = opponentEndResult.CorrectKey,
                        CorrectText = opponentEndResult.CorrectText,
                        Explanation = opponentEndResult.Explanation,
                        ChallengerScore = opponentEndResult.MatchSnapshot?.ChallengerScore ?? room.ChallengerScore,
                        OpponentScore = opponentEndResult.MatchSnapshot?.OpponentScore ?? room.OpponentScore
                    });
                }

                if (round < room.TotalRounds && !room.IsCompleted)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(2200));
                }
            }

            if (!room.IsCompleted)
            {
                room.IsCompleted = true;
                using var completeScope = _scopeFactory.CreateScope();
                var duelService = completeScope.ServiceProvider.GetRequiredService<IWordDuelService>();
                var finalMatch = await duelService.CompleteMatchAsync(matchId);

                if (finalMatch != null)
                {
                    await _hubContext.Clients.Group(roomGroup).SendAsync("MatchCompleted", new
                    {
                        Reason = "normal_completion",
                        Match = finalMatch
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [GameHub] Lỗi trong vòng đời trận đấu Word Duel {MatchId}", matchId);
        }
        finally
        {
            CleanupRoom(matchId);
        }
    }

    private async Task SimulateBotAnswerAsync(
        Guid matchId,
        DuelRoundQuestionDto question,
        CancellationToken roundToken)
    {
        try
        {
            var delayMs = Random.Shared.Next(2400, 5600);
            await Task.Delay(delayMs, roundToken);
            if (roundToken.IsCancellationRequested) return;

            // LingLing AI có xác suất trả lời đúng ~75% để tạo trải nghiệm thi đấu kịch tính
            using var scope = _scopeFactory.CreateScope();
            var duelService = scope.ServiceProvider.GetRequiredService<IWordDuelService>();

            var pickCorrect = Random.Shared.NextDouble() <= 0.75;
            var chosenOption = question.Options[Random.Shared.Next(question.Options.Count)].Key;

            // Gửi câu trả lời của Bot
            var botResult = await duelService.SubmitAnswerAsync(new SubmitDuelAnswerRequest(
                MatchId: matchId,
                UserId: WordDuelService.LingLingBotUserId,
                QuestionId: question.Id,
                SelectedKey: pickCorrect ? GetSmartBotChoiceKey(question) : chosenOption,
                ResponseMs: delayMs
            ));

            var roomGroup = GetRoomGroupName(matchId);
            await _hubContext.Clients.Group(roomGroup).SendAsync("OpponentAnswered", new
            {
                MatchId = matchId,
                UserId = WordDuelService.LingLingBotUserId,
                QuestionId = question.Id,
                RoundNumber = question.RoundNumber,
                TimeSpentMs = delayMs
            });

            if (botResult.BothAnsweredCurrentRound && ActiveRooms.TryGetValue(matchId, out var room))
            {
                room.RoundTimerCts?.Cancel();
            }
        }
        catch (TaskCanceledException)
        {
            // Vòng đấu đã kết thúc sớm
        }
    }

    private static string GetSmartBotChoiceKey(DuelRoundQuestionDto question)
    {
        var prompt = question.Prompt.ToLowerInvariant();
        if (prompt.Contains("boarding pass")) return "A";
        if (prompt.Contains("speak a language easily")) return "B";
        if (prompt.Contains("in london for three years")) return "C";
        if (prompt.Contains("synonym of 'essential'")) return "A";
        if (prompt.Contains("responsible ___")) return "B";
        if (prompt.Contains("meeting ___")) return "A";
        if (prompt.Contains("if i ___ more time")) return "B";
        return question.Options.FirstOrDefault()?.Key ?? "A";
    }

    private static void CleanupRoom(Guid matchId)
    {
        if (ActiveRooms.TryRemove(matchId, out var room))
        {
            room.RoundTimerCts?.Dispose();
            room.ChallengerDisconnectCts?.Dispose();
            room.OpponentDisconnectCts?.Dispose();
        }
    }

    private static Guid ParseOrGenerateUserId(string? rawUserId)
    {
        if (Guid.TryParse(rawUserId, out var parsed)) return parsed;
        if (string.IsNullOrWhiteSpace(rawUserId)) return Guid.NewGuid();

        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes($"duel_user:{rawUserId.Trim()}"));
        return new Guid(hash);
    }
}
