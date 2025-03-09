// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Microsoft.AspNetCore.Http.Validation;

/// <summary>
/// Provides an interface for resolving the validation information associated
/// with a given <seealso cref="Type"/> or <seealso cref="ParameterInfo"/>.
/// </summary>
public interface IValidatableInfoResolver
{
    /// <summary>
    /// Gets validation information for the specified type.
    /// </summary>
    /// <param name="type">The type to get validation information for.</param>
    /// <param name="validatableInfo">
    /// The output parameter that will contain the validatable information if found.
    /// </param>
    /// <returns><see langword="true" /> if the validatable type information was found; otherwise, false.</returns>
    bool TryGetValidatableTypeInfo(Type type, [NotNullWhen(true)] out IValidatableInfo? validatableInfo);

    /// <summary>
    /// Gets validation information for the specified parameter.
    /// </summary>
    /// <param name="parameterInfo">The parameter to get validation information for.</param>
    /// <param name="validatableInfo">The output parameter that will contain the validatable information if found.</param>
    /// <returns><see langword="true" /> if the validatable parameter information was found; otherwise, false.</returns>
    bool TryGetValidatableParameterInfo(ParameterInfo parameterInfo, [NotNullWhen(true)] out IValidatableInfo? validatableInfo);
}
