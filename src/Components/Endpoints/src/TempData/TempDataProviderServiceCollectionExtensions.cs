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
        services = AddTempDataCascadingValue(services);
        return services;
    }

    /// <summary>
    /// Enables component parameters to be supplied from the <see cref="TempData"/> using Cookies.
    /// </summary>
    public static IRazorComponentsBuilder AddCookieTempDataValueProvider(
        this IRazorComponentsBuilder builder)
    {
        builder.Services.Replace(ServiceDescriptor.Singleton<ITempDataProvider, CookieTempDataProvider>());
        builder.Services.TryAddSingleton<ITempDataSerializer, JsonTempDataSerializer>();
        builder.Services.TryAddSingleton<TempDataService>();
        AddTempDataCascadingValue(builder.Services);
        return builder;
    }

    /// <summary>
    /// Enables component parameters to be supplied from the <see cref="TempData"/> using Session storage.
    /// </summary>
    public static IRazorComponentsBuilder AddSessionStorageTempDataValueProvider(
        this IRazorComponentsBuilder builder)
    {
        builder.Services.Replace(ServiceDescriptor.Singleton<ITempDataProvider, SessionStorageTempDataProvider>());
        builder.Services.TryAddSingleton<ITempDataSerializer, JsonTempDataSerializer>();
        builder.Services.TryAddSingleton<TempDataService>();
        AddTempDataCascadingValue(builder.Services);
        return builder;
    }

    private static IServiceCollection AddTempDataCascadingValue(IServiceCollection services)
    {
        services.TryAddCascadingValue(sp =>
        {
            var httpContext = sp.GetRequiredService<EndpointHtmlRenderer>().HttpContext;
            return httpContext is null
                ? null
                : GetOrCreateTempData(httpContext);
        });
        return services;
    }

    private static ITempData GetOrCreateTempData(HttpContext httpContext)
    {
        var key = typeof(ITempData);
        if (!httpContext.Items.ContainsKey(key))
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
