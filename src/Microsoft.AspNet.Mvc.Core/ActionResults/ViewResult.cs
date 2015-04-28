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
        /// When <c>null</c>, defaults to <see cref="ActionDescriptor.Name"/>.
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
        public override async Task ExecuteResultAsync([NotNull] ActionContext context)
        {
            var viewEngine = ViewEngine ??
                             context.HttpContext.RequestServices.GetRequiredService<ICompositeViewEngine>();

            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ViewResult>>();

            var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<MvcOptions>>();

            var viewName = ViewName ?? context.ActionDescriptor.Name;
            var viewEngineResult = viewEngine.FindView(context, viewName);
            if(!viewEngineResult.Success)
            {
                logger.LogError(
                    "The view '{ViewName}' was not found. Searched locations: {SearchedViewLocations}", 
                    viewName,
                    viewEngineResult.SearchedLocations);
            }

            var view = viewEngineResult.EnsureSuccessful().View;

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
                    options.Options.HtmlHelperOptions,
                    ContentType);
            }
        }
    }
}
