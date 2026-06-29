// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Localization;

namespace Microsoft.Extensions.Validation.Localization;

/// <summary>
/// Configuration options for the <c>Microsoft.Extensions.Validation.Localization</c> default
/// <see cref="IStringLocalizer"/>-based validation localizer. Configure via
/// <c>services.AddValidationLocalization(options =&gt; ...)</c>.
/// </summary>
public class ValidationLocalizationOptions
{
    /// <summary>
    /// Gets or sets the delegate that controls which <see cref="IStringLocalizer"/> is used
    /// for a given declaring type. The declaring type is the type that contains the property
    /// being validated, or <see langword="null"/> when the validation pipeline has no declaring
    /// type to attribute the message to (e.g., top-level Minimal API parameters).
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see langword="null"/> (the default), <see cref="IStringLocalizerFactory.Create(Type)"/>
    /// is called with the declaring type, which follows the standard resource file naming convention
    /// (e.g., <c>Resources/Models.Customer.fr.resx</c> for type <c>Models.Customer</c>).
    /// For declaring-type-less invocations, the no-provider fallback uses <c>typeof(object)</c>.
    /// In that case the resolved resource source is rarely useful, which is why configuring
    /// this delegate (or using <c>AddValidationLocalization&lt;TResource&gt;()</c>)
    /// is recommended for Minimal API parameter validation.
    /// </para>
    /// <example>
    /// <code>
    /// options.LocalizerProvider = (type, factory) =&gt;
    ///     factory.Create(typeof(SharedValidationMessages));
    /// </code>
    /// </example>
    /// <para>
    /// For the shared-resource pattern above, prefer the dedicated
    /// <c>AddValidationLocalization&lt;TResource&gt;()</c> overload, which configures
    /// <see cref="LocalizerProvider"/> for you (with a single-instance optimization that
    /// skips re-resolving the same <see cref="IStringLocalizer"/> on every validation call).
    /// </para>
    /// <para>
    /// The delegate must return a non-null <see cref="IStringLocalizer"/>. Returning <see langword="null"/>
    /// causes an <see cref="InvalidOperationException"/> to be thrown the next time validation tries
    /// to localize a message for that declaring type.
    /// </para>
    /// <para>
    /// <b>Caching:</b> the validation pipeline does not cache the <see cref="IStringLocalizer"/>
    /// returned by this delegate. The delegate is invoked once per call to
    /// <see cref="IValidationLocalizer.ResolveDisplayName"/> and
    /// <see cref="IValidationLocalizer.ResolveErrorMessage"/>, so the underlying
    /// <see cref="IStringLocalizerFactory"/> is responsible for caching localizer instances if
    /// instance creation is expensive. However, the default factory registered by
    /// <c>AddLocalization()</c> (<c>ResourceManagerStringLocalizerFactory</c>) caches its
    /// results internally. If your delegate itself does
    /// meaningful work beyond calling the factory, capture the result in a closure to amortize
    /// that cost across calls.
    /// </para>
    /// </remarks>
    public Func<Type?, IStringLocalizerFactory, IStringLocalizer>? LocalizerProvider { get; set; }

    /// <summary>
    /// Gets or sets the delegate that determines the localization lookup key for a
    /// validation attribute's error message. When configured, the delegate is invoked for
    /// every attribute and takes precedence over <see cref="ValidationAttribute.ErrorMessage"/>.
    /// Returning <see langword="null"/> or an empty string defers to using
    /// <see cref="ValidationAttribute.ErrorMessage"/> directly as the lookup key.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see langword="null"/> (the default), only attributes with
    /// <see cref="ValidationAttribute.ErrorMessage"/> set are localized (using the
    /// <see cref="ValidationAttribute.ErrorMessage"/> value as the key); attributes without
    /// an explicit <see cref="ValidationAttribute.ErrorMessage"/> are not localized at all.
    /// </para>
    /// <para>
    /// When configured, the delegate can:
    /// </para>
    /// <list type="bullet">
    ///   <item>Provide a fallback key for attributes without an explicit
    ///   <see cref="ValidationAttribute.ErrorMessage"/> (convention-based key selection).</item>
    ///   <item>Transform an existing <see cref="ValidationAttribute.ErrorMessage"/> into a
    ///   different lookup key by reading
    ///   <see cref="ErrorMessageLocalizationContext.Attribute"/>'s
    ///   <see cref="ValidationAttribute.ErrorMessage"/> from the context.</item>
    ///   <item>Return <see langword="null"/> or an empty string to defer to the default
    ///   behavior of using <see cref="ValidationAttribute.ErrorMessage"/> as the key.</item>
    /// </list>
    /// <example>
    /// <code>
    /// options.ErrorMessageKeyProvider = context =&gt;
    ///     $"{context.Attribute.GetType().Name}_ValidationError";
    /// // This makes the localizer look up "RequiredAttribute_ValidationError"
    /// // instead of "The {0} field is required."
    /// </code>
    /// </example>
    /// </remarks>
    public Func<ErrorMessageLocalizationContext, string?>? ErrorMessageKeyProvider { get; set; }

    /// <summary>
    /// Gets the registry of formatters for attribute-specific error message template formatting.
    /// Built-in formatters for standard attributes are registered automatically.
    /// </summary>
    /// <remarks>
    /// The registry is intended to be configured during application startup (typically inside the
    /// <c>AddValidationLocalization</c> options callback) by calling
    /// <see cref="ValidationAttributeFormatterRegistry.AddFormatter{TAttribute}(Func{TAttribute, IValidationAttributeFormatter})"/>.
    /// Mutating the registry after the validation pipeline has begun processing requests is not
    /// thread-safe and is not supported.
    /// </remarks>
    public ValidationAttributeFormatterRegistry AttributeFormatters { get; } = new();
}
