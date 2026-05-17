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

    /// <summary>
    /// Attempts to get validation information for a property declared on the specified type or any of its super-types.
    /// </summary>
    /// <param name="type">The type that declares or inherits the property.</param>
    /// <param name="propertyName">The property name to look up.</param>
    /// <param name="validatablePropertyInfo">When this method returns, contains the validation information for the property,
    /// if a property with the specified name was found; otherwise, <see langword="null" />.</param>
    /// <returns><see langword="true" /> if a validatable property with the specified name was found; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    /// Members declared on <paramref name="type"/> take precedence over members inherited from super-types,
    /// matching the order in which <see cref="ValidatableTypeInfo.ValidateAsync"/> visits members.
    /// </remarks>
    [Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
    public bool TryGetValidatablePropertyInfo(Type type, string propertyName, [NotNullWhen(true)] out ValidatablePropertyInfo? validatablePropertyInfo)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(propertyName);

        if (TryGetValidatableTypeInfo(type, out var info) && info is ValidatableTypeInfo typeInfo)
        {
            if (typeInfo.FindMember(propertyName) is { } localProperty)
            {
                validatablePropertyInfo = localProperty;
                return true;
            }

            foreach (var superType in typeInfo.SuperTypes)
            {
                if (TryGetValidatableTypeInfo(superType, out var superInfo)
                    && superInfo is ValidatableTypeInfo superTypeInfo
                    && superTypeInfo.FindMember(propertyName) is { } inheritedProperty)
                {
                    validatablePropertyInfo = inheritedProperty;
                    return true;
                }
            }
        }

        validatablePropertyInfo = null;
        return false;
    }
}
