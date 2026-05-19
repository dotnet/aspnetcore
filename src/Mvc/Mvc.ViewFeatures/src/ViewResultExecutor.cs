// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// Finds and executes an <see cref="IView"/> for a <see cref="ViewResult"/>.
/// </summary>
public partial class ViewResultExecutor : ViewExecutor, IActionResultExecutor<ViewResult>
{
    private const string ActionNameKey = "action";

    /// <summary>
    /// Creates a new <see cref="ViewResultExecutor"/>.
    /// </summary>
    /// <param name="viewOptions">The <see cref="IOptions{MvcViewOptions}"/>.</param>
    /// <param name="writerFactory">The <see cref="IHttpResponseStreamWriterFactory"/>.</param>
    /// <param name="viewEngine">The <see cref="ICompositeViewEngine"/>.</param>
    /// <param name="tempDataFactory">The <see cref="ITempDataDictionaryFactory"/>.</param>
    /// <param name="diagnosticListener">The <see cref="DiagnosticListener"/>.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
    public ViewResultExecutor(
        IOptions<MvcViewOptions> viewOptions,
        IHttpResponseStreamWriterFactory writerFactory,
        ICompositeViewEngine viewEngine,
        ITempDataDictionaryFactory tempDataFactory,
        DiagnosticListener diagnosticListener,
        ILoggerFactory loggerFactory,
        IModelMetadataProvider modelMetadataProvider)
        : base(viewOptions, writerFactory, viewEngine, tempDataFactory, diagnosticListener, modelMetadataProvider)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        Logger = loggerFactory.CreateLogger<ViewResultExecutor>();
    }

    /// <summary>
    /// Gets the <see cref="ILogger"/>.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Attempts to find the <see cref="IView"/> associated with <paramref name="viewResult"/>.
    /// </summary>
    /// <param name="actionContext">The <see cref="ActionContext"/> associated with the current request.</param>
    /// <param name="viewResult">The <see cref="ViewResult"/>.</param>
    /// <returns>A <see cref="ViewEngineResult"/>.</returns>
    public virtual ViewEngineResult FindView(ActionContext actionContext, ViewResult viewResult)
    {
        ArgumentNullException.ThrowIfNull(actionContext);
        ArgumentNullException.ThrowIfNull(viewResult);

        var viewEngine = viewResult.ViewEngine ?? ViewEngine;

        var viewName = viewResult.ViewName ?? GetActionName(actionContext) ?? string.Empty;

        var stopwatch = ValueStopwatch.StartNew();

        var result = viewEngine.GetView(executingFilePath: null, viewPath: viewName, isMainPage: true);
        var originalResult = result;
        if (!result.Success)
        {
            result = viewEngine.FindView(actionContext, viewName, isMainPage: true);
        }

        Log.ViewResultExecuting(Logger, result.ViewName);
        if (!result.Success)
        {
            if (originalResult.SearchedLocations.Any())
            {
                if (result.SearchedLocations.Any())
                {
                    // Return a new ViewEngineResult listing all searched locations.
                    var locations = new List<string>(originalResult.SearchedLocations);
                    locations.AddRange(result.SearchedLocations);
                    result = ViewEngineResult.NotFound(viewName, locations);
                }
                else
                {
                    // GetView() searched locations but FindView() did not. Use first ViewEngineResult.
                    result = originalResult;
                }
            }
        }

        if (DiagnosticListener.IsEnabled())
        {
            OutputDiagnostics(actionContext, viewResult, viewName, result);
        }

        if (result.Success)
        {
            Log.ViewFound(Logger, result.View, stopwatch.GetElapsedTime());
        }
        else
        {
            Log.ViewNotFound(Logger, viewName, result.SearchedLocations);
        }

        return result;
    }

    private void OutputDiagnostics(ActionContext actionContext, ViewResult viewResult, string viewName, ViewEngineResult result)
    {
        if (result.Success)
        {
            DiagnosticListener.ViewFound(
                actionContext,
                isMainPage: true,
                viewResult,
                viewName,
                view: result.View);
        }
        else
        {
            DiagnosticListener.ViewNotFound(
                actionContext,
                isMainPage: true,
                viewResult,
                viewName,
                searchedLocations: result.SearchedLocations);
        }
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(ActionContext context, ViewResult result)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(result);

        var stopwatch = ValueStopwatch.StartNew();

        var viewEngineResult = FindView(context, result);
        viewEngineResult.EnsureSuccessful(originalLocations: null);

        var view = viewEngineResult.View;
        using (view as IDisposable)
        {
            await ExecuteAsync(
                context,
                view,
                result.ViewData,
                result.TempData,
                result.ContentType,
                result.StatusCode);
        }

        Log.ViewResultExecuted(Logger, viewEngineResult.ViewName, stopwatch.GetElapsedTime());
    }

    private static string? GetActionName(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.RouteData.Values.TryGetValue(ActionNameKey, out var routeValue))
        {
            return null;
        }

        var actionDescriptor = context.ActionDescriptor;
        string? normalizedValue = null;
        if (actionDescriptor.RouteValues.TryGetValue(ActionNameKey, out var value) &&
            !string.IsNullOrEmpty(value))
        {
            normalizedValue = value;
        }

        var stringRouteValue = Convert.ToString(routeValue, CultureInfo.InvariantCulture);
        if (string.Equals(normalizedValue, stringRouteValue, StringComparison.OrdinalIgnoreCase))
        {
            return normalizedValue;
        }

        return stringRouteValue;
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Information, "Executing ViewResult, running view {ViewName}.", EventName = "ViewResultExecuting")]
        public static partial void ViewResultExecuting(ILogger logger, string viewName);

        [LoggerMessage(2, LogLevel.Debug, "The view path '{ViewFilePath}' was found in {ElapsedMilliseconds}ms.", EventName = "ViewFound")]
        private static partial void ViewFound(ILogger logger, string viewFilePath, double elapsedMilliseconds);

        public static void ViewFound(ILogger logger, IView view, TimeSpan timespan)
        {
            ViewFound(logger, view.Path, timespan.TotalMilliseconds);
        }

        [LoggerMessage(3, LogLevel.Error, "The view '{ViewName}' was not found. Searched locations: {SearchedViewLocations}", EventName = "ViewNotFound")]
        public static partial void ViewNotFound(ILogger logger, string viewName, IEnumerable<string> searchedViewLocations);

        [LoggerMessage(4, LogLevel.Information, "Executed ViewResult - view {ViewName} executed in {ElapsedMilliseconds}ms.", EventName = "ViewResultExecuted")]
        private static partial void ViewResultExecuted(ILogger logger, string viewName, double elapsedMilliseconds);

        public static void ViewResultExecuted(ILogger logger, string viewName, TimeSpan timespan)
        {
            ViewResultExecuted(logger, viewName, timespan.TotalMilliseconds);
        }
    }
}
