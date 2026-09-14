using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.AI;
using Notifications.Application.Abstractions;
using Notifications.Infrastructure.Messaging;
using Notifications.Infrastructure.Llm;
using OpenAI;
using Microsoft.Extensions.Logging;
using Notifications.Infrastructure.Worker;



namespace Notifications.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            //Persistence
            services.AddDbContext<NotificationsDbContext>(option =>
                option.UseSqlServer(configuration.GetConnectionString("NotificationsDb"),
                sql => sql.EnableRetryOnFailure()));
            services.AddScoped<INotificationRepository, NotificationRepository>();


            //Outbound rate limiter
            services.AddOptions<OutboundRateLimitOptions>()
                .Bind(configuration.GetSection(OutboundRateLimitOptions.SectionName))
                //.ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddScoped<IOutboundRateLimiter, SlidingLogRateLimiter>();

            //Discord
            services.AddOptions<DiscordOptions>().Bind(configuration.GetSection(DiscordOptions.SectionName)).ValidateOnStart();
            services.AddHttpClient<INotificationSync, DiscordWebhookSink>(client=>client.Timeout = TimeSpan.FromSeconds(15));

            //LlM
            services.AddOptions<LlmOptions>().Bind(configuration.GetSection(LlmOptions.SectionName))
                .ValidateOnStart();
            services.AddSingleton<IChatClient>(sp =>
            {
                var llm = sp.GetRequiredService<IOptions<LlmOptions>>().Value;
                return new OpenAIClient(llm.ApiKey).GetChatClient(llm.Model).AsIChatClient();
            });

            services.AddScoped<LlmMessageGenerator>();
            services.AddScoped<IMessageGenerator>(o =>
                new FallbackMessageGenerator(o.GetRequiredService<LlmMessageGenerator>(), 
                o.GetRequiredService<ILogger<FallbackMessageGenerator>>()));

            //Workers
            services.AddHostedService<ForwardingWorker>();

            return services;
        }
    }
}
