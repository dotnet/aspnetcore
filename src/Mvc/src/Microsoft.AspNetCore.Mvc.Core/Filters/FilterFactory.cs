// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    internal static class FilterFactory
    {
        public static FilterFactoryResult GetAllFilters(
            IFilterProvider[] filterProviders,
            ActionContext actionContext)
        {
            if (filterProviders == null)
            {
                throw new ArgumentNullException(nameof(filterProviders));
            }

            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            var actionDescriptor = actionContext.ActionDescriptor;

            var staticFilterItems = new FilterItem[actionDescriptor.FilterDescriptors.Count];

            var orderedFilters = actionDescriptor.FilterDescriptors
                .OrderBy(
                    filter => filter,
                    FilterDescriptorOrderComparer.Comparer)
                .ToList();

            for (var i = 0; i < orderedFilters.Count; i++)
            {
                staticFilterItems[i] = new FilterItem(orderedFilters[i]);
            }

            var allFilterItems = new List<FilterItem>(staticFilterItems);

            // Execute the filter factory to determine which static filters can be cached.
            var filters = CreateUncachedFiltersCore(filterProviders, actionContext, allFilterItems);

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

            return new FilterFactoryResult(staticFilterItems, filters);
        }

        public static IFilterMetadata[] CreateUncachedFilters(
            IFilterProvider[] filterProviders,
            ActionContext actionContext,
            FilterItem[] cachedFilterItems)
        {
            if (filterProviders == null)
            {
                throw new ArgumentNullException(nameof(filterProviders));
            }

            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            if (cachedFilterItems == null)
            {
                throw new ArgumentNullException(nameof(cachedFilterItems));
            }

            // Deep copy the cached filter items as filter providers could modify them
            var filterItems = new List<FilterItem>(cachedFilterItems.Length);
            for (var i = 0; i < cachedFilterItems.Length; i++)
            {
                var filterItem = cachedFilterItems[i];
                filterItems.Add(
                    new FilterItem(filterItem.Descriptor)
                    {
                        Filter = filterItem.Filter,
                        IsReusable = filterItem.IsReusable
                    });
            }

            return CreateUncachedFiltersCore(filterProviders, actionContext, filterItems);
        }

        private static IFilterMetadata[] CreateUncachedFiltersCore(
            IFilterProvider[] filterProviders,
            ActionContext actionContext,
            List<FilterItem> filterItems)
        {
            // Execute providers
            var context = new FilterProviderContext(actionContext, filterItems);

            for (var i = 0; i < filterProviders.Length; i++)
            {
                filterProviders[i].OnProvidersExecuting(context);
            }

            for (var i = filterProviders.Length - 1; i >= 0; i--)
            {
                filterProviders[i].OnProvidersExecuted(context);
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
                return Array.Empty<IFilterMetadata>();
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
        }
    }
}
