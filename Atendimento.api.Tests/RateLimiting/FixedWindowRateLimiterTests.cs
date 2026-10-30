using System.Threading.RateLimiting;
using Xunit;

namespace Atendimento.API.Tests.RateLimiting;

public class FixedWindowRateLimiterTests
{
    [Fact]
    public void FixedWindowLimiter_WithPermitLimitOneAndNoAutoReplenishment_RejectsSecondAcquire()
    {
        using var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = 1,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = false
        });

        using var first = limiter.Acquire();
        using var second = limiter.Acquire();

        Assert.True(first.IsAcquired);
        Assert.False(second.IsAcquired);
    }
}
