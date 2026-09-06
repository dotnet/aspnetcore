// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Represents a table.
/// </summary>
public class TableNode : RichTextNode
{
    /// <summary>
    /// Gets or sets each column's alignment.
    /// </summary>
    public IReadOnlyList<TableColumnAlignment> Alignment { get; set; } =
        Array.Empty<TableColumnAlignment>();
}
