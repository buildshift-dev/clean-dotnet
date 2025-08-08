using System;

namespace SharedKernel.Domain.Events
{
    /// <summary>
    /// Base implementation of a domain event.
    /// </summary>
    public abstract class DomainEvent : IDomainEvent
    {
        protected DomainEvent()
        {
            EventId = Guid.NewGuid();
            OccurredAt = DateTime.UtcNow;
        }

        protected DomainEvent(Guid eventId, DateTime occurredAt)
        {
            EventId = eventId;
            OccurredAt = occurredAt;
        }

        public Guid EventId { get; }
        public DateTime OccurredAt { get; }
    }
}