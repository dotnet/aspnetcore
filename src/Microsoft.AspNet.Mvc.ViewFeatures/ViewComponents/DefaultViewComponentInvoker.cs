// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNet.Html;
using Microsoft.AspNet.Mvc.Controllers;
using Microsoft.AspNet.Mvc.Diagnostics;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.AspNet.Mvc.ViewFeatures.Logging;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.ViewComponents
{
    /// <summary>
    /// Default implementation for <see cref="IViewComponentInvoker"/>.
    /// </summary>
    public class DefaultViewComponentInvoker : IViewComponentInvoker
    {
        private readonly ITypeActivatorCache _typeActivatorCache;
        private readonly IViewComponentActivator _viewComponentActivator;
        private readonly DiagnosticSource _diagnosticSource;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultViewComponentInvoker"/>.
        /// </summary>
        /// <param name="typeActivatorCache">Caches factories for instantiating view component instances.</param>
        /// <param name="viewComponentActivator">The <see cref="IViewComponentActivator"/>.</param>
        /// <param name="diagnosticSource">The <see cref="DiagnosticSource"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public DefaultViewComponentInvoker(
            ITypeActivatorCache typeActivatorCache,
            IViewComponentActivator viewComponentActivator,
            DiagnosticSource diagnosticSource,
            ILogger logger)
        {
            if (typeActivatorCache == null)
            {
                throw new ArgumentNullException(nameof(typeActivatorCache));
            }

            if (viewComponentActivator == null)
            {
                throw new ArgumentNullException(nameof(viewComponentActivator));
            }

            if (diagnosticSource == null)
            {
                throw new ArgumentNullException(nameof(diagnosticSource));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _typeActivatorCache = typeActivatorCache;
            _viewComponentActivator = viewComponentActivator;
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

        private object CreateComponent(ViewComponentContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var services = context.ViewContext.HttpContext.RequestServices;
            var component = _typeActivatorCache.CreateInstance<object>(
                services,
                context.ViewComponentDescriptor.Type);
            _viewComponentActivator.Activate(component, context);
            return component;
        }

        private async Task<IViewComponentResult> InvokeAsyncCore(ViewComponentContext context)
        {
            var component = CreateComponent(context);

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

                return viewComponentResult;
            }
        }

        private IViewComponentResult InvokeSyncCore(ViewComponentContext context)
        {
            var component = CreateComponent(context);

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
                    // Preserve callstack of any user-thrown exceptions.
                    var exceptionInfo = ExceptionDispatchInfo.Capture(ex.InnerException);
                    exceptionInfo.Throw();
                    return null; // Unreachable
                }

                var viewComponentResult = CoerceToViewComponentResult(result);
                _logger.ViewComponentExecuted(context, startTimestamp, viewComponentResult);
                _diagnosticSource.AfterViewComponent(context, viewComponentResult, component);

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