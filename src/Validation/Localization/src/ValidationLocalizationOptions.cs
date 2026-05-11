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
    /// being validated.
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
    /// instance creation is expensive. The default factory registered by
    /// <c>AddLocalization()</c> (<c>ResourceManagerStringLocalizerFactory</c>) caches its
    /// results internally, so the default code path is cheap. If your delegate itself does
    /// meaningful work beyond calling the factory, capture the result in a closure to amortize
    /// that cost across calls.
    /// </para>
    /// <para>
    /// <b>Minimal API parameter validation note:</b> for top-level method parameters there is no
    /// declaring type, so the validation pipeline passes <c>typeof(object)</c> as the declaring type
    /// argument. With the default per-type lookup, this resolves to the <c>object</c> resource source
    /// (typically not what the user wants). To localize Minimal API parameter messages, configure a
    /// shared-resource <see cref="LocalizerProvider"/> that ignores the type argument:
    /// <c>options.LocalizerProvider = (_, factory) =&gt; factory.Create(typeof(SharedValidationMessages));</c>
    /// </para>
    /// </remarks>
    public Func<Type, IStringLocalizerFactory, IStringLocalizer>? LocalizerProvider { get; set; }

    /// <summary>
    /// Gets or sets the delegate that determines the localization lookup key for a
    /// validation attribute's error message. When <see langword="null"/> (the default),
    /// only attributes with <see cref="ValidationAttribute.ErrorMessage"/> set are localized
    /// (using the <see cref="ValidationAttribute.ErrorMessage"/> value as the key).
    /// </summary>
    /// <remarks>
    /// <para>
    /// When configured, this delegate is called as a fallback for attributes without an
    /// explicit <see cref="ValidationAttribute.ErrorMessage"/>, enabling convention-based
    /// key selection.
    /// </para>
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
