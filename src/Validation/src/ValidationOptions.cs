// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    /// Gets or sets a delegate that resolves the <see cref="IStringLocalizer"/> used to localize
    /// validation display names and error messages for a given declaring type.
    /// </summary>
    /// <remarks>
    /// The delegate receives the type used to resolve the localizer and the registered <see cref="IStringLocalizerFactory"/>.
    /// The default <see cref="IValidatableInfoResolver"/> sets the type argument to the declaring type for a property,
    /// the validated type itself for type-level validation, or the parameter's own type for a parameter.
    /// </remarks>
    public Func<Type, IStringLocalizerFactory, IStringLocalizer> LocalizerProvider { get; set; }
        = (type, factory) => factory.Create(type);

    /// <summary>
    /// Gets or sets a delegate that computes the resource key used to look up a localized validation
    /// message.
    /// </summary>
    /// <remarks>
    /// The provider supplies the lookup key by convention (for example, keyed by
    /// <see cref="ValidationMessageKeyContext.ValidatorType"/>) for validators that do not specify an
    /// explicit message. When a validator specifies an explicit message, that message is used as the
    /// lookup key and the provider is not consulted.
    /// </remarks>
    public Func<ValidationMessageKeyContext, string?>? MessageKeyProvider { get; set; }

    /// <summary>
    /// Attempts to get validation information for the specified type.
    /// </summary>
    /// <param name="type">The type to get validation information for.</param>
    /// <param name="validatableTypeInfo">When this method returns, contains the validation information for the specified type,
    /// if the type was found; otherwise, <see langword="null" />.</param>
    /// <returns><see langword="true" /> if validation information was found for the specified type; otherwise, <see langword="false" />.</returns>
    public bool TryGetValidatableTypeInfo(Type type, [NotNullWhen(true)] out IValidatableTypeInfo? validatableTypeInfo)
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
    public bool TryGetValidatableParameterInfo(ParameterInfo parameterInfo, [NotNullWhen(true)] out IValidatableParameterInfo? validatableInfo)
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
