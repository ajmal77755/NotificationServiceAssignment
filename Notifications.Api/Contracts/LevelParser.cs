using Notifications.Domain;
namespace Notifications.Api.Contracts
{
    public static class LevelParser
    {
        private static readonly Dictionary<string, NotificationLevel> _levelMap = new()
        {
            { "info", NotificationLevel.Info },
            { "warning", NotificationLevel.Warning },
            { "error", NotificationLevel.Error },
            { "err", NotificationLevel.Error  },
            { "critical", NotificationLevel.Critical },
            { "crit", NotificationLevel.Critical },
            { "warn", NotificationLevel.Warning },

        };

        public static bool Parse(string? level, out NotificationLevel notificationLevel)
        {
            notificationLevel = default;
            return !string.IsNullOrWhiteSpace(level) && _levelMap.TryGetValue(level.Trim().ToLowerInvariant(), out notificationLevel);
        }
    }
}
