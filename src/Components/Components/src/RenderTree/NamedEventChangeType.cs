// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.RenderTree;

/// <summary>
/// Describes a change to a named event.
/// </summary>
public enum NamedEventChangeType : int
{
    /// <summary>
    /// Indicates that the item was added.
    /// </summary>
    Added,

    /// <summary>
    /// Indicates that the item was removed.
    /// </summary>
    Removed,
}
