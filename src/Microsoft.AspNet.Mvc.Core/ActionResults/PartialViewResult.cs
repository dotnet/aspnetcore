// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;
using Microsoft.Net.Http.Headers;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Represents an <see cref="ActionResult"/> that renders a partial view to the response.
    /// </summary>
    public class PartialViewResult : ActionResult
    {
        /// <summary>
        /// Gets or sets the HTTP status code.
        /// </summary>
        public int? StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the name of the partial view to render.
        /// </summary>
        /// <remarks>
        /// When <c>null</c>, defaults to <see cref="ActionDescriptor.Name"/>.
        /// </remarks>
        public string ViewName { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ViewDataDictionary"/> used for rendering the view for this result.
        /// </summary>
        public ViewDataDictionary ViewData { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ITempDataDictionary"/> used for rendering the view for this result.
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
        public override async Task ExecuteResultAsync([NotNull] ActionContext context)
        {
            var viewEngine = ViewEngine ??
                             context.HttpContext.RequestServices.GetRequiredService<ICompositeViewEngine>();

            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<PartialViewResult>>();

            var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<MvcOptions>>();

            var viewName = ViewName ?? context.ActionDescriptor.Name;
            var viewEngineResult = viewEngine.FindPartialView(context, viewName);
            if (!viewEngineResult.Success)
            {
                logger.LogError(
                    "The partial view '{PartialViewName}' was not found. Searched locations: {SearchedViewLocations}",
                    viewName,
                    viewEngineResult.SearchedLocations);
            }

            var view = viewEngineResult.EnsureSuccessful().View;

            logger.LogVerbose("The partial view '{PartialViewName}' was found.", viewName);

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
                    options.Options.HtmlHelperOptions,
                    ContentType);
            }
        }
    }
}
