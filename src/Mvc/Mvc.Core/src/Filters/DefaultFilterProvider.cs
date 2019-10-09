// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Core;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    internal class DefaultFilterProvider : IFilterProvider
    {
        public int Order => -1000;

        /// <inheritdoc />
        public void OnProvidersExecuting(FilterProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.ActionContext.ActionDescriptor.FilterDescriptors != null)
            {
                var results = context.Results;
                // Perf: Avoid allocating enumerator and read interface .Count once rather than per iteration
                var resultsCount = results.Count;
                for (var i = 0; i < resultsCount; i++)
                {
                    ProvideFilter(context, results[i]);
                }
            }
        }

        /// <inheritdoc />
        public void OnProvidersExecuted(FilterProviderContext context)
        {
        }

        public void ProvideFilter(FilterProviderContext context, FilterItem filterItem)
        {
            if (filterItem.Filter != null)
            {
                return;
            }

            var filter = filterItem.Descriptor.Filter;

            if (!(filter is IFilterFactory filterFactory))
            {
                filterItem.Filter = filter;
                filterItem.IsReusable = true;
            }
            else
            {
                var services = context.ActionContext.HttpContext.RequestServices;
                filterItem.Filter = filterFactory.CreateInstance(services);
                filterItem.IsReusable = filterFactory.IsReusable;

                if (filterItem.Filter == null)
                {
                    throw new InvalidOperationException(Resources.FormatTypeMethodMustReturnNotNullValue(
                        "CreateInstance",
                        typeof(IFilterFactory).Name));
                }

                ApplyFilterToContainer(filterItem.Filter, filterFactory);
            }
        }

        private void ApplyFilterToContainer(object actualFilter, IFilterMetadata filterMetadata)
        {
            Debug.Assert(actualFilter != null, "actualFilter should not be null");
            Debug.Assert(filterMetadata != null, "filterMetadata should not be null");

            if (actualFilter is IFilterContainer container)
            {
                container.FilterDefinition = filterMetadata;
            }
        }
    }
}
