// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Connections.Features;

/// <summary>
/// Represents the completion action for a connection.
/// </summary>
public interface IConnectionCompleteFeature
{
    /// <summary>
    /// Registers a callback to be invoked after a connection has fully completed processing. This is
    /// intended for resource cleanup.
    /// </summary>
    /// <param name="callback">The callback to invoke after the connection has completed processing.</param>
    /// <param name="state">The state to pass into the callback.</param>
    void OnCompleted(Func<object, Task> callback, object state);
}
