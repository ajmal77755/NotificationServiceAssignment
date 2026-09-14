using Microsoft.Extensions.Logging;
using Notifications.Application.Abstractions;
using Notifications.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Notifications.Infrastructure.Llm
{
    /// <summary>
    /// Decorator: if LLM fails to generate a message, this class will log and return a default message instead of throwing an exception.
    /// </summary>
    /// <param name="messageGenerator"></param>
    /// <param name="logger"></param>
    public sealed class FallbackMessageGenerator(IMessageGenerator messageGenerator, ILogger<FallbackMessageGenerator> logger) : IMessageGenerator
    {
        public async Task<GeneratedMessage> GenerateMessageAsync(Notification notification, CancellationToken cancellationToken)
        {
            try
            {
                return await messageGenerator.GenerateMessageAsync(notification, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate message for notification {NotificationId}. Falling back to default message.", notification.Id);
                return GeneratedMessage.Sanitise(
                    $"{notification.Level} notification received",
                    "Other",
                    $"A notification of level {notification.Level.ToString().ToLowerInvariant()} was received but automatic summary was unavailable. Stored as {notification.Id} at {notification.ReceivedAt}.",
                    "Review the stored notification and source system directly."
                );
            }
        }
    
    }
}
