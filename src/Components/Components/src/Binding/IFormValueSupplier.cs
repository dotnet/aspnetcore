// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Binding;

/// <summary>
/// Binds form data values to a model.
/// </summary>
public interface IFormValueSupplier
{
    /// <summary>
    /// Determines whether the specified value type can be bound.
    /// </summary>
    /// <param name="valueType">The <see cref="Type"/> for the value to bind.</param>
    /// <param name="formName">The form name to bind data from or null to only validate the type can be bound.</param>
    /// <returns><c>true</c> if the value type can be bound; otherwise, <c>false</c>.</returns>
    bool CanBind(Type valueType, string? formName = null);

    /// <summary>
    /// Binds the form with the specified name to a value of the specified type.
    /// <param name="context">The <see cref="FormValueSupplierContext"/>.</param>
    /// </summary>
    void Bind(FormValueSupplierContext context);
}
