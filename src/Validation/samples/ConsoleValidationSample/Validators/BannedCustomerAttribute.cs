// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using ConsoleValidationSample.Models;
using Microsoft.Extensions.Validation.Localization.Attributes;

namespace ConsoleValidationSample.Validators;

/// <summary>
/// A custom <see cref="ValidationAttribute"/> that rejects customers with a banned name.
/// Demonstrates class-level validation with <see cref="AllowMultiple"/> and localized error messages.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="BannedCustomerAttribute"/> with the specified banned name.
/// </remarks>
/// <param name="bannedName">The customer name to reject.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class BannedCustomerAttribute(string bannedName) : ValidationAttribute, IValidationAttributeFormatter
{
    /// <summary>
    /// Gets the name that is banned.
    /// </summary>
    public string BannedName { get; } = bannedName;

    /// <inheritdoc />
    public override bool IsValid(object? value) => value is not Customer customer || customer.Name != BannedName;

    /// <inheritdoc />
    public override string FormatErrorMessage(string name) => string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, BannedName);

    public string FormatErrorMessage(CultureInfo culture, string messageTemplate, string displayName)
        => string.Format(culture, messageTemplate, displayName, BannedName);
}
