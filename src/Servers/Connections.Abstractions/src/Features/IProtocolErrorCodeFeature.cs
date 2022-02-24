// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Connections.Features;

/// <summary>
/// The error code for the protocol being used.
/// </summary>
public interface IProtocolErrorCodeFeature
{
    /// <summary>
    /// Gets or sets the error code. The property returns -1 if the error code hasn't been set.
    /// </summary>
    long Error { get; set; }
}
