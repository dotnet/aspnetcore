// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents a change to local or session storage made by another page of the same origin.
/// </summary>
public sealed class StorageEvent
{
    /// <summary>
    /// Gets the key of the changed item, or <see langword="null"/> if the storage was cleared.
    /// </summary>
    public string? Key { get; init; }

    /// <summary>
    /// Gets the previous value of the item, or <see langword="null"/> if the item was added or the storage was cleared.
    /// </summary>
    public string? OldValue { get; init; }

    /// <summary>
    /// Gets the new value of the item, or <see langword="null"/> if the item was removed or the storage was cleared.
    /// </summary>
    public string? NewValue { get; init; }

    /// <summary>
    /// Gets the URL of the page whose storage changed.
    /// </summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>
    /// Gets the storage that changed. It is the same instance as <c>Window.LocalStorage</c> or <c>Window.SessionStorage</c>.
    /// </summary>
    public Storage? StorageArea { get; init; }
}
