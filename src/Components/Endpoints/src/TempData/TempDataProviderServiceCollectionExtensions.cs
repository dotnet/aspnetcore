// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.DataProtection;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal static class TempDataProviderServiceCollectionExtensions
{
    internal static readonly object HttpContextItemKey = new object();

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

    internal static ITempData GetOrCreateTempData(HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue(HttpContextItemKey, out var tempDataObj) && tempDataObj is ITempData tempData)
        {
            return tempData;
        }

        var tempDataService = httpContext.RequestServices.GetRequiredService<TempDataService>();
        var tempDataInstance = tempDataService.CreateEmpty(httpContext);
        httpContext.Items[HttpContextItemKey] = tempDataInstance;
        httpContext.Response.OnStarting(() =>
        {
            tempDataService.Save(httpContext, tempDataInstance);
            return Task.CompletedTask;
        });

        return tempDataInstance;
    }
}
