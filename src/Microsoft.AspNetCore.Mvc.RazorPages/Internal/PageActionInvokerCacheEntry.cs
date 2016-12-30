// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageActionInvokerCacheEntry
    {
        public PageActionInvokerCacheEntry(
            CompiledPageActionDescriptor actionDescriptor,
            Func<PageContext, object> pageFactory,
            Action<PageContext, object> releasePage,
            FilterItem[] cacheableFilters)
        {
            ActionDescriptor = actionDescriptor;
            PageFactory = pageFactory;
            ReleasePage = releasePage;
            CacheableFilters = cacheableFilters;
        }

        public CompiledPageActionDescriptor ActionDescriptor { get; }

        public Func<PageContext, object> PageFactory { get; }

        /// <summary>
        /// The action invoked to release a page. This may be <c>null</c>.
        /// </summary>
        public Action<PageContext, object> ReleasePage { get; }

        public FilterItem[] CacheableFilters { get; }
    }
}
