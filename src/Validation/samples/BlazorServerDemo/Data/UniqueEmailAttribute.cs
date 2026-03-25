// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.Extensions.Validation;

namespace BlazorServerDemo.Data;

/// <summary>
/// Validates that an email address is not already registered.
/// Resolves <see cref="UserService"/> from DI to perform the check.
/// </summary>
public sealed class UniqueEmailAttribute : AsyncValidationAttribute
{
    protected override async Task<ValidationResult?> IsValidAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken)
    {
        if (value is not string email || string.IsNullOrWhiteSpace(email))
        {
            return ValidationResult.Success;
        }

        var userService = validationContext.GetRequiredService<UserService>();

        if (await userService.IsEmailTakenAsync(email, cancellationToken))
        {
            return new ValidationResult(
                string.Format(CultureInfo.CurrentCulture, ErrorMessageString, validationContext.DisplayName),
                [validationContext.MemberName!]);
        }

        return ValidationResult.Success;
    }
}
