// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents the options of a lock request made with the Web Locks API.
/// </summary>
public sealed class LockOptions
{
    /// <summary>
    /// Gets or sets the mode of the requested lock. The default is <see cref="LockMode.Exclusive"/>.
    /// </summary>
    public LockMode Mode { get; set; } = LockMode.Exclusive;

    /// <summary>
    /// Gets or sets a value indicating whether the lock is granted only if it is available at once.
    /// If it is not, the callback is called with <see langword="null"/>. The default is <see langword="false"/>.
    /// </summary>
    public bool IfAvailable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the locks with the same name are released and this request is granted at once.
    /// The default is <see langword="false"/>.
    /// </summary>
    public bool Steal { get; set; }
}
