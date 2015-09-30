// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc.ViewFeatures
{
    /// <summary>
    /// Finds and executes an <see cref="IView"/> for a <see cref="ViewResult"/>.
    /// </summary>
    public class ViewResultExecutor : ViewExecutor
    {
        /// <summary>
        /// Creates a new <see cref="ViewResultExecutor"/>.
        /// </summary>
        /// <param name="viewOptions">The <see cref="IOptions{MvcViewOptions}"/>.</param>
        /// <param name="viewEngine">The <see cref="ICompositeViewEngine"/>.</param>
        /// <param name="telemetry">The <see cref="TelemetrySource"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public ViewResultExecutor(
            IOptions<MvcViewOptions> viewOptions,
            ICompositeViewEngine viewEngine,
            TelemetrySource telemetry,
            ILoggerFactory loggerFactory)
            : base(viewOptions, viewEngine, telemetry)
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
            var viewName = viewResult.ViewName ?? actionContext.ActionDescriptor.Name;

            var result = viewEngine.FindView(actionContext, viewName);
            if (result.Success)
            {
                if (Telemetry.IsEnabled("Microsoft.AspNet.Mvc.ViewFound"))
                {
                    Telemetry.WriteTelemetry(
                        "Microsoft.AspNet.Mvc.ViewFound",
                        new
                        {
                            actionContext = actionContext,
                            isPartial = false,
                            result = viewResult,
                            viewName = viewName,
                            view = result.View,
                        });
                }

                Logger.LogVerbose("The view '{ViewName}' was found.", viewName);
            }
            else
            {
                if (Telemetry.IsEnabled("Microsoft.AspNet.Mvc.ViewNotFound"))
                {
                    Telemetry.WriteTelemetry(
                        "Microsoft.AspNet.Mvc.ViewNotFound",
                        new
                        {
                            actionContext = actionContext,
                            isPartial = false,
                            result = viewResult,
                            viewName = viewName,
                            searchedLocations = result.SearchedLocations
                        });
                }

                Logger.LogError(
                    "The view '{ViewName}' was not found. Searched locations: {SearchedViewLocations}",
                    viewName,
                    result.SearchedLocations);
            }

            return result;
        }

        /// <summary>
        /// Executes the <see cref="IView"/> asynchronously.
        /// </summary>
        /// <param name="actionContext">The <see cref="ActionContext"/> associated with the current request.</param>
        /// <param name="view">The <see cref="IView"/>.</param>
        /// <param name="viewResult">The <see cref="ViewResult"/>.</param>
        /// <returns>A <see cref="Task"/> which will complete when view execution is completed.</returns>
        public virtual Task ExecuteAsync(ActionContext actionContext, IView view, ViewResult viewResult)
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
    }
}
