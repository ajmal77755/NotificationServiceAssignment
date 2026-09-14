using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Api.Contracts;
using Notifications.Application.Forwarding;
using Notifications.Application.Notifications;
using Notifications.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace Notifications.Api.Integration.Tests
{
    public sealed class NotificationForwardingTests(NotificationApiFactory notificationApiFactory) : IClassFixture<NotificationApiFactory>
    {
        private static StringContent Json(string body) => new StringContent(body, Encoding.UTF8, "application/json");

        [Theory]
        [InlineData("""{"message":{"no level provided"}""")]
        [InlineData("""{"level":"urgent"}""")]
        [InlineData("""{"level":3}""")]
        [InlineData("""{"level":"unknown"}""")]
        public async Task Post_InvalidBodyReturns400(string body)
        {
            var respnse = await notificationApiFactory.CreateClient().PostAsync("/notifications", Json(body));
            Assert.Equal(HttpStatusCode.BadRequest, respnse.StatusCode);
        }

        [Fact]
        public async Task Post_info_IsAcceptedButNeverForwarded()
        {
            var before = notificationApiFactory.DiscordCalls;

            var respnse = await notificationApiFactory.CreateClient().PostAsync("/notifications", 
                Json("""{"level": "Info", "service": "billing", "message":"billing service is running"}"""));

            Assert.Equal(HttpStatusCode.Accepted, respnse.StatusCode);
            Assert.Equal("NotRequired", (await respnse.Content.ReadFromJsonAsync<ReceiveNotificationResponse>())!.Status);
            await Task.Delay(600); //deliberately longer than poll interval 
            Assert.Equal(before, notificationApiFactory.DiscordCalls);

        }

        [Fact]
        public async Task Post_WarningOfAnyShape_ReachedDiscordWithGeneratedMessage()
        {
            var before = notificationApiFactory.DiscordCalls;

            var respnse = await notificationApiFactory.CreateClient().PostAsync("/notifications",
                Json("""{"level": "warn", "host": "db-02", "message":"disk is almost full."}"""));

            Assert.Equal(HttpStatusCode.Accepted, respnse.StatusCode);
            Assert.Equal("Pending", (await respnse.Content.ReadFromJsonAsync<ReceiveNotificationResponse>())!.Status);

            await NotificationApiFactory.WaitUntilAsync(() =>
            notificationApiFactory.DiscordCalls > before, "worker should call the webhook");

            var body = notificationApiFactory.Discord?.LogEntries?.Last()?.RequestMessage?.Body;
             
            Assert.Contains("[WARNING]", body);
            Assert.Contains("automatic summary was unavailable", body);
            Assert.False(notificationApiFactory.DiscordReceived("db-02"));

        }

        [Fact]
        public async Task Post_Forwardtest()
        {
            var before = notificationApiFactory.DiscordCalls;

            var respnse = await notificationApiFactory.CreateClient().PostAsync("/notifications",
                Json("""{"level": "warn", "host": "db-02", "message":"disk is almost full."}"""));
            var id = (await respnse.Content.ReadFromJsonAsync<ReceiveNotificationResponse>())!.Id;

            using var scope = notificationApiFactory.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ForwardPendingNotificationService>();
            await service.ForwardNextAsync(CancellationToken.None);
            

            var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
            var row = await db.Notifications.AsNoTracking().SingleAsync(x => x.Id == id);

            Assert.True(row.Status == Domain.ForwardingStatus.Sent, $"Status={row.Status} LastError={row.LastError} DiscordCalls={notificationApiFactory.DiscordCalls}");

        }
    }
}
