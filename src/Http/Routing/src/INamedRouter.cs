// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// An interface for an <see cref="IRouter"/> with a name.
/// </summary>
public interface INamedRouter : IRouter
{
    /// <summary>
    /// The name of the router. Can be null.
    /// </summary>
    string? Name { get; }
}
