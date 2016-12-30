// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public struct FilterFactoryResult
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
}
