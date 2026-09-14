using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notifications.Application.Abstractions;
using Notifications.Domain;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace Notifications.Infrastructure.Messaging
{
    public sealed class DiscordWebhookSink(
        HttpClient httpClient,
        IOptions<DiscordOptions> options,
        ILogger<DiscordWebhookSink> logger) : INotificationSync
    {
        public async Task SendAsync(Notification notification, GeneratedMessage generatedMessage, CancellationToken cancellationToken)
        {
            //Discord limits: title 256 chars, description 4096 chars, embed 6000 chars, total message 2000 chars

            var payload = new DiscordWebhookPayload(
                [
                    new DiscordEmbed(
                        Title: Truncate($"[{ notification.Level.ToString().ToUpperInvariant() }] { generatedMessage.Title}", 256),
                        Description: Truncate(generatedMessage.Summary, 4096),
                        Color: GetColor(notification.Level),
                        Timestamp: DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        Fields:
                        [
                            new DiscordField(
                                Name: "Category",
                                Value: Truncate(generatedMessage.Category, 256),
                                Inline: true
                            ),
                            new DiscordField(
                                Name: "Suggested Action",
                                Value: Truncate(generatedMessage.SuggestedAction, 1024),
                                Inline: false
                            )
                        ],
                        Footer: new DiscordFooter(Text: $"Notification {notification.Id}")
                    )
                ]

                );
            try
            {
                using var response = await httpClient.PostAsJsonAsync(options.Value.WebhookUrl, payload, cancellationToken);
                response.EnsureSuccessStatusCode();
                logger.LogInformation("Notification sent to Discord webhook successfully.");
            }
            catch (Exception ex)
            {

                logger.LogError(ex, "Notification sent to Discord webhook successfully.");
                throw;
            }
            
        }

        private static int GetColor(NotificationLevel level) => level switch
        {
            NotificationLevel.Critical => 0xFF0000, // Red
            NotificationLevel.Error => 0xFF4500, // OrangeRed
            NotificationLevel.Warning => 0xFFA500, // Orange
            _ => 0xF59C00 // Yellow
        };

        private string Truncate(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];



        private sealed record DiscordWebhookPayload(
           [property: JsonPropertyName("embeds")] DiscordEmbed[] Embeds);

        private sealed record DiscordEmbed(
            [property: JsonPropertyName("title")] string Title,
            [property: JsonPropertyName("description")] string Description,
            [property: JsonPropertyName("color")] int Color,
            [property: JsonPropertyName("timestamp")] string Timestamp,
            [property: JsonPropertyName("fields")] DiscordField[] Fields,
            [property: JsonPropertyName("footer")] DiscordFooter Footer
            );

        private sealed record DiscordField(
            [property: JsonPropertyName("name")] string Name,
            [property: JsonPropertyName("value")] string Value,
            [property: JsonPropertyName("inline")] bool Inline
        );

        private sealed record DiscordFooter(
            [property: JsonPropertyName("text")] string Text
        );
    }
}
