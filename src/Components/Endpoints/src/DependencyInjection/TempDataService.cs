// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed partial class TempDataService
{
    private readonly ITempDataProvider _tempDataProvider;

    public TempDataService(ITempDataProvider tempDataProvider)
    {
        _tempDataProvider = tempDataProvider;
    }

    public TempData CreateEmpty(HttpContext httpContext)
    {
        return new TempData(() => Load(httpContext));
    }

    public IDictionary<string, object?> Load(HttpContext httpContext)
    {
        return _tempDataProvider.LoadTempData(httpContext);
    }

    public void Save(HttpContext httpContext, ITempData tempData)
    {
        if (httpContext.RequestServices.GetService<TempDataCascadingValueSupplier>() is { } supplier)
        {
            supplier.PersistValues(tempData);
        }

        if (tempData is not TempData data || !data.WasLoaded)
        {
            return;
        }
        _tempDataProvider.SaveTempData(httpContext, data.Save());
    }

    // Persists the per-request TempData (created lazily in
    // TempDataProviderServiceCollectionExtensions.GetOrCreateTempData) at the end of
    // rendering. Called explicitly by the invoker after streaming completes.
    // Cookie-based TempData cannot append Set-Cookie after Response.HasStarted, so we
    // log a warning and skip persistence in that case rather than throwing.
    public void Persist(HttpContext httpContext)
    {
        if (!httpContext.Items.TryGetValue(TempDataProviderServiceCollectionExtensions.HttpContextItemKey, out var tempDataObj)
            || tempDataObj is not ITempData tempData)
        {
            return;
        }

        if (_tempDataProvider is CookieTempDataProvider && httpContext.Response.HasStarted)
        {
            var logger = httpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(TempDataService).FullName!);
            Log.CookieTempDataNotPersistedAfterResponseStarted(logger);
            return;
        }

        Save(httpContext, tempData);
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
