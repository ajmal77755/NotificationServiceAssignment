using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notifications.Application.Forwarding;
using System;
using System.Collections.Generic;
using System.Text;

namespace Notifications.Infrastructure.Worker
{
    /// <summary>
    /// polls for pending notification and forward them but I would not follow this pending approach 
    /// </summary>
    /// <param name="serviceScopeFactory"></param>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    public sealed class ForwardingWorker(IServiceScopeFactory serviceScopeFactory, IOptions<ForwardingOptions> options, ILogger<ForwardingWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Forwarding worker started. Interval {Interval}", options.Value.PollInterval);
            while(!stoppingToken.IsCancellationRequested)
            {
                var didWork = false;
                try
                {
                    using var scope = serviceScopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<ForwardPendingNotificationService>();
                    didWork = await service.ForwardNextAsync(stoppingToken);
                }
                catch(OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                { 
                    break; 
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Forwarding Iteration failed.");
                }

                if(!didWork)
                {
                    try
                    { await Task.Delay(options.Value.PollInterval, stoppingToken); }
                    catch (OperationCanceledException){ break; }
                }
               
            }
            logger.LogInformation("Forwarding worker stopped.");
        }
    }
}
