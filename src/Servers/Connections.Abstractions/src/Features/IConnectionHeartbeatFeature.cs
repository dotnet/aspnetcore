// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Connections.Features;

/// <summary>
/// A feature that represents the connection heartbeat.
/// </summary>
public interface IConnectionHeartbeatFeature
{
    /// <summary>
    /// Registers the given <paramref name="action"/> to be called with the associated <paramref name="state"/> on each heartbeat of the connection.
    /// </summary>
    /// <param name="action">The <see cref="Action{T}"/> to invoke.</param>
    /// <param name="state">The state for the <paramref name="action"/>.</param>
    void OnHeartbeat(Action<object> action, object state);
}
