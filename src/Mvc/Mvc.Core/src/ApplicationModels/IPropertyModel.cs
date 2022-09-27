// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// An interface which is used to represent something with properties.
/// </summary>
public interface IPropertyModel
{
    /// <summary>
    /// The properties.
    /// </summary>
    IDictionary<object, object?> Properties { get; }
}
