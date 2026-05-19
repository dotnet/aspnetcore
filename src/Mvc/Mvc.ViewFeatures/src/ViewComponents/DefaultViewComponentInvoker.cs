// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections;
using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ViewComponents;

/// <summary>
/// Default implementation for <see cref="IViewComponentInvoker"/>.
/// </summary>
internal sealed partial class DefaultViewComponentInvoker : IViewComponentInvoker
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
        ArgumentNullException.ThrowIfNull(viewComponentFactory);
        ArgumentNullException.ThrowIfNull(viewComponentInvokerCache);
        ArgumentNullException.ThrowIfNull(diagnosticListener);
        ArgumentNullException.ThrowIfNull(logger);

        _viewComponentFactory = viewComponentFactory;
        _viewComponentInvokerCache = viewComponentInvokerCache;
        _diagnosticListener = diagnosticListener;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task InvokeAsync(ViewComponentContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var executor = _viewComponentInvokerCache.GetViewComponentMethodExecutor(context);

        var returnType = executor.MethodReturnType;

        if (returnType == typeof(void) || returnType == typeof(Task))
        {
            throw new InvalidOperationException(Resources.ViewComponent_MustReturnValue);
        }

        IViewComponentResult result;
        object? component = null;
        try
        {
            component = _viewComponentFactory.CreateViewComponent(context);
            if (executor.IsMethodAsync)
            {
                result = await InvokeAsyncCore(executor, component, context);
            }
            else
            {
                // We support falling back to synchronous if there is no InvokeAsync method, in this case we'll still
                // execute the IViewResult asynchronously.
                result = InvokeSyncCore(executor, component, context);
            }
        }
        finally
        {
            if (component != null)
            {
                await _viewComponentFactory.ReleaseViewComponentAsync(context, executor);
            }
        }

        await result.ExecuteAsync(context);
    }

    private async Task<IViewComponentResult> InvokeAsyncCore(ObjectMethodExecutor executor, object component, ViewComponentContext context)
    {
        using (Log.ViewComponentScope(_logger, context))
        {
            var arguments = PrepareArguments(context.Arguments, executor);

            _diagnosticListener.BeforeViewComponent(context, component);
            Log.ViewComponentExecuting(_logger, context, arguments);

            var stopwatch = ValueStopwatch.StartNew();

            object resultAsObject;
            var returnType = executor.MethodReturnType;

            if (returnType == typeof(Task<IViewComponentResult>))
            {
                var task = executor.Execute(component, arguments);
                if (task is null)
                {
                    throw new InvalidOperationException(Resources.ViewComponent_MustReturnValue);
                }

                resultAsObject = await (Task<IViewComponentResult>)task;
            }
            else if (returnType == typeof(Task<string>))
            {
                var task = executor.Execute(component, arguments);
                if (task is null)
                {
                    throw new InvalidOperationException(Resources.ViewComponent_MustReturnValue);
                }

                resultAsObject = await (Task<string>)task;
            }
            else if (returnType == typeof(Task<IHtmlContent>))
            {
                var task = executor.Execute(component, arguments);
                if (task is null)
                {
                    throw new InvalidOperationException(Resources.ViewComponent_MustReturnValue);
                }

                resultAsObject = await (Task<IHtmlContent>)task;
            }
            else
            {
                resultAsObject = await executor.ExecuteAsync(component, arguments);
            }

            var viewComponentResult = CoerceToViewComponentResult(resultAsObject);
            Log.ViewComponentExecuted(_logger, context, stopwatch.GetElapsedTime(), viewComponentResult);
            _diagnosticListener.AfterViewComponent(context, viewComponentResult, component);

            return viewComponentResult;
        }
    }

    private IViewComponentResult InvokeSyncCore(ObjectMethodExecutor executor, object component, ViewComponentContext context)
    {
        using (Log.ViewComponentScope(_logger, context))
        {
            var arguments = PrepareArguments(context.Arguments, executor);

            _diagnosticListener.BeforeViewComponent(context, component);
            Log.ViewComponentExecuting(_logger, context, arguments);

            var stopwatch = ValueStopwatch.StartNew();
            object? result;

            result = executor.Execute(component, arguments);

            var viewComponentResult = CoerceToViewComponentResult(result);
            Log.ViewComponentExecuted(_logger, context, stopwatch.GetElapsedTime(), viewComponentResult);
            _diagnosticListener.AfterViewComponent(context, viewComponentResult, component);

            return viewComponentResult;
        }
    }

    private static IViewComponentResult CoerceToViewComponentResult(object? value)
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

    private static object?[]? PrepareArguments(
        IDictionary<string, object?> parameters,
        ObjectMethodExecutor objectMethodExecutor)
    {
        var declaredParameterInfos = objectMethodExecutor.MethodParameters;
        var count = declaredParameterInfos.Length;
        if (count == 0)
        {
            return null;
        }

        var arguments = new object?[count];
        for (var index = 0; index < count; index++)
        {
            var parameterInfo = declaredParameterInfos[index];

            if (!parameters.TryGetValue(parameterInfo.Name!, out var value))
            {
                value = objectMethodExecutor.GetDefaultValueForParameter(index);
            }

            arguments[index] = value;
        }

        return arguments;
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Executing view component {ViewComponentName} with arguments ({Arguments}).", EventName = "ViewComponentExecuting", SkipEnabledCheck = true)]
        private static partial void ViewComponentExecuting(ILogger logger, string viewComponentName, string[] arguments);

        [LoggerMessage(2, LogLevel.Debug, "Executed view component {ViewComponentName} in {ElapsedMilliseconds}ms and returned {ViewComponentResult}", EventName = "ViewComponentExecuted", SkipEnabledCheck = true)]
        private static partial void ViewComponentExecuted(ILogger logger, string viewComponentName, double elapsedMilliseconds, string? viewComponentResult);

        public static IDisposable? ViewComponentScope(ILogger logger, ViewComponentContext context)
        {
            return logger.BeginScope(new ViewComponentLogScope(context.ViewComponentDescriptor));
        }

#nullable restore
        public static void ViewComponentExecuting(
            ILogger logger,
            ViewComponentContext context,
            object[] arguments)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                var formattedArguments = GetFormattedArguments(arguments);
                ViewComponentExecuting(logger, context.ViewComponentDescriptor.DisplayName, formattedArguments);
            }
        }

        public static void ViewComponentExecuted(
            ILogger logger,
            ViewComponentContext context,
            TimeSpan timespan,
            object result)
        {
            // Don't log if logging wasn't enabled at start of request as time will be wildly wrong.
            if (logger.IsEnabled(LogLevel.Debug))
            {
                ViewComponentExecuted(
                    logger,
                    context.ViewComponentDescriptor.DisplayName,
                    timespan.TotalMilliseconds,
                    Convert.ToString(result, CultureInfo.InvariantCulture));
            }
        }

        private static string[] GetFormattedArguments(object[] arguments)
        {
            if (arguments == null || arguments.Length == 0)
            {
                return Array.Empty<string>();
            }

            var formattedArguments = new string[arguments.Length];
            for (var i = 0; i < formattedArguments.Length; i++)
            {
                formattedArguments[i] = Convert.ToString(arguments[i], CultureInfo.InvariantCulture);
            }

            return formattedArguments;
        }

        private sealed class ViewComponentLogScope : IReadOnlyList<KeyValuePair<string, object>>
        {
            private readonly ViewComponentDescriptor _descriptor;

            public ViewComponentLogScope(ViewComponentDescriptor descriptor)
            {
                _descriptor = descriptor;
            }

            public KeyValuePair<string, object> this[int index]
            {
                get
                {
                    if (index == 0)
                    {
                        return new KeyValuePair<string, object>("ViewComponentName", _descriptor.DisplayName);
                    }
                    else if (index == 1)
                    {
                        return new KeyValuePair<string, object>("ViewComponentId", _descriptor.Id);
                    }
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
            }

            public int Count => 2;

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                for (var i = 0; i < Count; ++i)
                {
                    yield return this[i];
                }
            }

            public override string ToString()
            {
                return _descriptor.DisplayName;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
