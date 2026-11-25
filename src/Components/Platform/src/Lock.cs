// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents a lock that the Web Locks API granted, passed to the callback of
/// <see cref="LockManager.RequestAsync(string, Func{Lock?, Task})"/> while the lock is held.
/// </summary>
/// <remarks>
/// <c>System.Threading</c> also has a type named <c>Lock</c>, and SDK projects import that namespace by default.
/// Code that names this type explicitly can use its full name or a <see langword="using"/> alias.
/// A lambda such as <c>async lock => ...</c> infers the type and needs neither.
/// </remarks>
public sealed class Lock
{
    /// <summary>
    /// Gets the name of the lock.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the mode of the lock.
    /// </summary>
    public LockMode Mode { get; init; }
}
