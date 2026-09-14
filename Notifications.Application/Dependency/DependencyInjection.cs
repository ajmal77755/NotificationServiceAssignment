using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notifications.Application.Forwarding;
using Notifications.Application.Notifications;

namespace Notifications.Application.Dependency
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.TryAddSingleton(TimeProvider.System);
            services.AddScoped<ReceiveNotificationHandler>();
            services.AddScoped<ForwardPendingNotificationService>();
            return services;
        }
    }
}
