// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;

namespace Microsoft.AspNetCore.Connections.Features;

/// <summary>
/// A feature that represents a connection endpoints.
/// </summary>
public interface IConnectionEndPointFeature
{
    /// <summary>
    /// Gets or sets the local <see cref="EndPoint"/>.
    /// </summary>
    EndPoint? LocalEndPoint { get; set; }

    /// <summary>
    /// Gets or sets the remote <see cref="EndPoint"/>.
    /// </summary>
    EndPoint? RemoteEndPoint { get; set; }
}
