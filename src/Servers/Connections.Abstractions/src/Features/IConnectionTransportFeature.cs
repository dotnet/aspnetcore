// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Connections.Features;

/// <summary>
/// The transport for the connection.
/// </summary>
public interface IConnectionTransportFeature
{
    /// <summary>
    /// Gets or sets the transport for the connection.
    /// </summary>
    IDuplexPipe Transport { get; set; }
}
