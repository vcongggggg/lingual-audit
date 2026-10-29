using Microsoft.AspNetCore.SignalR;

namespace Lingual.Modules.Gamification;

public class GameHub : Hub
{
    public async Task JoinDuelQueue(string userId, string clanId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "DuelQueue");
        await Clients.Caller.SendAsync("QueueJoined", new { UserId = userId, Status = "Searching" });
    }

    public async Task LeaveDuelQueue(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "DuelQueue");
        await Clients.Caller.SendAsync("QueueLeft", new { UserId = userId });
    }

    public async Task SubmitAnswer(string roomId, string questionId, string selectedAnswer, int timeSpentMs)
    {
        await Clients.Group(roomId).SendAsync("OpponentAnswered", new
        {
            PlayerConnectionId = Context.ConnectionId,
            QuestionId = questionId,
            TimeSpentMs = timeSpentMs
        });
    }
}
