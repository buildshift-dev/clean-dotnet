using System;
using Domain.Entities;
using MediatR;
using SharedKernel.Common;

namespace Application.Queries.GetCustomer
{
    /// <summary>
    /// Query to retrieve a specific customer by ID.
    /// </summary>
    public sealed class GetCustomerQuery : IRequest<Result<Customer>>
    {
        public Guid CustomerId { get; init; }
    }
}