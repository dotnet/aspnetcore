// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.QuickGrid;

/// <summary>
/// Specifies the query parameter names that a <see cref="QuickGrid{TGridItem}"/> uses to persist its
/// sort column, sort direction, and page index in the URL.
/// </summary>
public sealed class QueryParameterNameOptions(string prefix = "")
{
    /// <summary>
    /// Gets or sets the query parameter name used to persist the sort column. Defaults to <c>"{prefix}sort"</c>.
    /// </summary>
    public string Sort { get; set; } = $"{prefix}sort";

    /// <summary>
    /// Gets or sets the query parameter name used to persist the sort direction. Defaults to <c>"{prefix}direction"</c>.
    /// </summary>
    public string Direction { get; set; } = $"{prefix}direction";

    /// <summary>
    /// Gets or sets the query parameter name used to persist the page index. Defaults to <c>"{prefix}page"</c>.
    /// </summary>
    public string Page { get; set; } = $"{prefix}page";
}
