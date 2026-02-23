// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Localization;

namespace Microsoft.Extensions.Validation.Localization;

/// <summary>
/// Options for configuring validation error message and display name localization.
/// </summary>
public sealed class ValidationLocalizationOptions
{
    /// <summary>
    /// Gets or sets the delegate that creates an <see cref="IStringLocalizer"/>
    /// for a given declaring type. The declaring type is the type that contains
    /// the property being validated, or the type itself in case of type-level validation attributes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see langword="null"/> (the default), <see cref="IStringLocalizerFactory.Create(Type)"/>
    /// is called with the declaring type, which follows the standard resource file naming convention
    /// (e.g., <c>Resources/Models.Customer.fr.resx</c> for type <c>Models.Customer</c>).
    /// </para>
    /// <para>
    /// Set this to use a shared resource file for all validation messages:
    /// </para>
    /// <example>
    /// <code>
    /// options.LocalizerProvider = (type, factory) =&gt;
    ///     factory.Create(typeof(SharedValidationMessages));
    /// </code>
    /// </example>
    /// </remarks>
    public Func<Type, IStringLocalizerFactory, IStringLocalizer>? LocalizerProvider { get; set; }

    /// <summary>
    /// Gets or sets the delegate that determines the localization lookup key for a
    /// validation attribute's error message. When <see langword="null"/> (the default),
    /// the key is the attribute's error message template itself
    /// (e.g., <c>"The {0} field is required."</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Set this to use semantic keys instead of template strings:
    /// </para>
    /// <example>
    /// <code>
    /// options.ErrorMessageKeySelector = context =&gt;
    ///     $"{context.Attribute.GetType().Name}_ValidationError";
    /// // This makes the localizer look up "RequiredAttribute_ValidationError"
    /// // instead of "The {0} field is required."
    /// </code>
    /// </example>
    /// <para>
    /// The delegate receives an <see cref="ErrorMessageProviderContext"/> which contains the
    /// attribute instance, the default error message template, the display name, and
    /// other contextual information. Return <see langword="null"/> to skip localization
    /// for the particular attribute.
    /// </para>
    /// </remarks>
    public ErrorMessageKeyProvider? ErrorMessageKeyProvider { get; set; }
}
