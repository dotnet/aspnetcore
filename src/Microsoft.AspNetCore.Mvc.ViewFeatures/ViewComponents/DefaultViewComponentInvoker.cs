// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ViewComponents
{
    /// <summary>
    /// Default implementation for <see cref="IViewComponentInvoker"/>.
    /// </summary>
    internal class DefaultViewComponentInvoker : IViewComponentInvoker
    {
        private readonly IViewComponentFactory _viewComponentFactory;
        private readonly ViewComponentInvokerCache _viewComponentInvokerCache;
        private readonly DiagnosticListener _diagnosticListener;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultViewComponentInvoker"/>.
        /// </summary>
        /// <param name="viewComponentFactory">The <see cref="IViewComponentFactory"/>.</param>
        /// <param name="viewComponentInvokerCache">The <see cref="ViewComponentInvokerCache"/>.</param>
        /// <param name="diagnosticListener">The <see cref="DiagnosticListener"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public DefaultViewComponentInvoker(
            IViewComponentFactory viewComponentFactory,
            ViewComponentInvokerCache viewComponentInvokerCache,
            DiagnosticListener diagnosticListener,
            ILogger logger)
        {
            if (viewComponentFactory == null)
            {
                throw new ArgumentNullException(nameof(viewComponentFactory));
            }

            if (viewComponentInvokerCache == null)
            {
                throw new ArgumentNullException(nameof(viewComponentInvokerCache));
            }

            if (diagnosticListener == null)
            {
                throw new ArgumentNullException(nameof(diagnosticListener));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _viewComponentFactory = viewComponentFactory;
            _viewComponentInvokerCache = viewComponentInvokerCache;
            _diagnosticListener = diagnosticListener;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task InvokeAsync(ViewComponentContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var executor = _viewComponentInvokerCache.GetViewComponentMethodExecutor(context);

            var returnType = executor.MethodReturnType;

            if (returnType == typeof(void) || returnType == typeof(Task))
            {
                throw new InvalidOperationException(Resources.ViewComponent_MustReturnValue);
            }

            IViewComponentResult result;
            if (executor.IsMethodAsync)
            {
                result = await InvokeAsyncCore(executor, context);
            }
            else
            {
                // We support falling back to synchronous if there is no InvokeAsync method, in this case we'll still
                // execute the IViewResult asynchronously.
                result = InvokeSyncCore(executor, context);
            }

            await result.ExecuteAsync(context);
        }

        private async Task<IViewComponentResult> InvokeAsyncCore(ObjectMethodExecutor executor, ViewComponentContext context)
        {
            var component = _viewComponentFactory.CreateViewComponent(context);

            using (_logger.ViewComponentScope(context))
            {
                var arguments = PrepareArguments(context.Arguments, executor);

                _diagnosticListener.BeforeViewComponent(context, component);
                _logger.ViewComponentExecuting(context, arguments);

                var stopwatch = ValueStopwatch.StartNew();

                object resultAsObject;
                var returnType = executor.MethodReturnType;

                if (returnType == typeof(Task<IViewComponentResult>))
                {
                    resultAsObject = await (Task<IViewComponentResult>)executor.Execute(component, arguments);
                }
                else if (returnType == typeof(Task<string>))
                {
                    resultAsObject = await (Task<string>)executor.Execute(component, arguments);
                }
                else if (returnType == typeof(Task<IHtmlContent>))
                {
                    resultAsObject = await (Task<IHtmlContent>)executor.Execute(component, arguments);
                }
                else
                {
                    resultAsObject = await executor.ExecuteAsync(component, arguments);
                }

                var viewComponentResult = CoerceToViewComponentResult(resultAsObject);
                _logger.ViewComponentExecuted(context, stopwatch.GetElapsedTime(), viewComponentResult);
                _diagnosticListener.AfterViewComponent(context, viewComponentResult, component);

                _viewComponentFactory.ReleaseViewComponent(context, component);

                return viewComponentResult;
            }
        }

        private IViewComponentResult InvokeSyncCore(ObjectMethodExecutor executor, ViewComponentContext context)
        {
            var component = _viewComponentFactory.CreateViewComponent(context);

            using (_logger.ViewComponentScope(context))
            {
                var arguments = PrepareArguments(context.Arguments, executor);

                _diagnosticListener.BeforeViewComponent(context, component);
                _logger.ViewComponentExecuting(context, arguments);

                var stopwatch = ValueStopwatch.StartNew();
                object result;

                try
                {
                    result = executor.Execute(component, arguments);
                }
                finally
                {
                    _viewComponentFactory.ReleaseViewComponent(context, component);
                }

                var viewComponentResult = CoerceToViewComponentResult(result);
                _logger.ViewComponentExecuted(context, stopwatch.GetElapsedTime(), viewComponentResult);
                _diagnosticListener.AfterViewComponent(context, viewComponentResult, component);

                _viewComponentFactory.ReleaseViewComponent(context, component);

                return viewComponentResult;
            }
        }

        private static IViewComponentResult CoerceToViewComponentResult(object value)
        {
            if (value == null)
            {
                throw new InvalidOperationException(Resources.ViewComponent_MustReturnValue);
            }

            if (value is IViewComponentResult componentResult)
            {
                return componentResult;
            }

            if (value is string stringResult)
            {
                return new ContentViewComponentResult(stringResult);
            }

            if (value is IHtmlContent htmlContent)
            {
                return new HtmlContentViewComponentResult(htmlContent);
            }

            throw new InvalidOperationException(Resources.FormatViewComponent_InvalidReturnValue(
                typeof(string).Name,
                typeof(IHtmlContent).Name,
                typeof(IViewComponentResult).Name));
        }

        private static object[] PrepareArguments(
            IDictionary<string, object> parameters,
            ObjectMethodExecutor objectMethodExecutor)
        {
            var declaredParameterInfos = objectMethodExecutor.MethodParameters;
            var count = declaredParameterInfos.Length;
            if (count == 0)
            {
                return null;
            }

            var arguments = new object[count];
            for (var index = 0; index < count; index++)
            {
                var parameterInfo = declaredParameterInfos[index];

                if (!parameters.TryGetValue(parameterInfo.Name, out var value))
                {
                    value = objectMethodExecutor.GetDefaultValueForParameter(index);
                }

                arguments[index] = value;
            }

            return arguments;
        }
    }
}