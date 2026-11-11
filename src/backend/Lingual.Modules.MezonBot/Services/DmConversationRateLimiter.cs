using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Lingual.Modules.MezonBot.Services;

public class DmConversationRateLimiter
{
    private readonly int _maxRequestsPerMinute;
    private readonly TimeSpan _window;
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _userRequests = new();

    public DmConversationRateLimiter(int maxRequestsPerMinute = 20, TimeSpan? window = null)
    {
        _maxRequestsPerMinute = maxRequestsPerMinute > 0 ? maxRequestsPerMinute : 20;
        _window = window ?? TimeSpan.FromMinutes(1);
    }

    public bool TryAcquire(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return false;

        var now = DateTime.UtcNow;
        var queue = _userRequests.GetOrAdd(userId, _ => new Queue<DateTime>());

        lock (queue)
        {
            while (queue.Count > 0 && (now - queue.Peek()) > _window)
            {
                queue.Dequeue();
            }

            if (queue.Count >= _maxRequestsPerMinute)
            {
                return false;
            }

            queue.Enqueue(now);
            return true;
        }
    }

    public void Reset(string userId)
    {
        if (_userRequests.TryGetValue(userId, out var queue))
        {
            lock (queue)
            {
                queue.Clear();
            }
        }
    }
}

