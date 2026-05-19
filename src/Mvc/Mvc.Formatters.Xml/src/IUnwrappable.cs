// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml;

/// <summary>
/// Defines an interface for objects to be un-wrappable after deserialization.
/// </summary>
public interface IUnwrappable
{
    /// <summary>
    /// Unwraps an object.
    /// </summary>
    /// <param name="declaredType">The type to which the object should be un-wrapped to.</param>
    /// <returns>The un-wrapped object.</returns>
    object Unwrap(Type declaredType);
}
