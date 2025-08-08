using System;
using System.Collections.Generic;
using System.Text.Json;
using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Entity Framework configuration for Customer entity.
    /// </summary>
    public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("customers");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id)
                .HasConversion(
                    id => id,
                    value => value)
                .ValueGeneratedNever();

            // CustomerId value object
            builder.Property(c => c.CustomerId)
                .HasConversion(
                    customerId => customerId.Value,
                    value => new CustomerId(value))
                .HasColumnName("customer_id");

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("name");

            // Email value object
            builder.Property(c => c.Email)
                .HasConversion(
                    email => email.Value,
                    value => new Email(value))
                .IsRequired()
                .HasMaxLength(320)
                .HasColumnName("email");

            builder.HasIndex(c => c.Email)
                .IsUnique()
                .HasDatabaseName("ix_customers_email");

            // Address value object (optional)
            builder.OwnsOne(c => c.Address, addressBuilder =>
            {
                addressBuilder.Property(a => a.Street)
                    .HasMaxLength(200)
                    .HasColumnName("address_street");

                addressBuilder.Property(a => a.City)
                    .HasMaxLength(100)
                    .HasColumnName("address_city");

                addressBuilder.Property(a => a.State)
                    .HasMaxLength(50)
                    .HasColumnName("address_state");

                addressBuilder.Property(a => a.PostalCode)
                    .HasMaxLength(20)
                    .HasColumnName("address_postal_code");

                addressBuilder.Property(a => a.Country)
                    .HasMaxLength(100)
                    .HasColumnName("address_country");

                addressBuilder.Property(a => a.Apartment)
                    .HasMaxLength(50)
                    .HasColumnName("address_apartment");
            });

            // PhoneNumber value object (optional)
            builder.OwnsOne(c => c.PhoneNumber, phoneBuilder =>
            {
                phoneBuilder.Property(p => p.Value)
                    .HasMaxLength(20)
                    .HasColumnName("phone_number");

                phoneBuilder.Property(p => p.CountryCode)
                    .HasMaxLength(5)
                    .HasColumnName("phone_country_code");
            });

            builder.Property(c => c.IsActive)
                .IsRequired()
                .HasColumnName("is_active");

            // JSON serialization for preferences dictionary
            builder.Property(c => c.Preferences)
                .HasConversion(
                    preferences => JsonSerializer.Serialize(preferences, (JsonSerializerOptions?)null),
                    json => JsonSerializer.Deserialize<Dictionary<string, object>>(json, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
                .HasColumnName("preferences");

            builder.Property(c => c.CreatedAt)
                .IsRequired()
                .HasColumnName("created_at");

            builder.Property(c => c.UpdatedAt)
                .IsRequired()
                .HasColumnName("updated_at");

            // Ignore domain events - they're not persisted
            builder.Ignore(c => c.DomainEvents);
        }
    }
}