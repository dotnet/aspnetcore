// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

namespace MinimalApiValidationSample.Models;

/// <summary>
/// Represents a customer with validated properties and a nested <see cref="Address"/>.
/// </summary>
#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
[ValidatableType]
#pragma warning restore ASP0029
public class Customer
{
    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    [Required]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the customer email address.
    /// </summary>
    [EmailAddress]
    public required string Email { get; set; }

    /// <summary>
    /// Gets or sets the customer age. Must be between 18 and 120.
    /// </summary>
    [Range(18, 120)]
    [Display(Name = "Customer Age")]
    public int Age { get; set; }

    /// <summary>
    /// Gets or sets the customer's home address. Demonstrates nested object validation.
    /// </summary>
    public Address HomeAddress { get; set; } = new Address
    {
        Street = "123 Main St",
        City = "Anytown",
        ZipCode = "12345"
    };
}
