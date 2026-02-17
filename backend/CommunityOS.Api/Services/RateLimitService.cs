using System.Collections.Concurrent;

namespace CommunityOS.Api.Services;

public interface IRateLimitService
{
    bool TryConsume(Guid tenantId, Guid userId, string actionKey, TimeSpan window, out TimeSpan retryAfter);
}

/// <summary>
/// Simple per-user action rate limiter (in-memory).
/// Good enough for MVP; for multi-instance you'd replace with distributed store.
/// </summary>
public sealed class RateLimitService : IRateLimitService
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastActionAt = new();

    public bool TryConsume(Guid userId, string actionKey, TimeSpan window, out TimeSpan retryAfter)
    {
        var now = DateTimeOffset.UtcNow;
        var key = $"{userId:N}:{actionKey}";

        retryAfter = TimeSpan.Zero;

        while (true)
        {
            if (!_lastActionAt.TryGetValue(key, out var last))
            {
                if (_lastActionAt.TryAdd(key, now)) return true;
                continue;
            }

            var elapsed = now - last;
            if (elapsed >= window)
            {
                if (_lastActionAt.TryUpdate(key, now, last)) return true;
                continue;
            }

            retryAfter = window - elapsed;
            return false;
        }
    }
}
