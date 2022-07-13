// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Additional options for navigating to another URI.
/// </summary>
public readonly struct NavigationOptions
{
    /// <summary>
    /// If true, bypasses client-side routing and forces the browser to load the new page from the server, whether or not the URI would normally be handled by the client-side router.
    /// </summary>
    public bool ForceLoad { get; init; }

    /// <summary>
    /// If true, replaces the currently entry in the history stack.
    /// If false, appends the new entry to the history stack.
    /// </summary>
    public bool ReplaceHistoryEntry { get; init; }

    /// <summary>
    /// Gets or sets the state to append to the history entry.
    /// </summary>
    public string? HistoryEntryState { get; init; }
}
