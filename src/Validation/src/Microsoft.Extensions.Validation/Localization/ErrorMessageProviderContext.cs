// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation.Localization;

/// <summary>
/// Provides context to the <see cref="ValidationOptions.ErrorMessageProvider"/> delegate
/// for resolving localized or customized error messages for validation attributes.
/// </summary>
public readonly struct ErrorMessageProviderContext
{
    /// <summary>
    /// Gets the validation attribute that produced the error.
    /// </summary>
    public required ValidationAttribute Attribute { get; init; }

    /// <summary>
    /// Gets the resolved display name for the member being validated.
    /// This value is already localized if <see cref="ValidationOptions.DisplayNameProvider"/>
    /// was configured and returned a non-null value.
    /// </summary>
    public required string? DisplayName { get; init; }

    /// <summary>
    /// Gets the CLR name of the member or parameter being validated.
    /// </summary>
    public required string MemberName { get; init; }

    /// <summary>
    /// Gets the type that declares the member being validated.
    /// <see langword="null"/> for top-level parameter validation.
    /// </summary>
    public required Type? DeclaringType { get; init; }

    /// <summary>
    /// Gets the service provider for resolving localization or other services.
    /// </summary>
    public required IServiceProvider Services { get; init; }
}
