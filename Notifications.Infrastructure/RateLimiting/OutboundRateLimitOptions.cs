using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Notifications.Infrastructure.RateLimiting
{
    public sealed class OutboundRateLimitOptions
    {
        public const string SectionName = "OutboundRateLimit";

        [Range(1, 1000)]
        public int PermitLimit { get; set; } = 10;

        public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
    }
}
