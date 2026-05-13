// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Provides localization services for the validation pipeline. Implementations resolve
/// display names (used as <c>{0}</c> placeholder substitutions in error messages) and
/// localized error message text for <see cref="System.ComponentModel.DataAnnotations.ValidationAttribute"/>
/// instances. Set an instance on <see cref="ValidationOptions.Localizer"/> to enable localization.
/// </summary>
/// <remarks>
/// <para>
/// The default implementation that integrates with <c>Microsoft.Extensions.Localization.IStringLocalizer</c>
/// is provided by the <c>Microsoft.Extensions.Validation.Localization</c> package and is wired
/// up by calling <c>AddValidationLocalization()</c>. Custom implementations can be supplied
/// directly via <see cref="ValidationOptions.Localizer"/>.
/// </para>
/// <para>
/// Implementations may be invoked outside the validation pipeline (for example, during
/// server-side rendering of client-side validation rules), so they must not depend on
/// per-validation state and must be safe to call concurrently.
/// </para>
/// </remarks>
public interface IValidationLocalizer
{
    /// <summary>
    /// Resolves a localized display name for the member described by <paramref name="context"/>.
    /// </summary>
    /// <remarks>
    /// Implementations should return <see langword="null"/> to indicate that no localization
    /// is available; the validation pipeline falls back to <see cref="DisplayNameLocalizationContext.MemberName"/>.
    /// When <see cref="DisplayNameLocalizationContext.DisplayName"/> is <see langword="null"/>,
    /// implementations typically return <see langword="null"/> as well.
    /// </remarks>
    /// <param name="context">Information about the member to resolve a display name for.</param>
    /// <returns>The localized display name, or <see langword="null"/> if not available.</returns>
    string? ResolveDisplayName(in DisplayNameLocalizationContext context);

    /// <summary>
    /// Resolves a fully-formatted localized error message for the validation attribute described
    /// by <paramref name="context"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementations should return <see langword="null"/> when no localized message is available;
    /// the validation pipeline falls back to the attribute's default error message.
    /// </para>
    /// </remarks>
    /// <param name="context">Information about the validation attribute and the member it applied to.</param>
    /// <returns>The fully-formatted localized error message, or <see langword="null"/> to use the
    /// attribute's default message.</returns>
    string? ResolveErrorMessage(in ErrorMessageLocalizationContext context);
}
