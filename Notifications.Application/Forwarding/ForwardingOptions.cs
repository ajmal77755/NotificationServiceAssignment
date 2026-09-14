using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Notifications.Application.Forwarding
{
    public sealed class ForwardingOptions
    {
        public const string SectionName = "Forwarding";

        [Range(1, 20)]
        public int MaxAttempts { get; set; } = 5;


        public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(2);

    }
}
