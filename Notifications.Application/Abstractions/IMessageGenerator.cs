using Notifications.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Notifications.Application.Abstractions
{
    /// <summary>
    /// Contract for generating messages from notifications.
    /// </summary>
    public interface IMessageGenerator
    {
        Task<GeneratedMessage> GenerateMessageAsync(Notification notification, CancellationToken cancellationToken);
    }
}
