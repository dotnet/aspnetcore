// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Internal;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageActionInvoker : IActionInvoker
    {
        private readonly ActionContext _actionContext;
        private readonly IFilterMetadata[] _filters;

        public PageActionInvoker(
            PageActionInvokerCacheEntry cacheEntry,
            ActionContext actionContext,
            IFilterMetadata[] filters)
        {
            CacheEntry = cacheEntry;
            _actionContext = actionContext;
            _filters = filters;
        }

        public PageActionInvokerCacheEntry CacheEntry { get; }

        public Task InvokeAsync()
        {
            return TaskCache.CompletedTask;
        }
    }
}
