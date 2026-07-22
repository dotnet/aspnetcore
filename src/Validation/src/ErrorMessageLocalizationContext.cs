// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Provides context to the <see cref="IValidationLocalizer.ResolveErrorMessage"/> method
/// for resolving localized or customized error messages for validation attributes.
/// </summary>
public readonly struct ErrorMessageLocalizationContext
{
    /// <summary>
    /// Gets the CLR name for the member being validated.
    /// </summary>
    public required string MemberName { get; init; }

    /// <summary>
    /// Gets the resolved display name for the member being validated.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Gets the type that declares the member being validated.
    /// <see langword="null"/> for top-level parameter validation.
    /// </summary>
    public Type? DeclaringType { get; init; }

    /// <summary>
    /// Gets the validation attribute that produced the error.
    /// </summary>
    public required ValidationAttribute Attribute { get; init; }
}
