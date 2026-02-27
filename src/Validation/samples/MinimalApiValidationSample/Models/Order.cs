// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace MinimalApiValidationSample.Models;

/// <summary>
/// Represents an order that uses <see cref="IValidatableObject"/> for custom validation logic.
/// </summary>
public class Order : IValidatableObject
{
    /// <summary>
    /// Gets or sets the order identifier. Must be a positive integer.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int OrderId { get; set; }

    /// <summary>
    /// Gets or sets the name of the product being ordered.
    /// </summary>
    [Required]
    public required string ProductName { get; set; }

    /// <summary>
    /// Gets or sets the quantity ordered. Validated via <see cref="Validate"/>.
    /// </summary>
    public int Quantity { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Quantity <= 0)
        {
            yield return new ValidationResult(
                "Quantity must be greater than zero",
                [nameof(Quantity)]);
        }
    }
}
