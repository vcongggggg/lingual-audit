using System;
using System.Collections.Concurrent;

namespace Lingual.Modules.MezonBot.Services;

public class ReactionBookmarkRateLimiter
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _userTimestamps = new();
    private readonly int _maxRequestsPerMinute;

    public ReactionBookmarkRateLimiter(int maxRequestsPerMinute = 10)
    {
        _maxRequestsPerMinute = maxRequestsPerMinute;
    }

    public bool TryAcquire(string userId)
    {
        var now = DateTime.UtcNow;
        var queue = _userTimestamps.GetOrAdd(userId, _ => new ConcurrentQueue<DateTime>());

        // Loại bỏ các timestamp cũ hơn 60 giây
        while (queue.TryPeek(out var timestamp) && (now - timestamp).TotalSeconds > 60)
        {
            queue.TryDequeue(out _);
        }

        if (queue.Count >= _maxRequestsPerMinute)
        {
            return false;
        }

        queue.Enqueue(now);
        return true;
    }

    public void Reset(string userId)
    {
        _userTimestamps.TryRemove(userId, out _);
    }
}

