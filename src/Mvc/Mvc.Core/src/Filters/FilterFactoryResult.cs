// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Filters;

internal readonly struct FilterFactoryResult
{
    public FilterFactoryResult(
        FilterItem[] cacheableFilters,
        IFilterMetadata[] filters)
    {
        CacheableFilters = cacheableFilters;
        Filters = filters;
    }

    public FilterItem[] CacheableFilters { get; }

    public IFilterMetadata[] Filters { get; }
}
