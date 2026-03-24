// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.Extensions.Validation;

namespace BlazorServerDemo.Data;

/// <summary>
/// Validates that a username is not already taken.
/// Simulates a 2-second database round-trip with occasional failures.
/// </summary>
public sealed class UniqueUsernameAttribute : AsyncValidationAttribute
{
    private static readonly HashSet<string> TakenUsernames = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin",
        "root",
        "blazor",
    };

    public UniqueUsernameAttribute()
    {
        ErrorMessage = "UniqueUsernameError";
    }

    protected override async Task<ValidationResult?> IsValidAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken)
    {
        if (value is not string username || string.IsNullOrWhiteSpace(username))
        {
            return ValidationResult.Success;
        }

        // Simulate a database round-trip
        await Task.Delay(2000, cancellationToken);

        // Simulate a random infrastructure failure ~20% of the time for "blaz" prefix
        if (username.StartsWith("blaz", StringComparison.OrdinalIgnoreCase) && Random.Shared.Next(5) == 0)
        {
            throw new InvalidOperationException("Simulated server error checking username availability.");
        }

        if (TakenUsernames.Contains(username))
        {
            return new ValidationResult(
                string.Format(CultureInfo.CurrentCulture, ErrorMessageString, username),
                [validationContext.MemberName!]);
        }

        return ValidationResult.Success;
    }
}
