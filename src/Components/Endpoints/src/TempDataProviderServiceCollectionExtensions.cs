// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// Enables component parameters to be supplied from the <see cref="TempData"/>.
/// </summary>
public static class TempDataProviderServiceCollectionExtensions
{

    internal static IServiceCollection AddDefaultTempDataValueProvider(this IServiceCollection services)
    {
        services.TryAddSingleton<ITempDataProvider, CookieTempDataProvider>();
        services.TryAddSingleton<ITempDataSerializer, JsonTempDataSerializer>();
        services.TryAddSingleton<TempDataService>();

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

    /// <summary>
    /// Enables component parameters to be supplied from the <see cref="TempData"/>.
    /// </summary>
    public static IServiceCollection AddTempDataValueProvider(this IServiceCollection services)
    {
        // add services based on options

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
            var tempDataService = httpContext.RequestServices.GetRequiredService<TempDataService>();
            var tempDataInstance = tempDataService.CreateEmpty(httpContext);
            httpContext.Items[key] = tempDataInstance;
            httpContext.Response.OnStarting(() =>
            {
                tempDataService.Save(httpContext, tempDataInstance);
                return Task.CompletedTask;
            });
        }
        return (ITempData)httpContext.Items[key]!;
    }
}
