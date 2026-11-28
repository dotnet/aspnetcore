// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents a held lock or a pending lock request, returned by the Web Locks API.
/// </summary>
public sealed class LockInfo
{
    /// <summary>
    /// Gets the name of the lock.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the mode of the lock.
    /// </summary>
    public LockMode? Mode { get; init; }

    /// <summary>
    /// Gets the identifier of the tab, window, or worker that holds or requested the lock.
    /// </summary>
    public string? ClientId { get; init; }
}
