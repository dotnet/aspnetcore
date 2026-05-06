// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Localization;

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
    /// <para>
    /// The delegate must return a non-null <see cref="IStringLocalizer"/>. Returning <see langword="null"/>
    /// causes an <see cref="InvalidOperationException"/> to be thrown the next time validation tries
    /// to localize a message for that declaring type. The result is cached per declaring type, so the
    /// delegate is invoked at most once per type per <see cref="ValidationLocalizer"/> instance.
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
    public Func<ErrorMessageKeyContext, string?>? ErrorMessageKeyProvider { get; set; }

    /// <summary>
    /// Gets the registry of formatters for attribute-specific error message template formatting.
    /// Built-in formatters for standard attributes are registered automatically.
    /// </summary>
    /// <remarks>
    /// The registry is intended to be configured during application startup (typically inside the
    /// <c>AddValidation</c> options callback or via <c>AddValidationAttributeFormatter&lt;TAttribute&gt;</c>).
    /// Mutating the registry after the validation pipeline has begun processing requests is not
    /// thread-safe and is not supported.
    /// </remarks>
    public ValidationAttributeFormatterRegistry AttributeFormatters { get; } = new();

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
