// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

namespace BlazorNet11.Validators;

/// <summary>
/// Async validator that simulates a database uniqueness check for email addresses.
/// Rejects known test emails after an 800ms simulated delay.
/// </summary>
public sealed class UniqueEmailAttribute : AsyncValidationAttribute
{
    private static readonly HashSet<string> _takenEmails = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin@example.com",
        "test@example.com",
        "user@example.com"
    };

    public UniqueEmailAttribute()
        : base("UniqueEmail")
    {
    }

    /// <inheritdoc />
    protected override async Task<ValidationResult?> IsValidAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken)
    {
        if (value is not string email || string.IsNullOrWhiteSpace(email))
        {
            return ValidationResult.Success;
        }

        // Simulate a slow database lookup
        await Task.Delay(800, cancellationToken);

        if (_takenEmails.Contains(email))
        {
            return new ValidationResult(
                ErrorMessage,
                new[] { validationContext.MemberName! });
        }

        return ValidationResult.Success;
    }
}
