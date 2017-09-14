// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    /// <summary>
    /// Finds and executes an <see cref="IView"/> for a <see cref="ViewResult"/>.
    /// </summary>
    public class ViewResultExecutor : ViewExecutor, IActionResultExecutor<ViewResult>
    {
        private const string ActionNameKey = "action";

        /// <summary>
        /// Creates a new <see cref="ViewResultExecutor"/>.
        /// </summary>
        /// <param name="viewOptions">The <see cref="IOptions{MvcViewOptions}"/>.</param>
        /// <param name="writerFactory">The <see cref="IHttpResponseStreamWriterFactory"/>.</param>
        /// <param name="viewEngine">The <see cref="ICompositeViewEngine"/>.</param>
        /// <param name="tempDataFactory">The <see cref="ITempDataDictionaryFactory"/>.</param>
        /// <param name="diagnosticSource">The <see cref="DiagnosticSource"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
        public ViewResultExecutor(
            IOptions<MvcViewOptions> viewOptions,
            IHttpResponseStreamWriterFactory writerFactory,
            ICompositeViewEngine viewEngine,
            ITempDataDictionaryFactory tempDataFactory,
            DiagnosticSource diagnosticSource,
            ILoggerFactory loggerFactory,
            IModelMetadataProvider modelMetadataProvider)
            : base(viewOptions, writerFactory, viewEngine, tempDataFactory, diagnosticSource, modelMetadataProvider)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

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

            var result = viewEngine.GetView(executingFilePath: null, viewPath: viewName, isMainPage: true);
            var originalResult = result;
            if (!result.Success)
            {
                result = viewEngine.FindView(actionContext, viewName, isMainPage: true);
            }

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
                if (DiagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.ViewFound"))
                {
                    DiagnosticSource.Write(
                        "Microsoft.AspNetCore.Mvc.ViewFound",
                        new
                        {
                            actionContext = actionContext,
                            isMainPage = true,
                            result = viewResult,
                            viewName = viewName,
                            view = result.View,
                        });
                }

                Logger.ViewFound(viewName);
            }
            else
            {
                if (DiagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.ViewNotFound"))
                {
                    DiagnosticSource.Write(
                        "Microsoft.AspNetCore.Mvc.ViewNotFound",
                        new
                        {
                            actionContext = actionContext,
                            isMainPage = true,
                            result = viewResult,
                            viewName = viewName,
                            searchedLocations = result.SearchedLocations
                        });
                }
                Logger.ViewNotFound(viewName, result.SearchedLocations);
            }

            return result;
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(ActionContext context, ViewResult result)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var viewEngineResult = FindView(context, result);
            viewEngineResult.EnsureSuccessful(originalLocations: null);

            var view = viewEngineResult.View;
            using (view as IDisposable)
            {
                Logger.ViewResultExecuting(view);

                await ExecuteAsync(
                    context,
                    view,
                    result.ViewData,
                    result.TempData,
                    result.ContentType,
                    result.StatusCode);
            }
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

            var stringRouteValue = routeValue?.ToString();
            if (string.Equals(normalizedValue, stringRouteValue, StringComparison.OrdinalIgnoreCase))
            {
                return normalizedValue;
            }

            return stringRouteValue;
        }
    }
}
