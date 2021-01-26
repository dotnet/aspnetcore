// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    /// <summary>
    /// Executes an <see cref="IView"/>.
    /// </summary>
    public class ViewExecutor
    {
        /// <summary>
        /// The default content-type header value for views, <c>text/html; charset=utf-8</c>.
        /// </summary>
        public static readonly string DefaultContentType = "text/html; charset=utf-8";

        /// <summary>
        /// Creates a new <see cref="ViewExecutor"/>.
        /// </summary>
        /// <param name="viewOptions">The <see cref="IOptions{MvcViewOptions}"/>.</param>
        /// <param name="writerFactory">The <see cref="IHttpResponseStreamWriterFactory"/>.</param>
        /// <param name="viewEngine">The <see cref="ICompositeViewEngine"/>.</param>
        /// <param name="tempDataFactory">The <see cref="ITempDataDictionaryFactory"/>.</param>
        /// <param name="diagnosticListener">The <see cref="DiagnosticListener"/>.</param>
        /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider" />.</param>
        public ViewExecutor(
            IOptions<MvcViewOptions> viewOptions,
            IHttpResponseStreamWriterFactory writerFactory,
            ICompositeViewEngine viewEngine,
            ITempDataDictionaryFactory tempDataFactory,
            DiagnosticListener diagnosticListener,
            IModelMetadataProvider modelMetadataProvider)
            : this(writerFactory, viewEngine, diagnosticListener)
        {
            if (viewOptions == null)
            {
                throw new ArgumentNullException(nameof(viewOptions));
            }

            if (tempDataFactory == null)
            {
                throw new ArgumentNullException(nameof(tempDataFactory));
            }

            if (diagnosticListener == null)
            {
                throw new ArgumentNullException(nameof(diagnosticListener));
            }

            ViewOptions = viewOptions.Value;
            TempDataFactory = tempDataFactory;
            ModelMetadataProvider = modelMetadataProvider;
        }

        /// <summary>
        /// Creates a new <see cref="ViewExecutor"/>.
        /// </summary>
        /// <param name="writerFactory">The <see cref="IHttpResponseStreamWriterFactory"/>.</param>
        /// <param name="viewEngine">The <see cref="ICompositeViewEngine"/>.</param>
        /// <param name="diagnosticListener">The <see cref="System.Diagnostics.DiagnosticListener"/>.</param>
        protected ViewExecutor(
            IHttpResponseStreamWriterFactory writerFactory,
            ICompositeViewEngine viewEngine,
            DiagnosticListener diagnosticListener)
        {
            if (writerFactory == null)
            {
                throw new ArgumentNullException(nameof(writerFactory));
            }

            if (viewEngine == null)
            {
                throw new ArgumentNullException(nameof(viewEngine));
            }

            if (diagnosticListener == null)
            {
                throw new ArgumentNullException(nameof(diagnosticListener));
            }

            WriterFactory = writerFactory;
            ViewEngine = viewEngine;
            DiagnosticListener = diagnosticListener;
        }

        /// <summary>
        /// Gets the <see cref="DiagnosticListener"/>.
        /// </summary>
        protected DiagnosticListener DiagnosticListener { get; }

        /// <summary>
        /// Gets the <see cref="ITempDataDictionaryFactory"/>.
        /// </summary>
        protected ITempDataDictionaryFactory TempDataFactory { get; }

        /// <summary>
        /// Gets the default <see cref="IViewEngine"/>.
        /// </summary>
        protected IViewEngine ViewEngine { get; }

        /// <summary>
        /// Gets the <see cref="MvcViewOptions"/>.
        /// </summary>
        protected MvcViewOptions ViewOptions { get; }

        /// <summary>
        /// Gets the <see cref="IModelMetadataProvider"/>.
        /// </summary>
        protected IModelMetadataProvider ModelMetadataProvider { get; }

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
        /// The content-type header value to set in the response. If <c>null</c>,
        /// <see cref="DefaultContentType"/> will be used.
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
            string contentType,
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

            if (ViewOptions == null)
            {
                throw new InvalidOperationException(Resources.FormatPropertyOfTypeCannotBeNull(nameof(ViewOptions), GetType().Name));
            }

            if (TempDataFactory == null)
            {
                throw new InvalidOperationException(Resources.FormatPropertyOfTypeCannotBeNull(nameof(TempDataFactory), GetType().Name));
            }

            if (ModelMetadataProvider == null)
            {
                throw new InvalidOperationException(Resources.FormatPropertyOfTypeCannotBeNull(nameof(ModelMetadataProvider), GetType().Name));
            }

            if (viewData == null)
            {
                viewData = new ViewDataDictionary(ModelMetadataProvider, actionContext.ModelState);
            }

            if (tempData == null)
            {
                tempData = TempDataFactory.GetTempData(actionContext.HttpContext);
            }

            var viewContext = new ViewContext(
                actionContext,
                view,
                viewData,
                tempData,
                TextWriter.Null,
                ViewOptions.HtmlHelperOptions);

            await ExecuteAsync(viewContext, contentType, statusCode);
        }

        /// <summary>
        /// Executes a view asynchronously.
        /// </summary>
        /// <param name="viewContext">The <see cref="ViewContext"/> associated with the current request.</param>
        /// <param name="contentType">
        /// The content-type header value to set in the response. If <c>null</c>,
        /// <see cref="DefaultContentType"/> will be used.
        /// </param>
        /// <param name="statusCode">
        /// The HTTP status code to set in the response. May be <c>null</c>.
        /// </param>
        /// <returns>A <see cref="Task"/> which will complete when view execution is completed.</returns>
        protected async Task ExecuteAsync(
            ViewContext viewContext,
            string contentType,
            int? statusCode)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            var response = viewContext.HttpContext.Response;

            ResponseContentTypeHelper.ResolveContentTypeAndEncoding(
                contentType,
                response.ContentType,
                DefaultContentType,
                out var resolvedContentType,
                out var resolvedContentTypeEncoding);

            response.ContentType = resolvedContentType;

            if (statusCode != null)
            {
                response.StatusCode = statusCode.Value;
            }

            OnExecuting(viewContext);

            await using (var writer = WriterFactory.CreateWriter(response.Body, resolvedContentTypeEncoding))
            {
                var view = viewContext.View;

                var oldWriter = viewContext.Writer;
                try
                {
                    viewContext.Writer = writer;

                    DiagnosticListener.BeforeView(view, viewContext);

                    await view.RenderAsync(viewContext);

                    DiagnosticListener.AfterView(view, viewContext);
                }
                finally
                {
                    viewContext.Writer = oldWriter;
                }

                // Perf: Invoke FlushAsync to ensure any buffered content is asynchronously written to the underlying
                // response asynchronously. In the absence of this line, the buffer gets synchronously written to the
                // response as part of the Dispose which has a perf impact.
                await writer.FlushAsync();
            }
        }

        private void OnExecuting(ViewContext viewContext)
        {
            var viewDataValuesProvider = viewContext.HttpContext.Features.Get<IViewDataValuesProviderFeature>();
            if (viewDataValuesProvider != null)
            {
                viewDataValuesProvider.ProvideViewDataValues(viewContext.ViewData);
            }
        }
    }
}
