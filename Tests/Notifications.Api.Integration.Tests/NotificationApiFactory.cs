using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System;
using System.Collections.Generic;
using System.Text;
using WireMock.Server;
using WireMock.ResponseBuilders;
using WireMock.RequestBuilders;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;



namespace Notifications.Api.Integration.Tests
{
    public sealed class NotificationApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private const string WebhookPath = "/webhook";
        private readonly string _databaseName = $"NotificationsDB_{Guid.NewGuid():N}";

        public WireMockServer Discord { get; private set; } = default!;
        public FakeChatClient Chat { get; } = new();

        public FakeTimeProvider Clock {  get; } = new(new DateTimeOffset(2026,9,14,0,0,0, TimeSpan.Zero));

        public int DiscordCalls => Discord.LogEntries.Count(e=>e.RequestMessage?.Path == WebhookPath);

        public bool DiscordReceived(string text) => Discord.LogEntries.Any(x=>x.RequestMessage?.Path ==WebhookPath && x.RequestMessage?.Body?.Contains(text) == true);

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:NotificationsDb"]=$"Server=DESKTOP-EIG8PF3;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True;",
                    ["Discord:WebhookUrl"] = Discord.Url + WebhookPath,
                    ["Llm:ApiKey"] = "not-used-intest",
                    ["Llm:Model"] = "not-used-intest",
                    ["Forwarding:PollInterval"] = "00:00:00.20",
                    ["Forwarding:MaxAttempts"] = "3"

                }));

            builder.ConfigureTestServices(s =>
            {
                //s.RemoveAll<TimeProvider>();
                //s.AddSingleton<TimeProvider>(Clock);
                s.RemoveAll<IChatClient>();
                s.AddSingleton<IChatClient>(Chat);
            });
        }

        public static async Task WaitUntilAsync(Func<bool> condition, string because, TimeSpan? timeout = null)
        {
            var sw = Stopwatch.StartNew();
            var limit = timeout ?? TimeSpan.FromSeconds(20);
            while(!condition())
            {
                if (sw.Elapsed > limit) throw new TimeoutException($"Not met within limit {limit}: {because}");
                await Task.Delay(50);
            }
        }

        async Task IAsyncLifetime.InitializeAsync()
        {
            Discord = WireMockServer.Start();
            Discord.Given(Request.Create().WithPath(WebhookPath).UsingPost())
                .RespondWith(Response.Create().WithStatusCode(204));

            using var scope = Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();
        }


        async Task IAsyncLifetime.DisposeAsync()
        {
            try
            {
                using var scope = Services.CreateScope();
                await scope.ServiceProvider.GetRequiredService<NotificationsDbContext>().Database.EnsureDeletedAsync();
            }
            finally
            {
                Discord.Stop();
                await base.DisposeAsync();
            }
            
        }

    }
}
