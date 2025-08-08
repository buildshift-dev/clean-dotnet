using System;
using SharedKernel.Domain.Exceptions;

namespace Domain.Exceptions
{
    /// <summary>
    /// Exception raised when a requested resource is not found.
    /// </summary>
    public sealed class ResourceNotFoundException : DomainException
    {
        public ResourceNotFoundException(string resourceType, string resourceId)
            : base($"{resourceType} with ID {resourceId} not found", "RESOURCE_NOT_FOUND")
        {
            ResourceType = resourceType;
            ResourceId = resourceId;
        }

        public string ResourceType { get; }
        public string ResourceId { get; }
    }
}