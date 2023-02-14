// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
internal static class PageActionEndpointDataSourceProvider
{
    public static PageActionEndpointDataSource GetOrCreateDataSource(IEndpointRouteBuilder endpoints)
    {
        var dataSource = endpoints.DataSources.OfType<PageActionEndpointDataSource>().FirstOrDefault();

        if (dataSource == null)
        {
            var orderProviderCache = endpoints.ServiceProvider.GetRequiredService<OrderedEndpointsSequenceProviderCache>();
            var factory = endpoints.ServiceProvider.GetRequiredService<PageActionEndpointDataSourceFactory>();
            dataSource = factory.Create(orderProviderCache.GetOrCreateOrderedEndpointsSequenceProvider(endpoints));
            endpoints.DataSources.Add(dataSource);
        }

        return dataSource;
    }
}
