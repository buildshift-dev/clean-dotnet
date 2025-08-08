using System;
using System.Collections.Generic;
using System.Text.Json;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Entity Framework configuration for Order entity.
    /// </summary>
    public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("orders");

            builder.HasKey(o => o.Id);

            builder.Property(o => o.Id)
                .HasConversion(
                    id => id,
                    value => value)
                .ValueGeneratedNever();

            // OrderId value object
            builder.Property(o => o.OrderId)
                .HasConversion(
                    orderId => orderId.Value,
                    value => new OrderId(value))
                .HasColumnName("order_id");

            // CustomerId value object
            builder.Property(o => o.CustomerId)
                .HasConversion(
                    customerId => customerId.Value,
                    value => new CustomerId(value))
                .HasColumnName("customer_id");

            builder.HasIndex(o => o.CustomerId)
                .HasDatabaseName("ix_orders_customer_id");

            // Money value object
            builder.OwnsOne(o => o.TotalAmount, moneyBuilder =>
            {
                moneyBuilder.Property(m => m.Amount)
                    .HasPrecision(18, 2)
                    .IsRequired()
                    .HasColumnName("total_amount");

                moneyBuilder.Property(m => m.Currency)
                    .IsRequired()
                    .HasMaxLength(3)
                    .HasColumnName("currency");
            });

            builder.Property(o => o.Status)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnName("status");

            // JSON serialization for details dictionary
            builder.Property(o => o.Details)
                .HasConversion(
                    details => JsonSerializer.Serialize(details, (JsonSerializerOptions?)null),
                    json => JsonSerializer.Deserialize<Dictionary<string, object>>(json, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
                .HasColumnName("details");

            builder.Property(o => o.CreatedAt)
                .IsRequired()
                .HasColumnName("created_at");

            builder.Property(o => o.UpdatedAt)
                .IsRequired()
                .HasColumnName("updated_at");

            // Ignore domain events - they're not persisted
            builder.Ignore(o => o.DomainEvents);
        }
    }
}