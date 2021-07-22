// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Connections;

/// <summary>
/// Options used to change behavior of how connections are handled.
/// </summary>
public class ConnectionOptions
{
    /// <summary>
    /// Gets or sets the interval used by the server to timeout idle connections.
    /// </summary>
    public TimeSpan? DisconnectTimeout { get; set; }

    /// <summary>
    /// Gets or sets the function to run after application shutdown is triggered and before connections are closed.
    /// </summary>
    /// <remarks>
    /// The Server or Host may forcefully close connections before this task completes.
    /// </remarks>
    public Func<Task>? ShutdownCallback { get; set; }
}
