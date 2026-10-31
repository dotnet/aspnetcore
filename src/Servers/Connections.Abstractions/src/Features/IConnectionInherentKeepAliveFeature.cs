// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Connections.Features;

/// <summary>
/// Indicates if the connection transport has an "inherent keep-alive", which means that the transport will automatically
/// inform the client that it is still present.
/// </summary>
/// <remarks>
/// The most common example of this feature is the Long Polling HTTP transport, which must (due to HTTP limitations) terminate
/// each poll within a particular interval and return a signal indicating "the server is still here, but there is no data yet".
/// This feature allows applications to add keep-alive functionality, but limit it only to transports that don't have some kind
/// of inherent keep-alive.
/// </remarks>
public interface IConnectionInherentKeepAliveFeature
{
    /// <summary>
    /// Gets whether or not the connection has an inherent keep-alive concept.
    /// </summary>
    bool HasInherentKeepAlive { get; }
}
