// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Internal;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageActionInvoker : IActionInvoker
    {
        private readonly PageActionInvokerCacheEntry _cacheEntry;
        private readonly ActionContext _actionContext;

        public PageActionInvoker(
            PageActionInvokerCacheEntry cacheEntry,
            ActionContext actionContext)
        {
            _cacheEntry = cacheEntry;
            _actionContext = actionContext;
        }

        public Task InvokeAsync()
        {
            return TaskCache.CompletedTask;
        }
    }
}
