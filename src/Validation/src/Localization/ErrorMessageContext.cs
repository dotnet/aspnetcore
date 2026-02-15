// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation.Localization;

/// <summary>
/// Provides context to the <see cref="ValidationOptions.ErrorMessageProvider"/> delegate
/// for resolving localized or customized error messages for validation attributes.
/// </summary>
public readonly struct ErrorMessageContext
{
    /// <summary>
    /// Gets the validation attribute that produced the error.
    /// </summary>
    public required ValidationAttribute Attribute { get; init; }

    /// <summary>
    /// Gets the current error message template on the attribute.
    /// This is the value of <see cref="ValidationAttribute.ErrorMessage"/> if explicitly set,
    /// or the attribute's built-in default English template if not set
    /// (e.g., <c>"The {0} field is required."</c> for <see cref="RequiredAttribute"/>).
    /// This value can be used as a localization lookup key.
    /// </summary>
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// Gets a value indicating whether <see cref="ValidationAttribute.ErrorMessage"/> was explicitly
    /// set on the attribute instance (i.e., the user wrote <c>[Required(ErrorMessage = "...")]</c>).
    /// When <see langword="false"/>, <see cref="ErrorMessage"/> contains the attribute's
    /// built-in default message template.
    /// </summary>
    public required bool IsCustomErrorMessage { get; init; }

    /// <summary>
    /// Gets the resolved display name for the member being validated.
    /// This value is already localized if <see cref="ValidationOptions.DisplayNameProvider"/>
    /// was configured and returned a non-null value.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Gets the CLR name of the member or parameter being validated.
    /// </summary>
    public required string MemberName { get; init; }

    /// <summary>
    /// Gets the type that declares the member being validated.
    /// <see langword="null"/> for top-level parameter validation.
    /// </summary>
    public Type? DeclaringType { get; init; }
}
