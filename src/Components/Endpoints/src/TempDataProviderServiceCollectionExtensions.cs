// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// Enables component parameters to be supplied from the <see cref="TempData"/> string with <see cref="SupplyParameterFromQueryAttribute"/>.
/// </summary>
public static class TempDataProviderServiceCollectionExtensions
{
    public static IServiceCollection AddTempDataValueProvider(this IServiceCollection services)
    {
        services.TryAddCascadingValue(sp =>
        {
            var httpContext = sp.GetRequiredService<EndpointHtmlRenderer>().HttpContext;
            if (httpContext is null)
            {
                return null!;
            }
            return GetOrCreateTempData(httpContext);
        });
        return services;
    }

    private static ITempData GetOrCreateTempData(HttpContext httpContext)
    {
        var key = typeof(ITempData);
        if (!httpContext.Items.TryGetValue(key, out var tempData))
        {
            var tempDataInstance = TempDataService.Load(httpContext);
            httpContext.Items[key] = tempDataInstance;
            httpContext.Response.OnStarting(() =>
            {
                TempDataService.Save(httpContext, tempDataInstance);
                return Task.CompletedTask;
            });
        }
        return (ITempData)httpContext.Items[key]!;
    }
}
