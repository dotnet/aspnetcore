// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;
using System.Globalization;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Resources = Microsoft.AspNetCore.Mvc.Core.Resources;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

#pragma warning disable CA1852 // Seal internal types
internal partial class ControllerActionInvoker : ResourceInvoker, IActionInvoker
#pragma warning restore CA1852 // Seal internal types
{
    private readonly ControllerActionInvokerCacheEntry _cacheEntry;
    private readonly ControllerContext _controllerContext;

    private Dictionary<string, object?>? _arguments;

    private ActionExecutingContextSealed? _actionExecutingContext;
    private ActionExecutedContextSealed? _actionExecutedContext;

    internal ControllerActionInvoker(
        ILogger logger,
        DiagnosticListener diagnosticListener,
        IActionContextAccessor actionContextAccessor,
        IActionResultTypeMapper mapper,
        ControllerContext controllerContext,
        ControllerActionInvokerCacheEntry cacheEntry,
        IFilterMetadata[] filters)
        : base(diagnosticListener, logger, actionContextAccessor, mapper, controllerContext, filters, controllerContext.ValueProviderFactories)
    {
        ArgumentNullException.ThrowIfNull(cacheEntry);

        _cacheEntry = cacheEntry;
        _controllerContext = controllerContext;
    }

    // Internal for testing
    internal ControllerContext ControllerContext => _controllerContext;

    protected override ValueTask ReleaseResources()
    {
        if (_instance != null && _cacheEntry.ControllerReleaser != null)
        {
            return _cacheEntry.ControllerReleaser(_controllerContext, _instance);
        }

        return default;
    }

    private Task Next(ref State next, ref Scope scope, ref object? state, ref bool isCompleted)
    {
        switch (next)
        {
            case State.ActionBegin:
                {
                    var controllerContext = _controllerContext;

                    _cursor.Reset();
                    Log.ExecutingControllerFactory(_logger, controllerContext);

                    _instance = _cacheEntry.ControllerFactory(controllerContext);
                    Log.ExecutedControllerFactory(_logger, controllerContext);

                    _arguments = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                    var task = BindArgumentsAsync();
                    if (task.Status != TaskStatus.RanToCompletion)
                    {
                        next = State.ActionNext;
                        return task;
                    }

                    goto case State.ActionNext;
                }

            case State.ActionNext:
                {
                    var current = _cursor.GetNextFilter<IActionFilter, IAsyncActionFilter>();
                    if (current.FilterAsync != null)
                    {
                        if (_actionExecutingContext == null)
                        {
                            _actionExecutingContext = new ActionExecutingContextSealed(_controllerContext, _filters, _arguments!, _instance!);
                        }

                        state = current.FilterAsync;
                        goto case State.ActionAsyncBegin;
                    }
                    else if (current.Filter != null)
                    {
                        if (_actionExecutingContext == null)
                        {
                            _actionExecutingContext = new ActionExecutingContextSealed(_controllerContext, _filters, _arguments!, _instance!);
                        }

                        state = current.Filter;
                        goto case State.ActionSyncBegin;
                    }
                    else
                    {
                        goto case State.ActionInside;
                    }
                }

            case State.ActionAsyncBegin:
                {
                    Debug.Assert(state != null);
                    Debug.Assert(_actionExecutingContext != null);

                    var filter = (IAsyncActionFilter)state;
                    var actionExecutingContext = _actionExecutingContext;

                    _diagnosticListener.BeforeOnActionExecution(actionExecutingContext, filter);
                    _logger.BeforeExecutingMethodOnFilter(
                        MvcCoreLoggerExtensions.ActionFilter,
                        nameof(IAsyncActionFilter.OnActionExecutionAsync),
                        filter);

                    var task = filter.OnActionExecutionAsync(actionExecutingContext, InvokeNextActionFilterAwaitedAsync);
                    if (task.Status != TaskStatus.RanToCompletion)
                    {
                        next = State.ActionAsyncEnd;
                        return task;
                    }

                    goto case State.ActionAsyncEnd;
                }

            case State.ActionAsyncEnd:
                {
                    Debug.Assert(state != null);
                    Debug.Assert(_actionExecutingContext != null);

                    var filter = (IAsyncActionFilter)state;

                    if (_actionExecutedContext == null)
                    {
                        // If we get here then the filter didn't call 'next' indicating a short circuit.
                        _logger.ActionFilterShortCircuited(filter);

                        _actionExecutedContext = new ActionExecutedContextSealed(
                            _controllerContext,
                            _filters,
                            _instance!)
                        {
                            Canceled = true,
                            Result = _actionExecutingContext.Result,
                        };
                    }

                    _diagnosticListener.AfterOnActionExecution(_actionExecutedContext, filter);
                    _logger.AfterExecutingMethodOnFilter(
                        MvcCoreLoggerExtensions.ActionFilter,
                        nameof(IAsyncActionFilter.OnActionExecutionAsync),
                        filter);

                    goto case State.ActionEnd;
                }

            case State.ActionSyncBegin:
                {
                    Debug.Assert(state != null);
                    Debug.Assert(_actionExecutingContext != null);

                    var filter = (IActionFilter)state;
                    var actionExecutingContext = _actionExecutingContext;

                    _diagnosticListener.BeforeOnActionExecuting(actionExecutingContext, filter);
                    _logger.BeforeExecutingMethodOnFilter(
                        MvcCoreLoggerExtensions.ActionFilter,
                        nameof(IActionFilter.OnActionExecuting),
                        filter);

                    filter.OnActionExecuting(actionExecutingContext);

                    _diagnosticListener.AfterOnActionExecuting(actionExecutingContext, filter);
                    _logger.AfterExecutingMethodOnFilter(
                        MvcCoreLoggerExtensions.ActionFilter,
                        nameof(IActionFilter.OnActionExecuting),
                        filter);

                    if (actionExecutingContext.Result != null)
                    {
                        // Short-circuited by setting a result.
                        _logger.ActionFilterShortCircuited(filter);

                        _actionExecutedContext = new ActionExecutedContextSealed(
                            _actionExecutingContext,
                            _filters,
                            _instance!)
                        {
                            Canceled = true,
                            Result = _actionExecutingContext.Result,
                        };

                        goto case State.ActionEnd;
                    }

                    var task = InvokeNextActionFilterAsync();
                    if (task.Status != TaskStatus.RanToCompletion)
                    {
                        next = State.ActionSyncEnd;
                        return task;
                    }

                    goto case State.ActionSyncEnd;
                }

            case State.ActionSyncEnd:
                {
                    Debug.Assert(state != null);
                    Debug.Assert(_actionExecutingContext != null);
                    Debug.Assert(_actionExecutedContext != null);

                    var filter = (IActionFilter)state;
                    var actionExecutedContext = _actionExecutedContext;

                    _diagnosticListener.BeforeOnActionExecuted(actionExecutedContext, filter);
                    _logger.BeforeExecutingMethodOnFilter(
                        MvcCoreLoggerExtensions.ActionFilter,
                        nameof(IActionFilter.OnActionExecuted),
                        filter);

                    filter.OnActionExecuted(actionExecutedContext);

                    _diagnosticListener.AfterOnActionExecuted(actionExecutedContext, filter);
                    _logger.AfterExecutingMethodOnFilter(
                        MvcCoreLoggerExtensions.ActionFilter,
                        nameof(IActionFilter.OnActionExecuted),
                        filter);

                    goto case State.ActionEnd;
                }

            case State.ActionInside:
                {
                    var task = InvokeActionMethodAsync();
                    if (task.Status != TaskStatus.RanToCompletion)
                    {
                        next = State.ActionEnd;
                        return task;
                    }

                    goto case State.ActionEnd;
                }

            case State.ActionEnd:
                {
                    if (scope == Scope.Action)
                    {
                        if (_actionExecutedContext == null)
                        {
                            _actionExecutedContext = new ActionExecutedContextSealed(_controllerContext, _filters, _instance!)
                            {
                                Result = _result,
                            };
                        }

                        isCompleted = true;
                        return Task.CompletedTask;
                    }

                    var actionExecutedContext = _actionExecutedContext;
                    Rethrow(actionExecutedContext);

                    if (actionExecutedContext != null)
                    {
                        _result = actionExecutedContext.Result;
                    }

                    isCompleted = true;
                    return Task.CompletedTask;
                }

            default:
                throw new InvalidOperationException();
        }
    }

    private Task InvokeNextActionFilterAsync()
    {
        try
        {
            var next = State.ActionNext;
            var state = (object?)null;
            var scope = Scope.Action;
            var isCompleted = false;
            while (!isCompleted)
            {
                var lastTask = Next(ref next, ref scope, ref state, ref isCompleted);
                if (!lastTask.IsCompletedSuccessfully)
                {
                    return Awaited(this, lastTask, next, scope, state, isCompleted);
                }
            }
        }
        catch (Exception exception)
        {
            _actionExecutedContext = new ActionExecutedContextSealed(_controllerContext, _filters, _instance!)
            {
                ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception),
            };
        }

        Debug.Assert(_actionExecutedContext != null);
        return Task.CompletedTask;

        static async Task Awaited(ControllerActionInvoker invoker, Task lastTask, State next, Scope scope, object? state, bool isCompleted)
        {
            try
            {
                await lastTask;

                while (!isCompleted)
                {
                    await invoker.Next(ref next, ref scope, ref state, ref isCompleted);
                }
            }
            catch (Exception exception)
            {
                invoker._actionExecutedContext = new ActionExecutedContextSealed(invoker._controllerContext, invoker._filters, invoker._instance!)
                {
                    ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception),
                };
            }

            Debug.Assert(invoker._actionExecutedContext != null);
        }
    }

    private Task<ActionExecutedContext> InvokeNextActionFilterAwaitedAsync()
    {
        Debug.Assert(_actionExecutingContext != null);
        if (_actionExecutingContext.Result != null)
        {
            // If we get here, it means that an async filter set a result AND called next(). This is forbidden.
            return Throw();
        }

        var task = InvokeNextActionFilterAsync();
        if (!task.IsCompletedSuccessfully)
        {
            return Awaited(this, task);
        }

        Debug.Assert(_actionExecutedContext != null);
        return Task.FromResult<ActionExecutedContext>(_actionExecutedContext);

        static async Task<ActionExecutedContext> Awaited(ControllerActionInvoker invoker, Task task)
        {
            await task;

            Debug.Assert(invoker._actionExecutedContext != null);
            return invoker._actionExecutedContext;
        }
#pragma warning disable CS1998
        static async Task<ActionExecutedContext> Throw()
        {
            var message = Resources.FormatAsyncActionFilter_InvalidShortCircuit(
                typeof(IAsyncActionFilter).Name,
                nameof(ActionExecutingContext.Result),
                typeof(ActionExecutingContext).Name,
                typeof(ActionExecutionDelegate).Name);

            throw new InvalidOperationException(message);
        }
#pragma warning restore CS1998
    }

    private Task InvokeActionMethodAsync()
    {
        if (_diagnosticListener.IsEnabled() || _logger.IsEnabled(LogLevel.Trace))
        {
            return Logged(this);
        }

        var objectMethodExecutor = _cacheEntry.ObjectMethodExecutor;
        var actionMethodExecutor = _cacheEntry.ActionMethodExecutor;
        var orderedArguments = PrepareArguments(_arguments, objectMethodExecutor);

        var actionResultValueTask = actionMethodExecutor.Execute(ControllerContext, _mapper, objectMethodExecutor, _instance!, orderedArguments);
        if (actionResultValueTask.IsCompletedSuccessfully)
        {
            _result = actionResultValueTask.Result;
        }
        else
        {
            return Awaited(this, actionResultValueTask);
        }

        return Task.CompletedTask;

        static async Task Awaited(ControllerActionInvoker invoker, ValueTask<IActionResult> actionResultValueTask)
        {
            invoker._result = await actionResultValueTask;
        }

        static async Task Logged(ControllerActionInvoker invoker)
        {
            var controllerContext = invoker._controllerContext;
            var objectMethodExecutor = invoker._cacheEntry.ObjectMethodExecutor;
            var controller = invoker._instance;
            var arguments = invoker._arguments;
            var actionMethodExecutor = invoker._cacheEntry.ActionMethodExecutor;
            var orderedArguments = PrepareArguments(arguments, objectMethodExecutor);

            var diagnosticListener = invoker._diagnosticListener;
            var logger = invoker._logger;

            IActionResult? result = null;
            try
            {
                diagnosticListener.BeforeControllerActionMethod(
                    controllerContext,
                    arguments,
                    controller);
                Log.ActionMethodExecuting(logger, controllerContext, orderedArguments);
                var stopwatch = ValueStopwatch.StartNew();
                var actionResultValueTask = actionMethodExecutor.Execute(controllerContext, invoker._mapper, objectMethodExecutor, controller!, orderedArguments);
                if (actionResultValueTask.IsCompletedSuccessfully)
                {
                    result = actionResultValueTask.Result;
                }
                else
                {
                    result = await actionResultValueTask;
                }

                invoker._result = result;
                Log.ActionMethodExecuted(logger, controllerContext, result, stopwatch.GetElapsedTime());
            }
            finally
            {
                diagnosticListener.AfterControllerActionMethod(
                    controllerContext,
                    arguments,
                    controllerContext,
                    result);
            }
        }
    }

    /// <remarks><see cref="ResourceInvoker.InvokeFilterPipelineAsync"/> for details on what the
    /// variables in this method represent.</remarks>
    protected override Task InvokeInnerFilterAsync()
    {
        try
        {
            var next = State.ActionBegin;
            var scope = Scope.Invoker;
            var state = (object?)null;
            var isCompleted = false;

            while (!isCompleted)
            {
                var lastTask = Next(ref next, ref scope, ref state, ref isCompleted);
                if (!lastTask.IsCompletedSuccessfully)
                {
                    return Awaited(this, lastTask, next, scope, state, isCompleted);
                }
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            // Wrap non task-wrapped exceptions in a Task,
            // as this isn't done automatically since the method is not async.
            return Task.FromException(ex);
        }

        static async Task Awaited(ControllerActionInvoker invoker, Task lastTask, State next, Scope scope, object? state, bool isCompleted)
        {
            await lastTask;

            while (!isCompleted)
            {
                await invoker.Next(ref next, ref scope, ref state, ref isCompleted);
            }
        }
    }

    private static void Rethrow(ActionExecutedContextSealed? context)
    {
        if (context == null)
        {
            return;
        }

        if (context.ExceptionHandled)
        {
            return;
        }

        context.ExceptionDispatchInfo?.Throw();

        if (context.Exception != null)
        {
            throw context.Exception;
        }
    }

    private Task BindArgumentsAsync()
    {
        // Perf: Avoid allocating async state machines where possible. We only need the state
        // machine if you need to bind properties or arguments.
        var actionDescriptor = _controllerContext.ActionDescriptor;
        if (actionDescriptor.BoundProperties.Count == 0 &&
            actionDescriptor.Parameters.Count == 0)
        {
            return Task.CompletedTask;
        }

        Debug.Assert(_cacheEntry.ControllerBinderDelegate != null);
        return _cacheEntry.ControllerBinderDelegate(_controllerContext, _instance!, _arguments!);
    }

    private static object?[]? PrepareArguments(
        IDictionary<string, object?>? actionParameters,
        ObjectMethodExecutor actionMethodExecutor)
    {
        var declaredParameterInfos = actionMethodExecutor.MethodParameters;
        var count = declaredParameterInfos.Length;
        if (count == 0)
        {
            return null;
        }

        Debug.Assert(actionParameters != null, "Expect arguments to be initialized.");

        var arguments = new object?[count];
        for (var index = 0; index < count; index++)
        {
            var parameterInfo = declaredParameterInfos[index];

            if (!actionParameters.TryGetValue(parameterInfo.Name!, out var value) || value is null)
            {
                value = actionMethodExecutor.GetDefaultValueForParameter(index);
            }

            arguments[index] = value;
        }

        return arguments;
    }

    private enum Scope
    {
        Invoker,
        Action,
    }

    private enum State
    {
        ActionBegin,
        ActionNext,
        ActionAsyncBegin,
        ActionAsyncEnd,
        ActionSyncBegin,
        ActionSyncEnd,
        ActionInside,
        ActionEnd,
    }

    private sealed class ActionExecutingContextSealed : ActionExecutingContext
    {
        public ActionExecutingContextSealed(ActionContext actionContext, IList<IFilterMetadata> filters, IDictionary<string, object?> actionArguments, object controller) : base(actionContext, filters, actionArguments, controller) { }
    }

    private sealed class ActionExecutedContextSealed : ActionExecutedContext
    {
        public ActionExecutedContextSealed(ActionContext actionContext, IList<IFilterMetadata> filters, object controller) : base(actionContext, filters, controller) { }
    }

    // Internal for unit testing
    internal static new partial class Log
    {
        public static void ExecutingControllerFactory(ILogger logger, ControllerContext context)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            var controllerType = context.ActionDescriptor.ControllerTypeInfo.AsType();
            var controllerName = TypeNameHelper.GetTypeDisplayName(controllerType);
            ExecutingControllerFactory(logger, controllerName, controllerType.Assembly.GetName().Name);
        }

        [LoggerMessage(1, LogLevel.Debug, "Executing controller factory for controller {Controller} ({AssemblyName})", EventName = "ControllerFactoryExecuting", SkipEnabledCheck = true)]
        private static partial void ExecutingControllerFactory(ILogger logger, string controller, string? assemblyName);

        public static void ExecutedControllerFactory(ILogger logger, ControllerContext context)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            var controllerType = context.ActionDescriptor.ControllerTypeInfo.AsType();
            var controllerName = TypeNameHelper.GetTypeDisplayName(controllerType);
            ExecutedControllerFactory(logger, controllerName, controllerType.Assembly.GetName().Name);
        }

        [LoggerMessage(2, LogLevel.Debug, "Executed controller factory for controller {Controller} ({AssemblyName})", EventName = "ControllerFactoryExecuted", SkipEnabledCheck = true)]
        private static partial void ExecutedControllerFactory(ILogger logger, string controller, string? assemblyName);

        public static void ActionMethodExecuting(ILogger logger, ControllerContext context, object?[]? arguments)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                var actionName = context.ActionDescriptor.DisplayName;

                var validationState = context.ModelState.ValidationState;
                ActionMethodExecuting(logger, actionName, validationState);

                if (arguments != null && logger.IsEnabled(LogLevel.Trace))
                {
                    var convertedArguments = new string?[arguments.Length];
                    for (var i = 0; i < arguments.Length; i++)
                    {
                        convertedArguments[i] = Convert.ToString(arguments[i], CultureInfo.InvariantCulture);
                    }

                    ActionMethodExecutingWithArguments(logger, actionName, convertedArguments);
                }
            }
        }

        [LoggerMessage(101, LogLevel.Information, "Executing action method {ActionName} - Validation state: {ValidationState}", EventName = "ActionMethodExecuting", SkipEnabledCheck = true)]
        private static partial void ActionMethodExecuting(ILogger logger, string? actionName, ModelValidationState validationState);

        [LoggerMessage(102, LogLevel.Trace, "Executing action method {ActionName} with arguments ({Arguments})", EventName = "ActionMethodExecutingWithArguments", SkipEnabledCheck = true)]
        private static partial void ActionMethodExecutingWithArguments(ILogger logger, string? actionName, string?[] arguments);

        public static void ActionMethodExecuted(ILogger logger, ControllerContext context, IActionResult result, TimeSpan timeSpan)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                var actionName = context.ActionDescriptor.DisplayName;
                ActionMethodExecuted(logger, actionName, Convert.ToString(result, CultureInfo.InvariantCulture), timeSpan.TotalMilliseconds);
            }
        }

        [LoggerMessage(103, LogLevel.Information, "Executed action method {ActionName}, returned result {ActionResult} in {ElapsedMilliseconds}ms.", EventName = "ActionMethodExecuted", SkipEnabledCheck = true)]
        private static partial void ActionMethodExecuted(ILogger logger, string? actionName, string? actionResult, double elapsedMilliseconds);
    }
}
