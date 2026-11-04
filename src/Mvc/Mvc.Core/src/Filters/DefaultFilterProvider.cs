// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Core;

namespace Microsoft.AspNetCore.Mvc.Filters;

internal sealed class DefaultFilterProvider : IFilterProvider
{
    public int Order => -1000;

    /// <inheritdoc />
    public void OnProvidersExecuting(FilterProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

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

    public static void ProvideFilter(FilterProviderContext context, FilterItem filterItem)
    {
        if (filterItem.Filter != null)
        {
            return;
        }

        var filter = filterItem.Descriptor.Filter;

        if (filter is not IFilterFactory filterFactory)
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

    private static void ApplyFilterToContainer(object actualFilter, IFilterMetadata filterMetadata)
    {
        Debug.Assert(actualFilter != null, "actualFilter should not be null");
        Debug.Assert(filterMetadata != null, "filterMetadata should not be null");

        if (actualFilter is IFilterContainer container)
        {
            container.FilterDefinition = filterMetadata;
        }
    }
}
