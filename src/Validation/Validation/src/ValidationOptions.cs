// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Validation.Localization;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Specifies configuration options for the validation system.
/// </summary>
public class ValidationOptions
{
    /// <summary>
    /// Gets the list of resolvers that provide validation metadata for types and parameters.
    /// Resolvers are processed in order, with the first resolver that provides a non-null result being used.
    /// </summary>
    /// <remarks>
    /// Source-generated resolvers are typically inserted at the beginning of this list
    /// to ensure they are checked before any runtime-based resolvers.
    /// </remarks>
    [Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
    public IList<IValidatableInfoResolver> Resolvers { get; } = [];

    /// <summary>
    /// Gets or sets the maximum depth for validation of nested objects.
    /// </summary>
    /// <value>
    /// The default is 32.
    /// </value>
    /// <remarks>
    /// A maximum depth prevents stack overflows from circular references or extremely deep object graphs.
    /// </remarks>
    public int MaxDepth { get; set; } = 32;

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
    public Func<ErrorMessageKeyContext, string?>? ErrorMessageKeyProvider { get; set; }

    /// <summary>
    /// Gets the registry of formatters for attribute-specific error message template formatting.
    /// Built-in formatters for standard attributes are registered automatically.
    /// </summary>
    public ValidationAttributeFormatterRegistry AttributeFormatters { get; } = new();

    /// <summary>
    /// The IStringLocalizerFactory discovered from DI, set by <see cref="ValidationLocalizationAutoSetup"/>.
    /// Used to lazily create <see cref="LocalizationContext"/> on first access.
    /// </summary>
    internal IStringLocalizerFactory? StringLocalizerFactory { get; set; }

    private ValidationLocalizationContext? _localizationContext;

    /// <summary>
    /// The localization context, created lazily on first access from <see cref="StringLocalizerFactory"/>
    /// and the current values of <see cref="LocalizerProvider"/>, <see cref="ErrorMessageKeyProvider"/>,
    /// and <see cref="AttributeFormatters"/>. Lazy creation ensures that all IPostConfigureOptions
    /// callbacks have run before the delegates are captured.
    /// </summary>
    internal ValidationLocalizationContext? LocalizationContext =>
        _localizationContext ??= StringLocalizerFactory is not null
            ? new ValidationLocalizationContext(StringLocalizerFactory, LocalizerProvider, ErrorMessageKeyProvider, AttributeFormatters)
            : null;

    // TODO: Consider the design further - these methods expose localization resolution
    // for external consumers (e.g., client-side validation attribute rendering) without
    // leaking internal types. Evaluate whether a separate public interface/service would be better.

    /// <summary>
    /// Resolves a localized display name using the configured localization pipeline.
    /// Returns the original <paramref name="displayName"/> if localization is not configured
    /// or no localized value is found.
    /// </summary>
    /// <param name="displayName">The display name to localize (typically from <see cref="DisplayAttribute.Name"/>).</param>
    /// <param name="declaringType">The type that declares the member, or <see langword="null"/> for parameters.</param>
    /// <returns>The localized display name, or the original value if not found.</returns>
    public string ResolveDisplayName(string displayName, Type? declaringType)
        => LocalizationContext?.ResolveDisplayName(displayName, declaringType) ?? displayName;

    /// <summary>
    /// Resolves a localized, fully formatted error message for a validation attribute.
    /// Returns <see langword="null"/> if localization is not configured, the attribute uses
    /// its own resource-based localization, or no localized value is found.
    /// </summary>
    /// <param name="attribute">The validation attribute that produced the error.</param>
    /// <param name="memberName">The name of the property or parameter being validated.</param>
    /// <param name="displayName">The (possibly localized) display name of the member.</param>
    /// <param name="declaringType">The type that declares the member, or <see langword="null"/> for parameters.</param>
    /// <returns>The localized error message, or <see langword="null"/> to use the attribute's default message.</returns>
    public string? FormatErrorMessage(ValidationAttribute attribute, string memberName, string displayName, Type? declaringType)
    {
        if (attribute.ErrorMessageResourceType is not null)
        {
            return null;
        }

        return LocalizationContext?.ResolveErrorMessage(attribute, memberName, displayName, declaringType);
    }

    /// <summary>
    /// Attempts to get validation information for the specified type.
    /// </summary>
    /// <param name="type">The type to get validation information for.</param>
    /// <param name="validatableTypeInfo">When this method returns, contains the validation information for the specified type,
    /// if the type was found; otherwise, <see langword="null" />.</param>
    /// <returns><see langword="true" /> if validation information was found for the specified type; otherwise, <see langword="false" />.</returns>
    [Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
    public bool TryGetValidatableTypeInfo(Type type, [NotNullWhen(true)] out IValidatableInfo? validatableTypeInfo)
    {
        foreach (var resolver in Resolvers)
        {
            if (resolver.TryGetValidatableTypeInfo(type, out validatableTypeInfo))
            {
                return true;
            }
        }

        validatableTypeInfo = null;
        return false;
    }

    /// <summary>
    /// Attempts to get validation information for the specified parameter.
    /// </summary>
    /// <param name="parameterInfo">The parameter to get validation information for.</param>
    /// <param name="validatableInfo">When this method returns, contains the validation information for the specified parameter,
    /// if validation information was found; otherwise, <see langword="null" />.</param>
    /// <returns><see langword="true" /> if validation information was found for the specified parameter; otherwise, <see langword="false" />.</returns>
    [Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
    public bool TryGetValidatableParameterInfo(ParameterInfo parameterInfo, [NotNullWhen(true)] out IValidatableInfo? validatableInfo)
    {
        foreach (var resolver in Resolvers)
        {
            if (resolver.TryGetValidatableParameterInfo(parameterInfo, out validatableInfo))
            {
                return true;
            }
        }

        validatableInfo = null;
        return false;
    }
}
