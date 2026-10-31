// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Connections;

/// <summary>
/// Defines an interface that provides the mechanisms to configure a connection pipeline.
/// </summary>
public interface IConnectionBuilder
{
    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> that provides access to the application's service container.
    /// </summary>
    IServiceProvider ApplicationServices { get; }

    /// <summary>
    /// Adds a middleware delegate to the application's connection pipeline.
    /// </summary>
    /// <param name="middleware">The middleware delegate.</param>
    /// <returns>The <see cref="IConnectionBuilder"/>.</returns>
    IConnectionBuilder Use(Func<ConnectionDelegate, ConnectionDelegate> middleware);

    /// <summary>
    /// Builds the delegate used by this application to process connections.
    /// </summary>
    /// <returns>The connection handling delegate.</returns>
    ConnectionDelegate Build();
}
