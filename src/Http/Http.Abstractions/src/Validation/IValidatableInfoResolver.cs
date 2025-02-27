// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Http.Validation;

/// <summary>
/// Resolves validation type information for a given type.
/// </summary>
public interface IValidatableInfoResolver
{
    /// <summary>
    /// Gets validation type information for the specified type.
    /// </summary>
    /// <param name="type">The type to get validation information for.</param>
    /// <returns>The validation type information, or null if the type is not validatable.</returns>
    ValidatableTypeInfo? GetValidatableTypeInfo(Type type);

    /// <summary>
    /// Gets validation parameter information for the specified parameter.
    /// </summary>
    /// <param name="parameterInfo">The parameter information to get validation for.</param>
    /// <returns>The validation parameter information, or null if the parameter is not validatable.</returns>
    ValidatableParameterInfo? GetValidatableParameterInfo(ParameterInfo parameterInfo);
}
