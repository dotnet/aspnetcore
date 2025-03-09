// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Microsoft.AspNetCore.Http.Validation;

/// <summary>
/// Provides configuration options for the validation system.
/// </summary>
public class ValidationOptions
{
    /// <summary>
    /// Gets the list of resolvers that provide validation metadata for types and parameters.
    /// Resolvers are processed in order, with the first resolver providing a non-null result being used.
    /// </summary>
    /// <remarks>
    /// Source-generated resolvers are typically inserted at the beginning of this list
    /// to ensure they are checked before any runtime-based resolvers.
    /// </remarks>
    public IList<IValidatableInfoResolver> Resolvers { get; } = [];

    /// <summary>
    /// Gets or sets the maximum depth for validation of nested objects.
    /// This prevents stack overflows from circular references or extremely deep object graphs.
    /// Default value is 32.
    /// </summary>
    public int MaxDepth { get; set; } = 32;

    /// <summary>
    /// Attempts to get validation information for the specified type.
    /// </summary>
    /// <param name="type">The type to get validation information for.</param>
    /// <param name="validatableTypeInfo">When this method returns, contains the validation information for the specified type,
    /// if the type was found; otherwise, null.</param>
    /// <returns>true if validation information was found for the specified type; otherwise, false.</returns>
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
    /// if validation information was found; otherwise, null.</param>
    /// <returns>true if validation information was found for the specified parameter; otherwise, false.</returns>
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
