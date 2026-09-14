using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Notifications.Infrastructure.Messaging
{
    public sealed class DiscordOptions
    {
        public static string SectionName { get; set; } = "Discord";

        [Required, Url]
        public string WebhookUrl { get; set; } = string.Empty;
    }
}
