// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Internal;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public static class PageFilterFactoryProvider
    {
        public static Func<ActionContext, IFilterMetadata[]> GetFilterFactory(
            IFilterProvider[] filterProviders,
            ActionInvokerProviderContext actionInvokerProviderContext)
        {
            if (filterProviders == null)
            {
                throw new ArgumentNullException(nameof(filterProviders));
            }

            if (actionInvokerProviderContext == null)
            {
                throw new ArgumentNullException(nameof(actionInvokerProviderContext));
            }

            var actionDescriptor = actionInvokerProviderContext.ActionContext.ActionDescriptor;

            // staticFilterItems is captured as part of the closure.We evaluate it once to determine
            // which of the staticFilters are reusable.
            var staticFilterItems = new FilterItem[actionDescriptor.FilterDescriptors.Count];
            for (var i = 0; i < actionDescriptor.FilterDescriptors.Count; i++)
            {
                staticFilterItems[i] = new FilterItem(actionDescriptor.FilterDescriptors[i]);
            }

            var internalFilterFactory = GetFilterFactory(filterProviders);
            var allFilterItems = new List<FilterItem>(staticFilterItems);

            // Execute the filter factory to determine which static filters can be cached.
            var filters = internalFilterFactory(allFilterItems, actionInvokerProviderContext.ActionContext);

            // Cache the filter items based on the following criteria
            // 1. Are created statically (ex: via filter attributes, added to global filter list etc.)
            // 2. Are re-usable
            for (var i = 0; i < staticFilterItems.Length; i++)
            {
                var item = staticFilterItems[i];
                if (!item.IsReusable)
                {
                    item.Filter = null;
                }
            }

            return (actionContext) =>
            {
                // Reuse the filters cached outside the closure for the very first run. This avoids re-running
                // filters twice the first time we cache for a page.
                var cachedFilters = Interlocked.Exchange(ref filters, null);
                if (cachedFilters != null)
                {
                    return cachedFilters;
                }

                // Create a separate collection as we want to hold onto the statically defined filter items
                // in order to cache them
                var filterItems = new List<FilterItem>(staticFilterItems.Length);
                for (var i = 0; i < staticFilterItems.Length; i++)
                {
                    // Deep copy the cached filter items as filter providers could modify them
                    var filterItem = staticFilterItems[i];
                    filterItems.Add(new FilterItem(filterItem.Descriptor)
                    {
                        Filter = filterItem.Filter,
                        IsReusable = filterItem.IsReusable
                    });
                }

                return internalFilterFactory(filterItems, actionContext);
            };
        }

        private static Func<IList<FilterItem>, ActionContext, IFilterMetadata[]> GetFilterFactory(
            IFilterProvider[] filterProviders)
        {
            return (filterItems, actionContext) =>
            {
                // Execute providers
                var filterContext = new FilterProviderContext(actionContext, filterItems);

                for (var i = 0; i < filterProviders.Length; i++)
                {
                    filterProviders[i].OnProvidersExecuting(filterContext);
                }

                for (var i = filterProviders.Length - 1; i >= 0; i--)
                {
                    filterProviders[i].OnProvidersExecuted(filterContext);
                }

                // Extract filter instances from statically defined filters and filter providers
                var count = 0;
                for (var i = 0; i < filterItems.Count; i++)
                {
                    if (filterItems[i].Filter != null)
                    {
                        count++;
                    }
                }

                if (count == 0)
                {
                    return EmptyArray<IFilterMetadata>.Instance;
                }
                else
                {
                    var filters = new IFilterMetadata[count];
                    var filterIndex = 0;
                    for (int i = 0; i < filterItems.Count; i++)
                    {
                        var filter = filterItems[i].Filter;
                        if (filter != null)
                        {
                            filters[filterIndex++] = filter;
                        }
                    }

                    return filters;
                }
            };
        }
    }
}
