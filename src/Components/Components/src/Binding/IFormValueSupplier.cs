// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Binding;

/// <summary>
/// Binds form data valuesto a model.
/// </summary>
public interface IFormValueSupplier
{
    /// <summary>
    /// Determines whether the specified value type can be bound.
    /// </summary>
    /// <param name="formName">The form name to bind data from.</param>
    /// <param name="valueType">The <see cref="Type"/> for the value to bind.</param>
    /// <returns><c>true</c> if the value type can be bound; otherwise, <c>false</c>.</returns>
    bool CanBind(string formName, Type valueType);

    /// <summary>
    /// Tries to bind the form with the specified name to a value of the specified type.
    /// </summary>
    /// <param name="formName">The form name to bind data from.</param>
    /// <param name="valueType">The <see cref="Type"/> for the value to bind.</param>
    /// <param name="boundValue">The bound value if succeeded.</param>
    /// <returns><c>true</c> if the form was bound successfully; otherwise, <c>false</c>.</returns>
    bool TryBind(string formName, Type valueType, [NotNullWhen(true)] out object? boundValue);
}
