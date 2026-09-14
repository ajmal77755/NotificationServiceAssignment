using Notifications.Application.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Notifications.Application.Forwarding
{
    public sealed class ForwardPendingNotificationService(
        INotificationRepository notificationRepository,
        IOutboundRateLimiter outboundRateLimiter,
        IMessageGenerator messageGenerator,
        INotificationSync sink,
        TimeProvider timeProvider,
        IOptions<ForwardingOptions> options,
        ILogger<ForwardPendingNotificationService> logger
        )
    {

        public async Task<bool> ForwardNextAsync(CancellationToken cancellationToken)
        {
            var pendingNotification = await notificationRepository.GetNextPendingNotificationAsync(cancellationToken);
            if (pendingNotification is null)
            {
                logger.LogInformation("No pending notifications to forward.");
                return false;
            }
            logger.LogInformation("Forwarding notification {NotificationId}...", pendingNotification.Id);

            await outboundRateLimiter.WaitForSlotAsync(cancellationToken);
            try
            {
                var message = await messageGenerator.GenerateMessageAsync(pendingNotification, cancellationToken);
                await sink.SendAsync(pendingNotification, message, cancellationToken);
                pendingNotification.MarkAsSent(timeProvider.GetUtcNow());
                logger.LogInformation("Notification {NotificationId} forwarded successfully.", pendingNotification.Id);
            }
            catch (Exception ex)
            {
                pendingNotification.RecordFailure(ex.Message, options.Value.MaxAttempts, timeProvider.GetUtcNow());
                logger.LogWarning(ex, "An error occurred while forwarding notification {NotificationId}.", pendingNotification.Id);
            }
            
            await notificationRepository.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
