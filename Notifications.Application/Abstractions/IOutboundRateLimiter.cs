using System;
using System.Collections.Generic;
using System.Text;

namespace Notifications.Application.Abstractions
{
    /// <summary>
    /// Contract for limiting the rate of outbound notifications.
    /// </summary>
    public interface IOutboundRateLimiter
    {
        /// <summary>
        /// Waits for an available slot to send an outbound notification, respecting the configured rate limits.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task WaitForSlotAsync(CancellationToken cancellationToken);
    }
}
