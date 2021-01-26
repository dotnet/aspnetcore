// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    /// <summary>
    /// Finds and executes an <see cref="IView"/> for a <see cref="PartialViewResult"/>.
    /// </summary>
    public class PartialViewResultExecutor : ViewExecutor, IActionResultExecutor<PartialViewResult>
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
            var viewName = viewResult.ViewName ?? GetActionName(actionContext);

            var stopwatch = ValueStopwatch.StartNew();

            var result = viewEngine.GetView(executingFilePath: null, viewPath: viewName, isMainPage: false);
            var originalResult = result;
            if (!result.Success)
            {
                result = viewEngine.FindView(actionContext, viewName, isMainPage: false);
            }

            Logger.PartialViewResultExecuting(result.ViewName);
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

                Logger.PartialViewFound(result.View, stopwatch.GetElapsedTime());
            }
            else
            {
                DiagnosticListener.ViewNotFound(
                    actionContext,
                    isMainPage: false,
                    viewResult: viewResult,
                    viewName: viewName,
                    searchedLocations: result.SearchedLocations);

                Logger.PartialViewNotFound(viewName, result.SearchedLocations);
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

            Logger.PartialViewResultExecuted(result.ViewName, stopwatch.GetElapsedTime());
        }

        private static string GetActionName(ActionContext context)
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
            string normalizedValue = null;
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
    }
}
