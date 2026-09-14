using Notifications.Application;
using Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Notifications.Infrastructure.Persistence
{
    public class NotificationRepository(NotificationsDbContext dbContext) : INotificationRepository
    {
        public async Task AddAsync(Domain.Notification notification, CancellationToken cancellationToken)
        {
            await dbContext.Notifications.AddAsync(notification, cancellationToken);
        }

        public Task<int> DeleteOlderThanAsync(DateTimeOffset cutoffDate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<Domain.Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await dbContext.Notifications.FindAsync(new object[] { id }, cancellationToken);
        }

        public async Task<Notification?> GetNextPendingNotificationAsync(CancellationToken cancellationToken) =>
            await dbContext.Notifications.Where(n => n.Status == ForwardingStatus.Pending)
                .OrderBy(n => n.ReceivedAt)
                .FirstOrDefaultAsync(cancellationToken);


        public async Task<IReadOnlyList<DateTimeOffset>> GetSentNotificationsTimeSineAsync(DateTimeOffset since, CancellationToken cancellationToken)
        {
            return await dbContext.Notifications
                .Where(n => n.Status == ForwardingStatus.Sent && n.SentAt >= since)
                .OrderBy(n => n.SentAt)
                .Select(n => n.SentAt)
                .ToListAsync(cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
    }
}
