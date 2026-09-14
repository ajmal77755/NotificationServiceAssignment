using Notifications.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Notifications.Application.Notifications
{
    public sealed record ReceiveNotificationCommand(string Payload, NotificationLevel Level);
    
    public sealed record ReceiveNotificationResult(Guid Id, ForwardingStatus Status);
}
