using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence
{
    /// <summary>
    /// Seeds initial data into the in-memory database.
    /// </summary>
    public class DataSeeder : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DataSeeder> _logger;

        public DataSeeder(IServiceProvider serviceProvider, ILogger<DataSeeder> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting data seeding...");

            using var scope = _serviceProvider.CreateScope();
            var customerRepository = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();
            var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

            try
            {
                // Seed customers
                var customers = await SeedCustomers(customerRepository, cancellationToken);
                _logger.LogInformation("Seeded {CustomerCount} customers", customers.Count);

                // Seed orders
                var orders = await SeedOrders(orderRepository, customers, cancellationToken);
                _logger.LogInformation("Seeded {OrderCount} orders", orders.Count);

                _logger.LogInformation("Data seeding completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during data seeding");
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private static async Task<List<Customer>> SeedCustomers(ICustomerRepository customerRepository, CancellationToken cancellationToken)
        {
            var customers = new List<Customer>();

            // Check if customers already exist
            var existingCustomers = await customerRepository.ListAllAsync(cancellationToken);
            if (existingCustomers.Count > 0)
            {
                return existingCustomers.ToList();
            }

            // Create sample customers
            var customerData = new[]
            {
                new { Name = "John Doe", Email = "john.doe@example.com", Preferences = new Dictionary<string, object> { { "newsletter", true }, { "notifications", "email" } } },
                new { Name = "Jane Smith", Email = "jane.smith@example.com", Preferences = new Dictionary<string, object> { { "newsletter", false }, { "notifications", "sms" } } },
                new { Name = "Bob Wilson", Email = "bob.wilson@example.com", Preferences = new Dictionary<string, object> { { "newsletter", true }, { "notifications", "push" } } },
                new { Name = "Alice Brown", Email = "alice.brown@example.com", Preferences = new Dictionary<string, object> { { "newsletter", true }, { "notifications", "email" } } },
                new { Name = "Charlie Davis", Email = "charlie.davis@example.com", Preferences = new Dictionary<string, object> { { "newsletter", false }, { "notifications", "none" } } }
            };

            foreach (var data in customerData)
            {
                var customerId = CustomerId.New();
                var email = new Email(data.Email);
                var customer = Customer.Create(
                    customerId,
                    data.Name,
                    email,
                    preferences: data.Preferences);

                await customerRepository.SaveAsync(customer, cancellationToken);
                customers.Add(customer);
            }

            return customers;
        }

        private static async Task<List<Order>> SeedOrders(IOrderRepository orderRepository, List<Customer> customers, CancellationToken cancellationToken)
        {
            var orders = new List<Order>();

            // Check if orders already exist
            var existingOrders = await orderRepository.ListAllAsync(cancellationToken);
            if (existingOrders.Count > 0)
            {
                return existingOrders.ToList();
            }

            // Create sample orders
            var orderData = new[]
            {
                new { CustomerIndex = 0, Amount = 99.99m, Currency = "USD", Details = new Dictionary<string, object> { { "items", new[] { "Laptop", "Mouse" } }, { "shipping", "express" } } },
                new { CustomerIndex = 0, Amount = 49.99m, Currency = "USD", Details = new Dictionary<string, object> { { "items", new[] { "Keyboard" } }, { "shipping", "standard" } } },
                new { CustomerIndex = 1, Amount = 149.99m, Currency = "USD", Details = new Dictionary<string, object> { { "items", new[] { "Monitor", "Cable" } }, { "shipping", "express" } } },
                new { CustomerIndex = 2, Amount = 29.99m, Currency = "USD", Details = new Dictionary<string, object> { { "items", new[] { "Book" } }, { "shipping", "standard" } } },
                new { CustomerIndex = 3, Amount = 199.99m, Currency = "USD", Details = new Dictionary<string, object> { { "items", new[] { "Tablet", "Case" } }, { "shipping", "express" } } },
                new { CustomerIndex = 1, Amount = 75.50m, Currency = "USD", Details = new Dictionary<string, object> { { "items", new[] { "Headphones" } }, { "shipping", "standard" } } }
            };

            foreach (var data in orderData)
            {
                var customer = customers[data.CustomerIndex];
                var money = new Money(data.Amount, data.Currency);
                var orderId = OrderId.New();

                var order = Order.Create(
                    orderId,
                    customer.CustomerId,
                    money,
                    data.Details);

                await orderRepository.SaveAsync(order, cancellationToken);
                orders.Add(order);
            }

            return orders;
        }
    }
}