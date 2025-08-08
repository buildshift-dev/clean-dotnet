using System;

namespace SharedKernel.Domain.Exceptions
{
    /// <summary>
    /// Base exception for domain-related errors.
    /// </summary>
    public class DomainException : Exception
    {
        public DomainException(string message, string? errorCode = null)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public DomainException(string message, Exception innerException, string? errorCode = null)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }

        public string? ErrorCode { get; }
    }
}