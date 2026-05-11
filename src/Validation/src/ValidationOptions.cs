// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

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
    /// Gets or sets the <see cref="IValidationLocalizer"/> used by the validation pipeline to
    /// resolve localized display names and error messages.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see langword="null"/> (the default), no localization is performed: literal display
    /// names from <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute.Name"/> and
    /// <see cref="System.ComponentModel.DisplayNameAttribute.DisplayName"/> are returned as-is,
    /// and validation error messages fall back to the attribute's default message.
    /// </para>
    /// <para>
    /// To enable the default <c>IStringLocalizer</c>-based implementation, add a reference to
    /// <c>Microsoft.Extensions.Validation.Localization</c> and call
    /// <c>services.AddValidationLocalization()</c> during DI configuration. Alternatively,
    /// assign a custom <see cref="IValidationLocalizer"/> implementation directly.
    /// </para>
    /// <para>
    /// This property is intended to be configured during application startup. Mutating it after
    /// the validation pipeline has begun processing requests is not thread-safe.
    /// </para>
    /// </remarks>
    public IValidationLocalizer? Localizer { get; set; }

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
