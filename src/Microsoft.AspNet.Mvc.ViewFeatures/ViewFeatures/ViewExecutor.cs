// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.Tracing;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.Framework.OptionsModel;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc.ViewFeatures
{
    /// <summary>
    /// Executes an <see cref="IView"/>.
    /// </summary>
    public class ViewExecutor
    {
        /// <summary>
        /// The default content-type header value for views, <c>text/html; charset=utf8</c>.
        /// </summary>
        public static readonly MediaTypeHeaderValue DefaultContentType = new MediaTypeHeaderValue("text/html")
        {
            Encoding = Encoding.UTF8
        }.CopyAsReadOnly();

        /// <summary>
        /// Creates a new <see cref="ViewExecutor"/>.
        /// </summary>
        /// <param name="viewOptions">The <see cref="IOptions{MvcViewOptions}"/>.</param>
        /// <param name="writerFactory">The <see cref="IHttpResponseStreamWriterFactory"/>.</param>
        /// <param name="viewEngine">The <see cref="ICompositeViewEngine"/>.</param>
        /// <param name="telemetry">The <see cref="TelemetrySource"/>.</param>
        public ViewExecutor(
            IOptions<MvcViewOptions> viewOptions,
            IHttpResponseStreamWriterFactory writerFactory,
            ICompositeViewEngine viewEngine,
            TelemetrySource telemetry)
        {
            if (viewOptions == null)
            {
                throw new ArgumentNullException(nameof(viewOptions));
            }

            if (writerFactory == null)
            {
                throw new ArgumentNullException(nameof(writerFactory));
            }

            if (viewEngine == null)
            {
                throw new ArgumentNullException(nameof(viewEngine));
            }

            if (telemetry == null)
            {
                throw new ArgumentNullException(nameof(telemetry));
            }

            ViewOptions = viewOptions.Value;
            WriterFactory = writerFactory;
            ViewEngine = viewEngine;
            Telemetry = telemetry;
        }

        /// <summary>
        /// Gets the <see cref="TelemetrySource"/>.
        /// </summary>
        protected TelemetrySource Telemetry { get; }

        /// <summary>
        /// Gets the default <see cref="IViewEngine"/>.
        /// </summary>
        protected IViewEngine ViewEngine { get; }

        /// <summary>
        /// Gets the <see cref="MvcViewOptions"/>.
        /// </summary>
        protected MvcViewOptions ViewOptions { get; }

        /// <summary>
        /// Gets the <see cref="IHttpResponseStreamWriterFactory"/>.
        /// </summary>
        protected IHttpResponseStreamWriterFactory WriterFactory { get; }

        /// <summary>
        /// Executes a view asynchronously.
        /// </summary>
        /// <param name="actionContext">The <see cref="ActionContext"/> associated with the current request.</param>
        /// <param name="view">The <see cref="IView"/>.</param>
        /// <param name="viewData">The <see cref="ViewDataDictionary"/>.</param>
        /// <param name="tempData">The <see cref="ITempDataDictionary"/>.</param>
        /// <param name="contentType">
        /// The content-type header value to set in the response. If <c>null</c>, <see cref="DefaultContentType"/> will be used.
        /// </param>
        /// <param name="statusCode">
        /// The HTTP status code to set in the response. May be <c>null</c>.
        /// </param>
        /// <returns>A <see cref="Task"/> which will complete when view execution is completed.</returns>
        public virtual async Task ExecuteAsync(
            ActionContext actionContext,
            IView view,
            ViewDataDictionary viewData,
            ITempDataDictionary tempData,
            MediaTypeHeaderValue contentType,
            int? statusCode)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            if (viewData == null)
            {
                throw new ArgumentNullException(nameof(viewData));
            }

            if (tempData == null)
            {
                throw new ArgumentNullException(nameof(tempData));
            }

            var response = actionContext.HttpContext.Response;

            if (contentType != null && contentType.Encoding == null)
            {
                // Do not modify the user supplied content type, so copy it instead
                contentType = contentType.Copy();
                contentType.Encoding = Encoding.UTF8;
            }

            // Priority list for setting content-type:
            //      1. passed in contentType (likely set by the user on the result)
            //      2. response.ContentType (likely set by the user in controller code)
            //      3. ViewExecutor.DefaultContentType (sensible default)
            response.ContentType = contentType?.ToString() ?? response.ContentType ?? DefaultContentType.ToString();

            if (statusCode != null)
            {
                response.StatusCode = statusCode.Value;
            }

            var encoding = contentType?.Encoding ?? DefaultContentType.Encoding;
            using (var writer = WriterFactory.CreateWriter(response.Body, encoding))
            {
                var viewContext = new ViewContext(
                    actionContext,
                    view,
                    viewData,
                    tempData,
                    writer,
                    ViewOptions.HtmlHelperOptions);

                if (Telemetry.IsEnabled("Microsoft.AspNet.Mvc.BeforeView"))
                {
                    Telemetry.WriteTelemetry(
                        "Microsoft.AspNet.Mvc.BeforeView",
                        new { view = view, viewContext = viewContext, });
                }

                await view.RenderAsync(viewContext);

                if (Telemetry.IsEnabled("Microsoft.AspNet.Mvc.AfterView"))
                {
                    Telemetry.WriteTelemetry(
                        "Microsoft.AspNet.Mvc.AfterView",
                        new { view = view, viewContext = viewContext, });
                }

                // Perf: Invoke FlushAsync to ensure any buffered content is asynchronously written to the underlying
                // response asynchronously. In the absence of this line, the buffer gets synchronously written to the
                // response as part of the Dispose which has a perf impact.
                await writer.FlushAsync();
            }
        }
    }
}