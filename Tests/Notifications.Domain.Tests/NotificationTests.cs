using Notifications.Domain;
using System.Diagnostics.Contracts;
using System.Text.Json;

namespace Notifications.Domain.Tests
{
    public class NotificationTests
    {

        private static readonly DateTimeOffset Now = new(2026, 9, 13, 0, 0, 0, TimeSpan.Zero);
        private const string Payload = """{"level": "warning", "service": "billing", "message":"billing service is down"} """;


        [Theory]
        [InlineData(NotificationLevel.Debug, false)]
        [InlineData(NotificationLevel.Info, false)]
        [InlineData(NotificationLevel.Warning, true)]
        [InlineData(NotificationLevel.Error, true)]
        [InlineData(NotificationLevel.Critical, true)]
        public void RequiresForwarding_IsTrueWarningAndAbove(NotificationLevel notificationLevel, bool expected)
        {
            var notification = Notification.Create(Payload, notificationLevel, Now);

            Assert.Equal(expected, notification.RequiresForwarding());
            Assert.Equal(expected ? ForwardingStatus.Pending : ForwardingStatus.NotRequired, notification.Status);

        }

        [Fact]
        public void Create_StoresPayloadValue()
        {
            var n = Notification.Create(Payload, NotificationLevel.Info, Now);

            Assert.Equal(Payload, n.Payload);
        }

        [Theory]
        [InlineData("")]
        [InlineData("not json")]
        public void Create_RejectNonJsonPayload(string payload)
        {
            Assert.ThrowsAny<ArgumentException>(() => Notification.Create(payload, NotificationLevel.Info, Now));
        }

        [Fact]
        public void Create_RejectUndefinedLevel() => Assert.ThrowsAny<ArgumentException>(() => Notification.Create(Payload, (NotificationLevel)99, Now));

        [Fact]
        public void MarkSent_MovesPendingToSent()
        {
            var n = Notification.Create(Payload, NotificationLevel.Warning, Now);

            n.MarkAsSent(Now.AddSeconds(1));

            Assert.Equal(ForwardingStatus.Sent, n.Status);
            Assert.Equal(Now.AddSeconds(1), n.SentAt);
            Assert.Equal(1, n.AttemptCount);
        }

        [Fact]
        public void MarkSent_ThrowsWhenNotPending()
        {
            var n = Notification.Create(Payload, NotificationLevel.Info, Now);
            Assert.Throws<InvalidOperationException>(() => n.MarkAsSent(Now));
        }


        [Fact]
        public void RecordFailure_StaysPendingBelowMaxAttempts()
        {
            var n = Notification.Create(Payload, NotificationLevel.Error, Now);

            n.RecordFailure("failurerecord", 3, Now);

            Assert.Equal(ForwardingStatus.Pending, n.Status);
            Assert.Equal(1, n.AttemptCount);
            Assert.Equal("failurerecord", n.LastError);
        }

        [Fact]
        public void RecordFailure_MovesToFailedAfterMaxAttempt()
        {
            var n = Notification.Create(Payload, NotificationLevel.Error, Now);

            n.RecordFailure("failurerecord", 2, Now);
            n.RecordFailure("failurerecord", 2, Now.AddMinutes(2));

            Assert.Equal(ForwardingStatus.Failed, n.Status);

        }

    }

    public class GeneratedMessageTests
    {
        [Fact]
        public void Sanitise_ClampUnknownCategoryAndBlankFields()
        {
            var m = GeneratedMessage.Sanitise(" ", "", null, "");
            Assert.Equal("Notification", m.Title);
            Assert.Equal("Other", m.Category);
            Assert.Equal("No summary available", m.Summary);
            Assert.Equal("No suggested action available, Investigate the source system.", m.SuggestedAction);

        }

        [Fact]
        public void Sanitise_TruncateLongTitle()
        {
            var m = GeneratedMessage.Sanitise(new string('a', 500), "Sec", null, "");
            Assert.Equal(200, m.Title.Length);
        }
    }
}
