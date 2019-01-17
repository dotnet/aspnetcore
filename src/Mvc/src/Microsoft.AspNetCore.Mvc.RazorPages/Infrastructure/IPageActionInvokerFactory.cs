// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    internal interface IPageActionInvokerFactory
    {
        IActionInvoker CreateInvoker(
           ActionContext actionContext,
           PageActionInvokerCacheEntry cacheEntry,
           IFilterMetadata[] filters);
    }
}
