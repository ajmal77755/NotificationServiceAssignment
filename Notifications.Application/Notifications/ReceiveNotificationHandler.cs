using Notifications.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Notifications.Application.Notifications
{
    public sealed class ReceiveNotificationHandler
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly TimeProvider _timeProvider;
        public ReceiveNotificationHandler(INotificationRepository notificationRepository, TimeProvider timeProvider)
        {
            _notificationRepository = notificationRepository;
            _timeProvider = timeProvider;
        }

        public async Task<ReceiveNotificationResult> HandleAsync(ReceiveNotificationCommand command, CancellationToken cancellationToken)
        {
            var notification = Notification.Create(command.Payload, command.Level, _timeProvider.GetUtcNow());

            await _notificationRepository.AddAsync(notification, cancellationToken);
            await _notificationRepository.SaveChangesAsync(cancellationToken);

            return new ReceiveNotificationResult(notification.Id, notification.Status);
        }
    }
}
