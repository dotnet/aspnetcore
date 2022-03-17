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
/// Finds and executes an <see cref="IView"/> for a <see cref="PartialViewResult"/>.
/// </summary>
public partial class PartialViewResultExecutor : ViewExecutor, IActionResultExecutor<PartialViewResult>
{
    private const string ActionNameKey = "action";

    /// <summary>
    /// Creates a new <see cref="PartialViewResultExecutor"/>.
    /// </summary>
    /// <param name="viewOptions">The <see cref="IOptions{MvcViewOptions}"/>.</param>
    /// <param name="writerFactory">The <see cref="IHttpResponseStreamWriterFactory"/>.</param>
    /// <param name="viewEngine">The <see cref="ICompositeViewEngine"/>.</param>
    /// <param name="tempDataFactory">The <see cref="ITempDataDictionaryFactory"/>.</param>
    /// <param name="diagnosticListener">The <see cref="DiagnosticListener"/>.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
    public PartialViewResultExecutor(
        IOptions<MvcViewOptions> viewOptions,
        IHttpResponseStreamWriterFactory writerFactory,
        ICompositeViewEngine viewEngine,
        ITempDataDictionaryFactory tempDataFactory,
        DiagnosticListener diagnosticListener,
        ILoggerFactory loggerFactory,
        IModelMetadataProvider modelMetadataProvider)
        : base(viewOptions, writerFactory, viewEngine, tempDataFactory, diagnosticListener, modelMetadataProvider)
    {
        if (loggerFactory == null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        Logger = loggerFactory.CreateLogger<PartialViewResultExecutor>();
    }

    /// <summary>
    /// Gets the <see cref="ILogger"/>.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Attempts to find the <see cref="IView"/> associated with <paramref name="viewResult"/>.
    /// </summary>
    /// <param name="actionContext">The <see cref="ActionContext"/> associated with the current request.</param>
    /// <param name="viewResult">The <see cref="PartialViewResult"/>.</param>
    /// <returns>A <see cref="ViewEngineResult"/>.</returns>
    public virtual ViewEngineResult FindView(ActionContext actionContext, PartialViewResult viewResult)
    {
        if (actionContext == null)
        {
            throw new ArgumentNullException(nameof(actionContext));
        }

        if (viewResult == null)
        {
            throw new ArgumentNullException(nameof(viewResult));
        }

        var viewEngine = viewResult.ViewEngine ?? ViewEngine;
        var viewName = viewResult.ViewName ?? GetActionName(actionContext) ?? string.Empty;

        var stopwatch = ValueStopwatch.StartNew();

        var result = viewEngine.GetView(executingFilePath: null, viewPath: viewName, isMainPage: false);
        var originalResult = result;
        if (!result.Success)
        {
            result = viewEngine.FindView(actionContext, viewName, isMainPage: false);
        }

        Log.PartialViewResultExecuting(Logger, result.ViewName);
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

        if (result.Success)
        {
            DiagnosticListener.ViewFound(
                actionContext,
                isMainPage: false,
                viewResult: viewResult,
                viewName: viewName,
                view: result.View);
            Log.PartialViewFound(Logger, result.View, stopwatch.GetElapsedTime());
        }
        else
        {
            DiagnosticListener.ViewNotFound(
                actionContext,
                isMainPage: false,
                viewResult: viewResult,
                viewName: viewName,
                searchedLocations: result.SearchedLocations);
            Log.PartialViewNotFound(Logger, viewName, result.SearchedLocations);
        }

        return result;
    }

    /// <summary>
    /// Executes the <see cref="IView"/> asynchronously.
    /// </summary>
    /// <param name="actionContext">The <see cref="ActionContext"/> associated with the current request.</param>
    /// <param name="view">The <see cref="IView"/>.</param>
    /// <param name="viewResult">The <see cref="PartialViewResult"/>.</param>
    /// <returns>A <see cref="Task"/> which will complete when view execution is completed.</returns>
    public virtual Task ExecuteAsync(ActionContext actionContext, IView view, PartialViewResult viewResult)
    {
        if (actionContext == null)
        {
            throw new ArgumentNullException(nameof(actionContext));
        }

        if (view == null)
        {
            throw new ArgumentNullException(nameof(view));
        }

        if (viewResult == null)
        {
            throw new ArgumentNullException(nameof(viewResult));
        }

        return ExecuteAsync(
            actionContext,
            view,
            viewResult.ViewData,
            viewResult.TempData,
            viewResult.ContentType,
            viewResult.StatusCode);
    }

    /// <inheritdoc />
    public virtual async Task ExecuteAsync(ActionContext context, PartialViewResult result)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        var stopwatch = ValueStopwatch.StartNew();

        var viewEngineResult = FindView(context, result);
        viewEngineResult.EnsureSuccessful(originalLocations: null);

        var view = viewEngineResult.View;
        using (view as IDisposable)
        {
            await ExecuteAsync(context, view, result);
        }

        Log.PartialViewResultExecuted(Logger, result.ViewName, stopwatch.GetElapsedTime());
    }

    private static string? GetActionName(ActionContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

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
        [LoggerMessage(1, LogLevel.Information, "Executing PartialViewResult, running view {PartialViewName}.", EventName = "PartialViewResultExecuting")]
        public static partial void PartialViewResultExecuting(ILogger logger, string partialViewName);

        [LoggerMessage(2, LogLevel.Debug, "The partial view path '{PartialViewFilePath}' was found in {ElapsedMilliseconds}ms.", EventName = "PartialViewFound")]
        private static partial void PartialViewFound(ILogger logger, string partialViewFilePath, double elapsedMilliseconds);

        public static void PartialViewFound(ILogger logger, IView view, TimeSpan timespan)
        {
            PartialViewFound(logger, view.Path, timespan.TotalMilliseconds);
        }

        [LoggerMessage(3, LogLevel.Error, "The partial view '{PartialViewName}' was not found. Searched locations: {SearchedViewLocations}", EventName = "PartialViewNotFound")]
        public static partial void PartialViewNotFound(ILogger logger, string partialViewName, IEnumerable<string> searchedViewLocations);

        [LoggerMessage(4, LogLevel.Information, "Executed PartialViewResult - view {PartialViewName} executed in {ElapsedMilliseconds}ms.", EventName = "PartialViewResultExecuted")]
        private static partial void PartialViewResultExecuted(ILogger logger, string? partialViewName, double elapsedMilliseconds);

        public static void PartialViewResultExecuted(ILogger logger, string? partialViewName, TimeSpan timespan)
        {
            PartialViewResultExecuted(logger, partialViewName, timespan.TotalMilliseconds);
        }
    }
}
