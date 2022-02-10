// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Connections.Features;

/// <summary>
/// Represents the identifier for the stream.
/// </summary>
public interface IStreamIdFeature
{
    /// <summary>
    /// Gets the stream identifier.
    /// </summary>
    long StreamId { get; }
}
