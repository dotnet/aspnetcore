// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.DataProtection;

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// Enables component parameters to be supplied from the <see cref="TempData"/>.
/// </summary>
public static class TempDataProviderServiceCollectionExtensions
{

    internal static IServiceCollection AddTempData(this IServiceCollection services)
    {
        services.TryAddSingleton<ITempDataSerializer, JsonTempDataSerializer>();
        services.TryAddSingleton<ITempDataProvider>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<RazorComponentsServiceOptions>>();
            var serializer = serviceProvider.GetRequiredService<ITempDataSerializer>();
            var dataProtectionProvider = serviceProvider.GetRequiredService<IDataProtectionProvider>();
            var logger = serviceProvider.GetRequiredService<ILogger<CookieTempDataProvider>>();
            return options.Value.TempDataProviderType switch
            {
                TempDataProviderType.Cookie => new CookieTempDataProvider(dataProtectionProvider, options, serializer, logger),
                TempDataProviderType.SessionStorage => new SessionStorageTempDataProvider(serializer, serviceProvider.GetRequiredService<ILogger<SessionStorageTempDataProvider>>()),
                _ => throw new InvalidOperationException($"Unsupported TempDataProviderType: {options.Value.TempDataProviderType}"),
            };
        });
        services.TryAddSingleton<TempDataService>();
        services = AddTempDataCascadingValue(services);
        return services;
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
