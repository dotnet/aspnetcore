// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Provides context for the <see cref="ValidationOptions.ErrorMessageKeyProvider"/> callback
/// to determine the localization lookup key for a validation attribute's error message.
/// </summary>
public readonly struct ErrorMessageKeyContext
{
    /// <summary>
    /// Gets the validation attribute that produced the error.
    /// </summary>
    public required ValidationAttribute Attribute { get; init; }

    /// <summary>
    /// Gets the name of the member (property or parameter) being validated.
    /// </summary>
    public required string MemberName { get; init; }

    /// <summary>
    /// Gets the resolved display name for the member being validated.
    /// This value is already localized if localization is configured.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Gets the type that declares the member being validated.
    /// <see langword="null"/> for top-level parameter validation.
    /// </summary>
    public Type? DeclaringType { get; init; }
}
