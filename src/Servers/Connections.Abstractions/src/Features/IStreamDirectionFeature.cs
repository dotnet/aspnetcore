// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Connections.Features;

/// <summary>
/// The direction of a connection stream
/// </summary>
public interface IStreamDirectionFeature
{
    /// <summary>
    /// Gets whether or not the connection stream can be read.
    /// </summary>
    bool CanRead { get; }

    /// <summary>
    /// Gets whether or not the connection stream can be written.
    /// </summary>
    bool CanWrite { get; }
}
