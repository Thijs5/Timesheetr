namespace Timesheetr.Api.Infrastructure.Models;

public class RateLimitException(TimeSpan retryAfter) : Exception($"Rate limited by Toggl. Retry after {(int)retryAfter.TotalSeconds}s.")
{
    public TimeSpan RetryAfter { get; } = retryAfter;
}
