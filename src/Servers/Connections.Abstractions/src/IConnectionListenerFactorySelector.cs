// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;

namespace Microsoft.AspNetCore.Connections;

/// <summary>
/// Defines an interface that determines whether the listener factory supports binding to the specified <see cref="EndPoint"/>.
/// </summary>
/// <remarks>
/// This interface should be implemented by <see cref="IConnectionListenerFactory"/> and <see cref="IMultiplexedConnectionListenerFactory"/>
/// types that want to control want endpoint instances they can bind to.
/// </remarks>
public interface IConnectionListenerFactorySelector
{
    /// <summary>
    /// Returns a value that indicates whether the listener factory supports binding to the specified <see cref="EndPoint"/>.
    /// </summary>
    /// <param name="endpoint">The <see cref="EndPoint" /> to bind to.</param>
    /// <returns>A value that indicates whether the listener factory supports binding to the specified <see cref="EndPoint"/>.</returns>
    bool CanBind(EndPoint endpoint);
}
