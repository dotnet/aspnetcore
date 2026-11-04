// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;

namespace Microsoft.AspNetCore.Connections.Features;

/// <summary>
/// Provides access to the connection's underlying <see cref="Socket"/>.
/// </summary>
public interface IConnectionSocketFeature
{
    /// <summary>
    /// Gets the underlying <see cref="Socket"/>.
    /// </summary>
    Socket Socket { get; }
}
