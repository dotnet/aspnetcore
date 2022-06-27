// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;

/// <summary>
/// A filter that saves temp data.
/// </summary>
internal sealed class SaveTempDataFilter : IResourceFilter, IResultFilter
{
    private static readonly Func<object, Task> OnStartingCallback = (state) => OnStarting((HttpContext)state);
    // Internal for unit testing
    internal static readonly object SaveTempDataFilterContextKey = new object();

    private readonly ITempDataDictionaryFactory _factory;

    /// <summary>
    /// Creates a new instance of <see cref="SaveTempDataFilter"/>.
    /// </summary>
    /// <param name="factory">The <see cref="ITempDataDictionaryFactory"/>.</param>
    public SaveTempDataFilter(ITempDataDictionaryFactory factory)
    {
        _factory = factory;
    }

    /// <inheritdoc />
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        if (!context.HttpContext.Items.ContainsKey(SaveTempDataFilterContextKey))
        {
            var tempDataContext = new SaveTempDataContext()
            {
                Filters = context.Filters,
                TempDataDictionaryFactory = _factory
            };
            context.HttpContext.Items.Add(SaveTempDataFilterContextKey, tempDataContext);
        }

        if (!context.HttpContext.Response.HasStarted)
        {
            context.HttpContext.Response.OnStarting(
                callback: OnStartingCallback,
                state: context.HttpContext);
        }
    }

    private static Task OnStarting(HttpContext httpContext)
    {
        var saveTempDataContext = GetTempDataContext(httpContext);
        if (saveTempDataContext is null || saveTempDataContext.RequestHasUnhandledException)
        {
            return Task.CompletedTask;
        }

        // If temp data was already saved, skip trying to save again as the calls here would potentially fail
        // because the session feature might not be available at this point.
        // Example: An action returns NoContentResult and since NoContentResult does not write anything to
        // the body of the response, this delegate would get executed way late in the pipeline at which point
        // the session feature would have been removed.
        if (saveTempDataContext.TempDataSaved)
        {
            return Task.CompletedTask;
        }

        SaveTempData(
            result: null,
            factory: saveTempDataContext.TempDataDictionaryFactory,
            filters: saveTempDataContext.Filters,
            httpContext: httpContext);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void OnResourceExecuted(ResourceExecutedContext context)
    {
        // If there is an unhandled exception, we would like to avoid setting tempdata as
        // the end user is going to see an error page anyway and also it helps us in avoiding
        // accessing resources like Session too late in the request lifecyle where SessionFeature might
        // not be available.
        if (!context.HttpContext.Response.HasStarted && context.Exception != null)
        {
            var saveTempDataContext = GetTempDataContext(context.HttpContext);
            if (saveTempDataContext != null)
            {
                saveTempDataContext.RequestHasUnhandledException = true;
            }
        }
    }

    /// <inheritdoc />
    public void OnResultExecuting(ResultExecutingContext context)
    {
    }

    /// <inheritdoc />
    public void OnResultExecuted(ResultExecutedContext context)
    {
        // We are doing this here again because the OnStarting delegate above might get fired too late in scenarios
        // where the action result doesn't write anything to the body. This causes the delegate to be executed
        // late in the pipeline at which point SessionFeature would not be available.
        if (!context.HttpContext.Response.HasStarted)
        {
            SaveTempData(context.Result, _factory, context.Filters, context.HttpContext);

            var saveTempDataContext = GetTempDataContext(context.HttpContext);
            if (saveTempDataContext != null)
            {
                saveTempDataContext.TempDataSaved = true;
            }
        }
    }

    private static SaveTempDataContext GetTempDataContext(HttpContext httpContext)
    {
        SaveTempDataContext saveTempDataContext = null;
        if (httpContext.Items.TryGetValue(SaveTempDataFilterContextKey, out var value))
        {
            saveTempDataContext = (SaveTempDataContext)value;
        }
        return saveTempDataContext;
    }

    private static void SaveTempData(
        IActionResult result,
        ITempDataDictionaryFactory factory,
        IList<IFilterMetadata> filters,
        HttpContext httpContext)
    {
        var tempData = factory.GetTempData(httpContext);

        for (var i = 0; i < filters.Count; i++)
        {
            if (filters[i] is ISaveTempDataCallback callback)
            {
                callback.OnTempDataSaving(tempData);
            }
        }

        if (result is IKeepTempDataResult)
        {
            tempData.Keep();
        }

        tempData.Save();
    }

    internal sealed class SaveTempDataContext
    {
        public bool RequestHasUnhandledException { get; set; }
        public bool TempDataSaved { get; set; }
        public IList<IFilterMetadata> Filters { get; set; }
        public ITempDataDictionaryFactory TempDataDictionaryFactory { get; set; }
    }
}
