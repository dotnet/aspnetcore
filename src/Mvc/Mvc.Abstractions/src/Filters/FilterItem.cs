// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// Used to associate executable filters with <see cref="IFilterMetadata"/> instances
/// as part of <see cref="FilterProviderContext"/>. An <see cref="IFilterProvider"/> should
/// inspect <see cref="FilterProviderContext.Results"/> and set <see cref="Filter"/> and
/// <see cref="IsReusable"/> as appropriate.
/// </summary>
[DebuggerDisplay("Filter = {Filter}")]
public class FilterItem
{
    /// <summary>
    /// Creates a new <see cref="FilterItem"/>.
    /// </summary>
    /// <param name="descriptor">The <see cref="FilterDescriptor"/>.</param>
    public FilterItem(FilterDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        Descriptor = descriptor;
    }

    /// <summary>
    /// Creates a new <see cref="FilterItem"/>.
    /// </summary>
    /// <param name="descriptor">The <see cref="FilterDescriptor"/>.</param>
    /// <param name="filter"></param>
    public FilterItem(FilterDescriptor descriptor, IFilterMetadata filter)
        : this(descriptor)
    {
        ArgumentNullException.ThrowIfNull(filter);

        Filter = filter;
    }

    /// <summary>
    /// Gets the <see cref="FilterDescriptor"/> containing the filter metadata.
    /// </summary>
    public FilterDescriptor Descriptor { get; } = default!;

    /// <summary>
    /// Gets or sets the executable <see cref="IFilterMetadata"/> associated with <see cref="Descriptor"/>.
    /// </summary>
    public IFilterMetadata? Filter { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not <see cref="Filter"/> can be reused across requests.
    /// </summary>
    public bool IsReusable { get; set; }
}
