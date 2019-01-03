// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    /// <summary>
    /// Finds and executes an <see cref="IView"/> for a <see cref="PartialViewResult"/>.
    /// </summary>
    internal class PartialViewResultTemplateExecutor : ViewTemplateExecutor, IActionResultExecutor<PartialViewResult>
    {
        private const string ActionNameKey = "action";

        /// <summary>
        /// Creates a new <see cref="PartialViewResultExecutor"/>.
        /// </summary>
        /// <param name="viewOptions">The <see cref="IOptions{MvcViewOptions}"/>.</param>
        /// <param name="writerFactory">The <see cref="IHttpResponseStreamWriterFactory"/>.</param>
        /// <param name="viewFactory">The <see cref="IViewTemplateFactory"/>.</param>
        /// <param name="tempDataFactory">The <see cref="ITempDataDictionaryFactory"/>.</param>
        /// <param name="diagnosticListener">The <see cref="DiagnosticListener"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
        public PartialViewResultTemplateExecutor(
            IOptions<MvcViewOptions> viewOptions,
            IHttpResponseStreamWriterFactory writerFactory,
            IViewTemplateFactory viewFactory,
            ITempDataDictionaryFactory tempDataFactory,
            DiagnosticListener diagnosticListener,
            ILoggerFactory loggerFactory,
            IModelMetadataProvider modelMetadataProvider)
            : base(viewOptions, writerFactory, viewFactory, tempDataFactory, diagnosticListener, modelMetadataProvider)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            Logger = loggerFactory.CreateLogger<PartialViewResultTemplateExecutor>();
        }

        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        protected ILogger Logger { get; }

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

            var viewName = result.ViewName ?? NormalizedRouteValue.GetNormalizedRouteValue(context, ActionNameKey);
            var viewFactoryContext = new ViewFactoryContext(context, executingFilePath: null, viewName, isMainPage: false);
            var locateViewResult = await ViewFactory.LocateViewAsync(viewFactoryContext);

            if (!locateViewResult.Success)
            {
                throw new InvalidOperationException();
            }

            Logger.PartialViewResultExecuting(viewName);
            await ExecuteAsync(
                context,
                locateViewResult.ViewTemplate,
                result.ViewData,
                result.TempData,
                result.ContentType,
                result.StatusCode);

            Logger.PartialViewResultExecuted(result.ViewName, stopwatch.GetElapsedTime());
        }
    }
}
