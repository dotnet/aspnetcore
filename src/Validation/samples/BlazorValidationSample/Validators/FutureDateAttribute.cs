// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace BlazorValidationSample.Validators;

/// <summary>
/// A custom <see cref="ValidationAttribute"/> that validates a <see cref="DateTime"/>
/// value is in the future.
/// </summary>
public class FutureDateAttribute : ValidationAttribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="FutureDateAttribute"/>.
    /// </summary>
    public FutureDateAttribute()
    {
        ErrorMessage ??= "FutureDateError";
    }

    /// <inheritdoc />
    public override string FormatErrorMessage(string name)
        => string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name);

    /// <inheritdoc />
    public override bool IsValid(object? value) => value is DateTime date && date > DateTime.Now;
}
