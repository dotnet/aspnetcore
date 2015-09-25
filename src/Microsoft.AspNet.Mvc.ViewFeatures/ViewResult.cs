// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Represents an <see cref="ActionResult"/> that renders a view to the response.
    /// </summary>
    public class ViewResult : ActionResult
    {
        /// <summary>
        /// Gets or sets the HTTP status code.
        /// </summary>
        public int? StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the name of the view to render.
        /// </summary>
        /// <remarks>
        /// When <c>null</c>, defaults to <see cref="Abstractions.ActionDescriptor.Name"/>.
        /// </remarks>
        public string ViewName { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ViewDataDictionary"/> for this result.
        /// </summary>
        public ViewDataDictionary ViewData { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ITempDataDictionary"/> for this result.
        /// </summary>
        public ITempDataDictionary TempData { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IViewEngine"/> used to locate views.
        /// </summary>
        /// <remarks>When <c>null</c>, an instance of <see cref="ICompositeViewEngine"/> from
        /// <c>ActionContext.HttpContext.RequestServices</c> is used.</remarks>
        public IViewEngine ViewEngine { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="MediaTypeHeaderValue"/> representing the Content-Type header of the response.
        /// </summary>
        public MediaTypeHeaderValue ContentType { get; set; }

        /// <inheritdoc />
        public override async Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var services = context.HttpContext.RequestServices;
            var viewEngine = ViewEngine ?? services.GetRequiredService<ICompositeViewEngine>();

            var logger = services.GetRequiredService<ILogger<ViewResult>>();
            var telemetry = services.GetRequiredService<TelemetrySource>();

            var options = services.GetRequiredService<IOptions<MvcViewOptions>>();

            var viewName = ViewName ?? context.ActionDescriptor.Name;
            var viewEngineResult = viewEngine.FindView(context, viewName);
            if (!viewEngineResult.Success)
            {
                if (telemetry.IsEnabled("Microsoft.AspNet.Mvc.ViewResultViewNotFound"))
                {
                    telemetry.WriteTelemetry(
                        "Microsoft.AspNet.Mvc.ViewResultViewNotFound",
                        new
                        {
                            actionContext = context,
                            result = this,
                            viewName = viewName,
                            searchedLocations = viewEngineResult.SearchedLocations
                        });
                }

                logger.LogError(
                    "The view '{ViewName}' was not found. Searched locations: {SearchedViewLocations}",
                    viewName,
                    viewEngineResult.SearchedLocations);
            }

            var view = viewEngineResult.EnsureSuccessful().View;
            if (telemetry.IsEnabled("Microsoft.AspNet.Mvc.ViewResultViewFound"))
            {
                telemetry.WriteTelemetry(
                    "Microsoft.AspNet.Mvc.ViewResultViewFound",
                    new { actionContext = context, result = this, viewName, view = view });
            }

            logger.LogVerbose("The view '{ViewName}' was found.", viewName);

            if (StatusCode != null)
            {
                context.HttpContext.Response.StatusCode = StatusCode.Value;
            }

            using (view as IDisposable)
            {
                await ViewExecutor.ExecuteAsync(
                    view,
                    context,
                    ViewData,
                    TempData,
                    options.Value.HtmlHelperOptions,
                    ContentType);
            }
        }
    }
}
