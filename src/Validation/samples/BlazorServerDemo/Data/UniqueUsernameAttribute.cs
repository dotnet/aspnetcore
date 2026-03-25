// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.Extensions.Validation;

namespace BlazorServerDemo.Data;

/// <summary>
/// Validates that a username is not already taken.
/// Resolves <see cref="UserService"/> from DI to perform the check.
/// </summary>
public sealed class UniqueUsernameAttribute : AsyncValidationAttribute
{
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

        var userService = validationContext.GetRequiredService<UserService>();

        if (await userService.IsUsernameTakenAsync(username, cancellationToken))
        {
            return new ValidationResult(
                string.Format(CultureInfo.CurrentCulture, ErrorMessageString, username),
                [validationContext.MemberName!]);
        }

        return ValidationResult.Success;
    }
}
