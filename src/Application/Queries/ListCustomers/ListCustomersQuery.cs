using System.Collections.Generic;
using Domain.Entities;
using MediatR;
using SharedKernel.Common;

namespace Application.Queries.ListCustomers
{
    /// <summary>
    /// Query to retrieve all customers.
    /// </summary>
    public sealed class ListCustomersQuery : IRequest<Result<IReadOnlyList<Customer>>>
    {
    }
}