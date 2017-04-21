// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Razor;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageActionInvokerCacheEntry
    {
        public PageActionInvokerCacheEntry(
            CompiledPageActionDescriptor actionDescriptor,
            Func<PageContext, object> pageFactory,
            Action<PageContext, object> releasePage,
            Func<PageContext, object> modelFactory,
            Action<PageContext, object> releaseModel,
            Func<Page, object, Task> propertyBinder,
            Func<object, object[], Task<IActionResult>>[] executors,
            IReadOnlyList<Func<IRazorPage>> viewStartFactories,
            FilterItem[] cacheableFilters)
        {
            ActionDescriptor = actionDescriptor;
            PageFactory = pageFactory;
            ReleasePage = releasePage;
            ModelFactory = modelFactory;
            ReleaseModel = releaseModel;
            PropertyBinder = propertyBinder;
            Executors = executors;
            ViewStartFactories = viewStartFactories;
            CacheableFilters = cacheableFilters;
        }

        public CompiledPageActionDescriptor ActionDescriptor { get; }

        public Func<PageContext, object> PageFactory { get; }

        /// <summary>
        /// The action invoked to release a page. This may be <c>null</c>.
        /// </summary>
        public Action<PageContext, object> ReleasePage { get; }

        public Func<PageContext, object> ModelFactory { get; }

        /// <summary>
        /// The delegate invoked to release a model. This may be <c>null</c>.
        /// </summary>
        public Action<PageContext, object> ReleaseModel { get; }

        /// <summary>
        /// The delegate invoked to bind either the handler type (page or model).
        /// This may be <c>null</c>.
        /// </summary>
        public Func<Page, object, Task> PropertyBinder { get; }

        public Func<object, object[], Task<IActionResult>>[] Executors { get; }

        /// <summary>
        /// Gets the applicable ViewStart pages.
        /// </summary>
        public IReadOnlyList<Func<IRazorPage>> ViewStartFactories { get; }

        public FilterItem[] CacheableFilters { get; }
    }
}
