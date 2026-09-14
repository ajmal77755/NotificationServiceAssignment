using Notifications.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Notifications.Application.Abstractions
{
    /// <summary>
    /// Contract for sending notifications to external systems or services. Should not swallow exceptions; any exceptions should be handled and logged within the implementation.
    /// </summary>
    public interface INotificationSync
    {
        Task SendAsync(Notification notification, GeneratedMessage generatedMessage, CancellationToken cancellationToken);
    }
}
