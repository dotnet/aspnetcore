// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace MinimalApiValidationSample.Models;

/// <summary>
/// Represents a mailing address with validated properties.
/// </summary>
public class Address
{
    /// <summary>
    /// Gets or sets the street address.
    /// </summary>
    [Required]
    public required string Street { get; set; }

    /// <summary>
    /// Gets or sets the city name.
    /// </summary>
    [Required]
    public required string City { get; set; }

    /// <summary>
    /// Gets or sets the ZIP code. Must be at most 5 characters.
    /// </summary>
    [StringLength(5)]
    public required string ZipCode { get; set; }
}
