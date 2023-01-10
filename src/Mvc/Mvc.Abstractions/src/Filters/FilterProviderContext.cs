// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// A context for filter providers i.e. <see cref="IFilterProvider"/> implementations.
/// </summary>
public class FilterProviderContext
{
    /// <summary>
    /// Instantiates a new <see cref="FilterProviderContext"/> instance.
    /// </summary>
    /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
    /// <param name="items">
    /// The <see cref="FilterItem"/>s, initially created from <see cref="FilterDescriptor"/>s or a cache entry.
    /// </param>
    public FilterProviderContext(ActionContext actionContext, IList<FilterItem> items)
    {
        ArgumentNullException.ThrowIfNull(actionContext);
        ArgumentNullException.ThrowIfNull(items);

        ActionContext = actionContext;
        Results = items;
    }

    /// <summary>
    /// Gets or sets the <see cref="ActionContext"/>.
    /// </summary>
    public ActionContext ActionContext { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="FilterItem"/>s, initially created from <see cref="FilterDescriptor"/>s or a
    /// cache entry. <see cref="IFilterProvider"/>s should set <see cref="FilterItem.Filter"/> on existing items or
    /// add new <see cref="FilterItem"/>s to make executable filters available.
    /// </summary>
    public IList<FilterItem> Results { get; set; }
}
