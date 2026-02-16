// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace MinimalApiValidationSample.Models;

/// <summary>
/// A custom <see cref="ValidationAttribute"/> that ensures the value is an even integer.
/// </summary>
public sealed class EvenNumberAttribute : ValidationAttribute
{
    /// <inheritdoc />
    public override bool IsValid(object? value)
    {
        return value is int number && number % 2 == 0;
    }
}
