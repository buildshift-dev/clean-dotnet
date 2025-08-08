namespace SharedKernel.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when a business rule is violated.
    /// </summary>
    public class BusinessRuleViolationException : DomainException
    {
        public BusinessRuleViolationException(string message, string? ruleName = null)
            : base(message, ruleName)
        {
            RuleName = ruleName;
        }

        public string? RuleName { get; }
    }
}