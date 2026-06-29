// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation.Localization;

/// <summary>
/// Provides context to the <see cref="IValidationLocalizer.ResolveDisplayName"/> method
/// for resolving localized display names for properties and parameters.
/// </summary>
public readonly struct DisplayNameLocalizationContext
{
    /// <summary>
    /// Gets the type that declares the member being validated for property-level validation,
    /// the validated type itself for type-level validation,
    /// or <see langword="null"/> for parameter validation.
    /// </summary>
    public Type? Type { get; init; }

    /// <summary>
    /// Gets the display name from <see cref="DisplayAttribute.Name"/> to use as a localization lookup key.
    /// </summary>
    public required string? DisplayName { get; init; }

    /// <summary>
    /// Gets the CLR member name (property name, parameter name, or type name) being validated.
    /// Use this as the fallback display name when no localization is available.
    /// </summary>
    public required string MemberName { get; init; }
}
