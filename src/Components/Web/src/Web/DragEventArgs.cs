// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Supplies information about an drag event that is being raised.
/// </summary>
public class DragEventArgs : MouseEventArgs
{
    /// <summary>
    /// The data that underlies a drag-and-drop operation, known as the drag data store.
    /// See <see cref="DataTransfer"/>.
    /// </summary>
    public DataTransfer DataTransfer { get; set; } = default!;
}
