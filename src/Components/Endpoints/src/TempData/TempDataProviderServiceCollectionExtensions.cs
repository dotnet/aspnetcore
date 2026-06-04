// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.DataProtection;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal static partial class TempDataProviderServiceCollectionExtensions
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
        return tempDataInstance;
    }

    internal static void PersistTempData(HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue(HttpContextItemKey, out var tempDataObj) && tempDataObj is ITempData tempData)
        {
            var provider = httpContext.RequestServices.GetService<ITempDataProvider>();
            if (provider is CookieTempDataProvider && httpContext.Response.HasStarted)
            {
                var logger = httpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(TempDataProviderServiceCollectionExtensions).FullName!);
                Log.CookieTempDataNotPersistedAfterResponseStarted(logger);
                return;
            }

            var tempDataService = httpContext.RequestServices.GetRequiredService<TempDataService>();
            tempDataService.Save(httpContext, tempData);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Warning,
            "TempData values written during or after streaming SSR cannot be persisted by the cookie TempData provider because the response has already started. " +
            "Switch to the session-storage TempData provider (RazorComponentsServiceOptions.TempDataProviderType = TempDataProviderType.SessionStorage) to enable persistence in streaming SSR scenarios.",
            EventName = "CookieTempDataNotPersistedAfterResponseStarted")]
        public static partial void CookieTempDataNotPersistedAfterResponseStarted(ILogger logger);
    }
}
