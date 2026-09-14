using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notifications.Application;
using Notifications.Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Notifications.Infrastructure.RateLimiting
{
    public sealed class SlidingLogRateLimiter(
        INotificationRepository notificationRepository, 
        TimeProvider timeProvider,
        IOptions<OutboundRateLimitOptions> options,
        ILogger<SlidingLogRateLimiter> logger) : IOutboundRateLimiter
    {
        private static readonly TimeSpan SafetyMargin = TimeSpan.FromMilliseconds(50);

        public async Task WaitForSlotAsync(CancellationToken cancellationToken)
        {
            var limit = options.Value.PermitLimit;
            var window = options.Value.Window;
            while (true)
            {
                var now = timeProvider.GetUtcNow();
                var sent = await notificationRepository.GetSentNotificationsTimeSineAsync(now - window, cancellationToken);
              
                if(sent.Count < limit)
                    return;
                
                var wait = sent[0] + window - now + SafetyMargin;

                if(wait < SafetyMargin)
                    wait = SafetyMargin;

                logger.LogDebug("Outbound rate limit reached ({Count}/{Limit}). Waiting for {WaitTime} before sending the next notification.", sent.Count, limit, wait);

                await Task.Delay(wait, timeProvider, cancellationToken);
            }
            
        }
    }
}
