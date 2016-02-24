// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ViewComponents
{
    /// <summary>
    /// Default implementation for <see cref="IViewComponentInvoker"/>.
    /// </summary>
    public class DefaultViewComponentInvoker : IViewComponentInvoker
    {
        private readonly IViewComponentFactory _viewComponentFactory;
        private readonly DiagnosticSource _diagnosticSource;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultViewComponentInvoker"/>.
        /// </summary>
        /// <param name="viewComponentFactory">The <see cref="IViewComponentFactory"/>.</param>
        /// <param name="diagnosticSource">The <see cref="DiagnosticSource"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public DefaultViewComponentInvoker(
            IViewComponentFactory viewComponentFactory,
            DiagnosticSource diagnosticSource,
            ILogger logger)
        {
            if (viewComponentFactory == null)
            {
                throw new ArgumentNullException(nameof(viewComponentFactory));
            }

            if (diagnosticSource == null)
            {
                throw new ArgumentNullException(nameof(diagnosticSource));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _viewComponentFactory = viewComponentFactory;
            _diagnosticSource = diagnosticSource;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task InvokeAsync(ViewComponentContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var methodInfo = context.ViewComponentDescriptor?.MethodInfo;
            if (methodInfo == null)
            {
                throw new InvalidOperationException(Resources.FormatPropertyOfTypeCannotBeNull(
                    nameof(ViewComponentDescriptor.MethodInfo),
                    nameof(ViewComponentDescriptor)));
            }

            var isAsync = typeof(Task).GetTypeInfo().IsAssignableFrom(methodInfo.ReturnType.GetTypeInfo());
            IViewComponentResult result;
            if (isAsync)
            {
                result = await InvokeAsyncCore(context);
            }
            else
            {
                // We support falling back to synchronous if there is no InvokeAsync method, in this case we'll still
                // execute the IViewResult asynchronously.
                result = InvokeSyncCore(context);
            }

            await result.ExecuteAsync(context);
        }

        private async Task<IViewComponentResult> InvokeAsyncCore(ViewComponentContext context)
        {
            var component = _viewComponentFactory.CreateViewComponent(context);

            using (_logger.ViewComponentScope(context))
            {
                var method = context.ViewComponentDescriptor.MethodInfo;
                var arguments = ControllerActionExecutor.PrepareArguments(context.Arguments, method.GetParameters());

                _diagnosticSource.BeforeViewComponent(context, component);
                _logger.ViewComponentExecuting(context, arguments);

                var startTimestamp = _logger.IsEnabled(LogLevel.Debug) ? Stopwatch.GetTimestamp() : 0;
                var result = await ControllerActionExecutor.ExecuteAsync(method, component, arguments);

                var viewComponentResult = CoerceToViewComponentResult(result);
                _logger.ViewComponentExecuted(context, startTimestamp, viewComponentResult);
                _diagnosticSource.AfterViewComponent(context, viewComponentResult, component);

                _viewComponentFactory.ReleaseViewComponent(context, component);

                return viewComponentResult;
            }
        }

        private IViewComponentResult InvokeSyncCore(ViewComponentContext context)
        {
            var component = _viewComponentFactory.CreateViewComponent(context);

            using (_logger.ViewComponentScope(context))
            {
                var method = context.ViewComponentDescriptor.MethodInfo;
                var arguments = ControllerActionExecutor.PrepareArguments(
                    context.Arguments,
                    method.GetParameters());

                _diagnosticSource.BeforeViewComponent(context, component);
                _logger.ViewComponentExecuting(context, arguments);

                var startTimestamp = _logger.IsEnabled(LogLevel.Debug) ? Stopwatch.GetTimestamp() : 0;
                object result;
                try
                {
                    result = method.Invoke(component, arguments);
                }
                catch (TargetInvocationException ex)
                {
                    _viewComponentFactory.ReleaseViewComponent(context, component);

                    // Preserve callstack of any user-thrown exceptions.
                    var exceptionInfo = ExceptionDispatchInfo.Capture(ex.InnerException);
                    exceptionInfo.Throw();
                    return null; // Unreachable
                }

                var viewComponentResult = CoerceToViewComponentResult(result);
                _logger.ViewComponentExecuted(context, startTimestamp, viewComponentResult);
                _diagnosticSource.AfterViewComponent(context, viewComponentResult, component);

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

            var componentResult = value as IViewComponentResult;
            if (componentResult != null)
            {
                return componentResult;
            }

            var stringResult = value as string;
            if (stringResult != null)
            {
                return new ContentViewComponentResult(stringResult);
            }

            var htmlContent = value as IHtmlContent;
            if (htmlContent != null)
            {
                return new HtmlContentViewComponentResult(htmlContent);
            }

            throw new InvalidOperationException(Resources.FormatViewComponent_InvalidReturnValue(
                typeof(string).Name,
                typeof(IHtmlContent).Name,
                typeof(IViewComponentResult).Name));
        }
    }
}