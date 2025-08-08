using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SharedKernel.Domain.Events;

namespace SharedKernel.Domain.BaseTypes
{
    /// <summary>
    /// Base class for aggregate roots.
    /// Manages domain events and provides aggregate-specific functionality.
    /// </summary>
    public abstract class AggregateRoot : Entity
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected AggregateRoot(Guid id) : base(id) { }

        protected AggregateRoot() : base() { }

        /// <summary>
        /// Adds a domain event to be raised.
        /// </summary>
        protected void AddDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        /// <summary>
        /// Removes a domain event.
        /// </summary>
        protected void RemoveDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Remove(domainEvent);
        }

        /// <summary>
        /// Clears all domain events.
        /// </summary>
        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }

        /// <summary>
        /// Collects and clears domain events.
        /// </summary>
        public IReadOnlyCollection<IDomainEvent> CollectDomainEvents()
        {
            var events = new ReadOnlyCollection<IDomainEvent>(_domainEvents.ToList());
            _domainEvents.Clear();
            return events;
        }
    }
}