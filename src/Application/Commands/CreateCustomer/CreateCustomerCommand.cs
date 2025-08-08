using System.Collections.Generic;
using Domain.Entities;
using MediatR;
using SharedKernel.Common;

namespace Application.Commands.CreateCustomer
{
    /// <summary>
    /// Command to create a new customer.
    /// </summary>
    public sealed class CreateCustomerCommand : IRequest<Result<Customer>>
    {
        public string Name { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public Dictionary<string, object>? Preferences { get; init; }
    }
}