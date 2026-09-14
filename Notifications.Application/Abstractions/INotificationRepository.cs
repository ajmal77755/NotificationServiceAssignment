using Notifications.Domain;

namespace Notifications.Application
{
    public interface INotificationRepository
    {
        Task AddAsync(Notification notification, CancellationToken cancellationToken);

        Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        Task<int> DeleteOlderThanAsync(DateTimeOffset cutoffDate, CancellationToken cancellationToken);

        /// <summary>
        /// Oldest notification that is pending to be sent. If there is no pending notification, returns null.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Notification?> GetNextPendingNotificationAsync(CancellationToken cancellationToken);

        /// <summary>
        /// SentAt timestamps at or after <paramref name="since"/>, ascending order. Feeds the rate limiter to determine if a new notification can be sent.
        /// </summary>
        /// <param name="since"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IReadOnlyList<DateTimeOffset>> GetSentNotificationsTimeSineAsync(DateTimeOffset since, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
