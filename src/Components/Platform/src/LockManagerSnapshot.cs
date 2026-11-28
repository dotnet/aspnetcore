// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents the state of the locks of an origin, returned by the Web Locks API.
/// </summary>
public sealed class LockManagerSnapshot
{
    /// <summary>
    /// Gets the locks that are held, in the order they were granted.
    /// </summary>
    public ImmutableArray<LockInfo>? Held { get; init; }

    /// <summary>
    /// Gets the lock requests that are waiting, in the order they were made.
    /// </summary>
    public ImmutableArray<LockInfo>? Pending { get; init; }
}
