// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using ConsoleValidationSample.Models;

namespace ConsoleValidationSample.Validators;

/// <summary>
/// A custom <see cref="ValidationAttribute"/> that rejects customers with a banned name.
/// Demonstrates class-level validation with <see cref="AllowMultiple"/> and localized error messages.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class BannedCustomerAttribute : ValidationAttribute
{
    /// <summary>
    /// Gets the name that is banned.
    /// </summary>
    public string BannedName { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="BannedCustomerAttribute"/> with the specified banned name.
    /// </summary>
    /// <param name="bannedName">The customer name to reject.</param>
    public BannedCustomerAttribute(string bannedName)
    {
        BannedName = bannedName;
        ErrorMessage ??= "The customer {1} is banned";
    }

    /// <inheritdoc />
    public override string FormatErrorMessage(string name) => string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, BannedName);

    /// <inheritdoc />
    public override bool IsValid(object? value) => value is not Customer customer || customer.Name != BannedName;
}
