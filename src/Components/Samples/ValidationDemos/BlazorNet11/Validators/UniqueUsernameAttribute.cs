// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

namespace BlazorNet11.Validators;

/// <summary>
/// Async validator that simulates a database uniqueness check for usernames.
/// Rejects known reserved usernames after a 500ms simulated delay.
/// </summary>
public sealed class UniqueUsernameAttribute : AsyncValidationAttribute
{
    private static readonly HashSet<string> _takenUsernames = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin",
        "root",
        "test"
    };

    public UniqueUsernameAttribute()
        : base("UniqueUsername")
    {
    }

    /// <inheritdoc />
    protected override async Task<ValidationResult?> IsValidAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken)
    {
        if (value is not string username || string.IsNullOrWhiteSpace(username))
        {
            return ValidationResult.Success;
        }

        // Simulate a database lookup
        await Task.Delay(500, cancellationToken);

        if (_takenUsernames.Contains(username))
        {
            return new ValidationResult(
                ErrorMessage,
                new[] { validationContext.MemberName! });
        }

        return ValidationResult.Success;
    }
}
