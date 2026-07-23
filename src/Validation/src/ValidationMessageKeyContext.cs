// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Provides contextual information used to compute the resource key for looking up a localized
/// validation error message.
/// </summary>
/// <remarks>
/// An instance is passed to <see cref="ValidationOptions.MessageKeyProvider"/> when a custom key
/// convention is configured. The returned key is then resolved against the configured
/// <see cref="Microsoft.Extensions.Localization.IStringLocalizer"/>.
/// </remarks>
public sealed class ValidationMessageKeyContext
{
    /// <summary>
    /// Gets the type of the validator that produced the error being localized. For DataAnnotations,
    /// this is the validation attribute type (for example, <c>typeof(RequiredAttribute)</c>).
    /// </summary>
    public required Type ValidatorType { get; init; }

    /// <summary>
    /// Gets the name of the member being validated: the property name for property validation, the
    /// parameter name for parameter validation, or the type name for type-level validation.
    /// </summary>
    public required string MemberName { get; init; }

    /// <summary>
    /// Gets the type that declares the member being validated (the containing type for a property, or
    /// the validated type itself for type-level validation), or <see langword="null"/> when there is
    /// no declaring type (for example, a top-level minimal API parameter).
    /// </summary>
    public Type? DeclaringType { get; init; }
}
