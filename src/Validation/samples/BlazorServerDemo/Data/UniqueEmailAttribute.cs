// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.Extensions.Validation;

namespace BlazorServerDemo.Data;

/// <summary>
/// Validates that an email address is not already registered.
/// Simulates a 1.5-second database round-trip.
/// </summary>
public sealed class UniqueEmailAttribute : AsyncValidationAttribute
{
    private static readonly HashSet<string> TakenEmails = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin@example.com",
        "test@example.com",
        "user@example.com",
    };

    public UniqueEmailAttribute()
    {
        ErrorMessage = "UniqueEmailError";
    }

    protected override async Task<ValidationResult?> IsValidAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken)
    {
        if (value is not string email || string.IsNullOrWhiteSpace(email))
        {
            return ValidationResult.Success;
        }

        // Simulate a database round-trip
        await Task.Delay(1500, cancellationToken);

        if (TakenEmails.Contains(email))
        {
            return new ValidationResult(
                string.Format(CultureInfo.CurrentCulture, ErrorMessageString, email),
                [validationContext.MemberName!]);
        }

        return ValidationResult.Success;
    }
}
