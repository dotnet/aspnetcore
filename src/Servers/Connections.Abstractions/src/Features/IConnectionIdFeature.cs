// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Connections.Features;

/// <summary>
/// The unique identifier for a given connection.
/// </summary>
public interface IConnectionIdFeature
{
    /// <summary>
    /// Gets or sets the connection identifier.
    /// </summary>
    string ConnectionId { get; set; }
}
