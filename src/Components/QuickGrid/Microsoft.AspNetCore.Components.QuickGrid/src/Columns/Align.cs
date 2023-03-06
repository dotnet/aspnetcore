// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.QuickGrid;

/// <summary>
/// Describes alignment for a <see cref="QuickGrid{TGridItem}"/> column.
/// </summary>
public enum Align
{
    /// <summary>
    /// Justifies the content against the start of the container.
    /// </summary>
    Start,

    /// <summary>
    /// Justifies the content at the center of the container.
    /// </summary>
    Center,

    /// <summary>
    /// Justifies the content at the end of the container.
    /// </summary>
    End,

    /// <summary>
    /// Justifies the content against the left of the container.
    /// </summary>
    Left,

    /// <summary>
    /// Justifies the content at the right of the container.
    /// </summary>
    Right,
}
