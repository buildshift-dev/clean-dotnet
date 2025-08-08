using System;
using MediatR;

namespace SharedKernel.Domain.Events
{
    /// <summary>
    /// Marker interface for domain events.
    /// Extends INotification for MediatR integration.
    /// </summary>
    public interface IDomainEvent : INotification
    {
        Guid EventId { get; }
        DateTime OccurredAt { get; }
    }
}