using Microsoft.Extensions.Time.Testing;
using Notifications.Application;
using Notifications.Application.Notifications;
using Notifications.Domain;
using NSubstitute;
namespace Notifications.Applications.Tests
{
    public class ReceiveNotificationHandlerTests
    {

        private static readonly DateTimeOffset Now = new(2026, 9, 13, 0, 0, 0, TimeSpan.Zero);
        private const string Payload = """{"level": "warning", "service": "billing", "message":"billing service is down"} """;
        private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 9, 13, 9, 0, 0, TimeSpan.Zero));

        private readonly INotificationRepository notificationRepository = Substitute.For<INotificationRepository>();

        [Theory]
        [InlineData(NotificationLevel.Info, ForwardingStatus.NotRequired)]
        [InlineData(NotificationLevel.Warning, ForwardingStatus.Pending)]
        public async Task HandleAsync_PersistWithCorrectStatus(NotificationLevel level, ForwardingStatus expectedStatus)
        {
            var sut = new ReceiveNotificationHandler(notificationRepository, _clock);

            var result = await sut.HandleAsync(new ReceiveNotificationCommand(Payload, level), CancellationToken.None);

            Assert.Equal(expectedStatus, result.Status);

            await notificationRepository.Received(1).AddAsync(
                Arg.Is<Notification>(n => n.Id == result.Id && n.Payload == Payload && n.ReceivedAt == _clock.GetUtcNow()),
                Arg.Any<CancellationToken>());

            await notificationRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        }
        [Fact]
        public async Task HandleAsync_InvalidPayload_ThrowsbeforeTouchingRepo()
        {
            var sut = new ReceiveNotificationHandler(notificationRepository, _clock);

            await Assert.ThrowsAnyAsync<Exception>(()=>sut.HandleAsync(new ReceiveNotificationCommand("[]", NotificationLevel.Info), CancellationToken.None));

            await notificationRepository.DidNotReceive().AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());

        }
    }
}
