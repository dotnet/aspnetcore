// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents an estimate of the storage that the current origin uses and can use, returned by the Storage API.
/// </summary>
public sealed class StorageEstimate
{
    /// <summary>
    /// Gets the estimated number of bytes that the current origin uses.
    /// </summary>
    public ulong? Usage { get; init; }

    /// <summary>
    /// Gets the estimated number of bytes that the current origin can use.
    /// </summary>
    public ulong? Quota { get; init; }
}
