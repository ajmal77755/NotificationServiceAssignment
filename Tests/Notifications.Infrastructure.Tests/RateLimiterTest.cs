using Notifications.Application;
using WireMock.Server;
using WireMock.ResponseBuilders;
using WireMock.RequestBuilders;
using NSubstitute;
using Microsoft.Extensions.Time.Testing;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using Notifications.Application.Abstractions;
using Notifications.Infrastructure.RateLimiting;
using Castle.Core.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Notifications.Infrastructure.Tests
{
    public sealed class RateLimiterTest
    {
        private static readonly DateTimeOffset Start = new(2026, 9, 13, 0, 0, 0, TimeSpan.Zero);

        private readonly List<DateTimeOffset> _sent = [];
        private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();
        private readonly FakeTimeProvider _clock = new(Start);

        public RateLimiterTest()
        {
            _repository.GetSentNotificationsTimeSineAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).
                Returns(call =>
                {
                    var since = call.ArgAt<DateTimeOffset>(0);
                    return (IReadOnlyList<DateTimeOffset>)_sent.Where(x => x >= since).OrderBy(t => t).ToList();
                });
        }

        private SlidingLogRateLimiter CreateSut() => 
            new(_repository, _clock, Options.Create(new OutboundRateLimitOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }),
                NullLogger<SlidingLogRateLimiter>.Instance);
        

        [Fact]
        public void WaitForSlotAsync_NineSentInWindow()
        {
            _sent.AddRange(Enumerable.Range(0,9).Select(x => Start.AddSeconds(x)));
            var task = CreateSut().WaitForSlotAsync(CancellationToken.None);
            Assert.True(task.IsCompletedSuccessfully);
        }

        [Fact]
        public async Task WaitForSlotAsync_TenSentinWindow_WaitUntilOldestLeaves()
        {
            _sent.AddRange(Enumerable.Range(0, 10).Select(x => Start.AddSeconds(x)));
            var task = CreateSut().WaitForSlotAsync(CancellationToken.None);
            Assert.False(task.IsCompleted);

            _clock.Advance(TimeSpan.FromSeconds(59));
            Assert.False(task.IsCompleted);

            _clock.Advance(TimeSpan.FromSeconds(1.1));
            await task.WaitAsync(TimeSpan.FromSeconds(2));


        }
    }
}
