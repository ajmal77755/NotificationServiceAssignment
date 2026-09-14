using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Notifications.Domain
{
    public sealed class Notification
    {
        public Guid Id { get; private set; }
        public string Payload { get; private set; } = string.Empty;
        public NotificationLevel Level { get; private set; }
        public DateTimeOffset ReceivedAt { get; private set; }
        public ForwardingStatus Status { get; private set; }

        public int AttemptCount { get; private set; }
        public DateTimeOffset LastAttemptAt { get; private set; }
        public DateTimeOffset SentAt { get; private set; }
        public string? LastError { get; private set; }

        private Notification() { }

        private Notification(Guid id, string payload, NotificationLevel level, DateTimeOffset receivedAt)
        {
            Id = id;
            Payload = payload;
            Level = level;
            ReceivedAt = receivedAt;
            Status = RequiresForwarding() ? ForwardingStatus.Pending : ForwardingStatus.NotRequired;
        }

        public bool RequiresForwarding()
        {
            return Level >= NotificationLevel.Warning;
        }


        public static Notification Create(string payload, NotificationLevel level, DateTimeOffset receivedAt)
        {
            var id = Guid.NewGuid();
            ArgumentNullException.ThrowIfNullOrWhiteSpace(payload, nameof(payload));
            if(!Enum.IsDefined(level))
                throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown level");
            // Additional validation can be added here if needed like max length checks, etc.

            JsonValueKind kind;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(payload);
                kind = doc.RootElement.ValueKind;
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("payload is not a valid JSON.", nameof(payload), ex);
            }

            if(kind != JsonValueKind.Object)
                throw new ArgumentException("payload is not a valid JSON.", nameof(payload));

            return new Notification(id, payload, level, receivedAt);
        }

        public void MarkAsSent(DateTimeOffset sentAt)
        {
            EnsurePending();
            AttemptCount++;
            LastAttemptAt = sentAt;
            LastError = null;
            Status = ForwardingStatus.Sent;
            SentAt = sentAt;
        }

        private void EnsurePending()
        {
            if (Status != ForwardingStatus.Pending)
            {
                throw new InvalidOperationException($"Notification {Id} is {Status}. not pending.");
            }
        }
        
        public void RecordFailure(string message, int maxAttempts, DateTimeOffset lastTryAt)
        {
            EnsurePending();
            AttemptCount++;
            LastAttemptAt = lastTryAt;
            LastError = message;
            Status = AttemptCount >= maxAttempts ? ForwardingStatus.Failed : ForwardingStatus.Pending;
        }
    }
}
