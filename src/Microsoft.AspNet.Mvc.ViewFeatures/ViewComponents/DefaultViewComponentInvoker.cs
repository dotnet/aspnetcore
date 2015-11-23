// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNet.Html.Abstractions;
using Microsoft.AspNet.Mvc.Controllers;
using Microsoft.AspNet.Mvc.Diagnostics;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.AspNet.Mvc.ViewFeatures.Logging;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.ViewComponents
{
    public class DefaultViewComponentInvoker : IViewComponentInvoker
    {
        private readonly ITypeActivatorCache _typeActivatorCache;
        private readonly IViewComponentActivator _viewComponentActivator;
        private readonly DiagnosticSource _diagnosticSource;
        private readonly ILogger _logger;

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

        public void Invoke(ViewComponentContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var method = ViewComponentMethodSelector.FindSyncMethod(
                context.ViewComponentDescriptor.Type.GetTypeInfo(),
                context.Arguments);
            if (method == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatViewComponent_CannotFindMethod(ViewComponentMethodSelector.SyncMethodName));
            }

            var result = InvokeSyncCore(method, context);

            result.Execute(context);
        }

        public async Task InvokeAsync(ViewComponentContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            IViewComponentResult result;

            var asyncMethod = ViewComponentMethodSelector.FindAsyncMethod(
                context.ViewComponentDescriptor.Type.GetTypeInfo(),
                context.Arguments);
            if (asyncMethod == null)
            {
                // We support falling back to synchronous if there is no InvokeAsync method, in this case we'll still
                // execute the IViewResult asynchronously.
                var syncMethod = ViewComponentMethodSelector.FindSyncMethod(
                    context.ViewComponentDescriptor.Type.GetTypeInfo(),
                    context.Arguments);
                if (syncMethod == null)
                {
                    throw new InvalidOperationException(
                        Resources.FormatViewComponent_CannotFindMethod_WithFallback(
                        ViewComponentMethodSelector.SyncMethodName, ViewComponentMethodSelector.AsyncMethodName));
                }
                else
                {
                    result = InvokeSyncCore(syncMethod, context);
                }
            }
            else
            {
                result = await InvokeAsyncCore(asyncMethod, context);
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

        private async Task<IViewComponentResult> InvokeAsyncCore(
            MethodInfo method,
            ViewComponentContext context)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var component = CreateComponent(context);

            using (_logger.ViewComponentScope(context))
            {
                _diagnosticSource.BeforeViewComponent(context, component);
                _logger.ViewComponentExecuting(context);

                var startTime = Environment.TickCount;
                var result = await ControllerActionExecutor.ExecuteAsync(method, component, context.Arguments);

                var viewComponentResult = CoerceToViewComponentResult(result);
                _logger.ViewComponentExecuted(context, startTime, viewComponentResult);
                _diagnosticSource.AfterViewComponent(context, viewComponentResult, component);

                return viewComponentResult;
            }
        }

        public IViewComponentResult InvokeSyncCore(MethodInfo method, ViewComponentContext context)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var component = CreateComponent(context);

            using (_logger.ViewComponentScope(context))
            {
                _diagnosticSource.BeforeViewComponent(context, component);
                _logger.ViewComponentExecuting(context);

                try
                {
                    var startTime = Environment.TickCount;
                    var result = method.Invoke(component, context.Arguments);

                    var viewComponentResult = CoerceToViewComponentResult(result);
                    _logger.ViewComponentExecuted(context, startTime, viewComponentResult);
                    _diagnosticSource.AfterViewComponent(context, viewComponentResult, component);

                    return viewComponentResult;
                }
                catch (TargetInvocationException ex)
                {
                    // Preserve callstack of any user-thrown exceptions.
                    var exceptionInfo = ExceptionDispatchInfo.Capture(ex.InnerException);
                    exceptionInfo.Throw();
                    return null; // Unreachable
                }
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