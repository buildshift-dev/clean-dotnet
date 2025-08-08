using System;

namespace SharedKernel.Common
{
    /// <summary>
    /// Abstraction for date/time operations to support testing.
    /// </summary>
    public interface IDateTime
    {
        DateTime Now { get; }
        DateTime UtcNow { get; }
        DateTime Today { get; }
    }

    /// <summary>
    /// System implementation of IDateTime.
    /// </summary>
    public class SystemDateTime : IDateTime
    {
        public DateTime Now => DateTime.Now;
        public DateTime UtcNow => DateTime.UtcNow;
        public DateTime Today => DateTime.Today;
    }
}