// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

internal abstract partial class ResourceInvoker
{
    protected readonly DiagnosticListener _diagnosticListener;
    protected readonly ILogger _logger;
    protected readonly IActionContextAccessor _actionContextAccessor;
    protected readonly IActionResultTypeMapper _mapper;
    protected readonly ActionContext _actionContext;
    protected readonly IFilterMetadata[] _filters;
    protected readonly IList<IValueProviderFactory> _valueProviderFactories;

    private AuthorizationFilterContextSealed? _authorizationContext;
    private ResourceExecutingContextSealed? _resourceExecutingContext;
    private ResourceExecutedContextSealed? _resourceExecutedContext;
    private ExceptionContextSealed? _exceptionContext;
    private ResultExecutingContextSealed? _resultExecutingContext;
    private ResultExecutedContextSealed? _resultExecutedContext;

    // Do not make this readonly, it's mutable. We don't want to make a copy.
    // https://blogs.msdn.microsoft.com/ericlippert/2008/05/14/mutating-readonly-structs/
    protected FilterCursor _cursor;
    protected IActionResult? _result;
    protected object? _instance;

    public ResourceInvoker(
        DiagnosticListener diagnosticListener,
        ILogger logger,
        IActionContextAccessor actionContextAccessor,
        IActionResultTypeMapper mapper,
        ActionContext actionContext,
        IFilterMetadata[] filters,
        IList<IValueProviderFactory> valueProviderFactories)
    {
        _diagnosticListener = diagnosticListener ?? throw new ArgumentNullException(nameof(diagnosticListener));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _actionContextAccessor = actionContextAccessor ?? throw new ArgumentNullException(nameof(actionContextAccessor));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _actionContext = actionContext ?? throw new ArgumentNullException(nameof(actionContext));

        _filters = filters ?? throw new ArgumentNullException(nameof(filters));
        _valueProviderFactories = valueProviderFactories ?? throw new ArgumentNullException(nameof(valueProviderFactories));
        _cursor = new FilterCursor(filters);
    }

    public virtual Task InvokeAsync()
    {
        if (_diagnosticListener.IsEnabled() || _logger.IsEnabled(LogLevel.Information))
        {
            return Logged(this);
        }

        _actionContextAccessor.ActionContext = _actionContext;
        var scope = _logger.ActionScope(_actionContext.ActionDescriptor);

        Task task;
        try
        {
            task = InvokeFilterPipelineAsync();
        }
        catch (Exception exception)
        {
            return Awaited(this, Task.FromException(exception), scope);
        }

        if (!task.IsCompletedSuccessfully)
        {
            return Awaited(this, task, scope);
        }

        return ReleaseResourcesCore(scope).AsTask();

        static async Task Awaited(ResourceInvoker invoker, Task task, IDisposable? scope)
        {
            try
            {
                await task;
            }
            finally
            {
                await invoker.ReleaseResourcesCore(scope);
            }
        }

        static async Task Logged(ResourceInvoker invoker)
        {
            var actionContext = invoker._actionContext;
            invoker._actionContextAccessor.ActionContext = actionContext;
            try
            {
                var logger = invoker._logger;

                invoker._diagnosticListener.BeforeAction(
                    actionContext.ActionDescriptor,
                    actionContext.HttpContext,
                    actionContext.RouteData);

                var actionScope = logger.ActionScope(actionContext.ActionDescriptor);

                logger.ExecutingAction(actionContext.ActionDescriptor);

                var filters = invoker._filters;
                logger.AuthorizationFiltersExecutionPlan(filters);
                logger.ResourceFiltersExecutionPlan(filters);
                logger.ActionFiltersExecutionPlan(filters);
                logger.ExceptionFiltersExecutionPlan(filters);
                logger.ResultFiltersExecutionPlan(filters);

                var stopwatch = ValueStopwatch.StartNew();

                try
                {
                    await invoker.InvokeFilterPipelineAsync();
                }
                finally
                {
                    await invoker.ReleaseResourcesCore(actionScope);
                    logger.ExecutedAction(actionContext.ActionDescriptor, stopwatch.GetElapsedTime());
                }
            }
            finally
            {
                invoker._diagnosticListener.AfterAction(
                    actionContext.ActionDescriptor,
                    actionContext.HttpContext,
                    actionContext.RouteData);
            }
        }
    }

    internal ValueTask ReleaseResourcesCore(IDisposable? scope)
    {
        Exception? releaseException = null;
        ValueTask releaseResult;
        try
        {
            releaseResult = ReleaseResources();
            if (!releaseResult.IsCompletedSuccessfully)
            {
                return HandleAsyncReleaseErrors(releaseResult, scope);
            }
        }
        catch (Exception exception)
        {
            releaseException = exception;
        }

        return HandleReleaseErrors(scope, releaseException);

        static async ValueTask HandleAsyncReleaseErrors(ValueTask releaseResult, IDisposable? scope)
        {
            Exception? releaseException = null;
            try
            {
                await releaseResult;
            }
            catch (Exception exception)
            {
                releaseException = exception;
            }

            await HandleReleaseErrors(scope, releaseException);
        }

        static ValueTask HandleReleaseErrors(IDisposable? scope, Exception? releaseException)
        {
            Exception? scopeException = null;
            try
            {
                scope?.Dispose();
            }
            catch (Exception exception)
            {
                scopeException = exception;
            }

            if (releaseException == null && scopeException == null)
            {
                return default;
            }
            else if (releaseException != null && scopeException != null)
            {
                return ValueTask.FromException(new AggregateException(releaseException, scopeException));
            }
            else if (releaseException != null)
            {
                return ValueTask.FromException(releaseException);
            }
            else
            {
                return ValueTask.FromException(scopeException!);
            }
        }
    }

    /// <summary>
    /// In derived types, releases resources such as controller, model, or page instances created as
    /// part of invoking the inner pipeline.
    /// </summary>
    protected abstract ValueTask ReleaseResources();

    private Task InvokeFilterPipelineAsync()
    {
        var next = State.InvokeBegin;

        // The `scope` tells the `Next` method who the caller is, and what kind of state to initialize to
        // communicate a result. The outermost scope is `Scope.Invoker` and doesn't require any type
        // of context or result other than throwing.
        var scope = Scope.Invoker;

        // The `state` is used for internal state handling during transitions between states. In practice this
        // means storing a filter instance in `state` and then retrieving it in the next state.
        var state = (object?)null;

        // `isCompleted` will be set to true when we've reached a terminal state.
        var isCompleted = false;
        try
        {
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

        static async Task Awaited(ResourceInvoker invoker, Task lastTask, State next, Scope scope, object? state, bool isCompleted)
        {
            await lastTask;

            while (!isCompleted)
            {
                await invoker.Next(ref next, ref scope, ref state, ref isCompleted);
            }
        }
    }

    protected abstract Task InvokeInnerFilterAsync();

    protected virtual Task InvokeResultAsync(IActionResult result)
    {
        if (_diagnosticListener.IsEnabled() || _logger.IsEnabled(LogLevel.Trace))
        {
            return Logged(this, result);
        }

        return result.ExecuteResultAsync(_actionContext);

        static async Task Logged(ResourceInvoker invoker, IActionResult result)
        {
            var actionContext = invoker._actionContext;

            invoker._diagnosticListener.BeforeActionResult(actionContext, result);
            Log.BeforeExecutingActionResult(invoker._logger, result);

            try
            {
                await result.ExecuteResultAsync(actionContext);
            }
            finally
            {
                invoker._diagnosticListener.AfterActionResult(actionContext, result);
                Log.AfterExecutingActionResult(invoker._logger, result);
            }
        }
    }

    private Task Next(ref State next, ref Scope scope, ref object? state, ref bool isCompleted)
    {
        switch (next)
        {
            case State.InvokeBegin:
                {
                    goto case State.AuthorizationBegin;
                }

            case State.AuthorizationBegin:
                {
                    _cursor.Reset();
                    goto case State.AuthorizationNext;
                }

            case State.AuthorizationNext:
                {
                    var current = _cursor.GetNextFilter<IAuthorizationFilter, IAsyncAuthorizationFilter>();
                    if (current.FilterAsync != null)
                    {
                        if (_authorizationContext == null)
                        {
                            _authorizationContext = new AuthorizationFilterContextSealed(_actionContext, _filters);
                        }

                        state = current.FilterAsync;
                        goto case State.AuthorizationAsyncBegin;
                    }
                    else if (current.Filter != null)
                    {
                        if (_authorizationContext == null)
                        {
                            _authorizationContext = new AuthorizationFilterContextSealed(_actionContext, _filters);
                        }

                        state = current.Filter;
                        goto case State.AuthorizationSync;
                    }
                    else
                    {
                        goto case State.AuthorizationEnd;
                    }
                }

            case State.AuthorizationAsyncBegin:
                {
                    Debug.Assert(state != null);
                    Debug.Assert(_authorizationContext != null);

                    var filter = (IAsyncAuthorizationFilter)state;
                    var authorizationContext = _authorizationContext;

                    _diagnosticListener.BeforeOnAuthorizationAsync(authorizationContext, filter);
                    _logger.BeforeExecutingMethodOnFilter(
                        FilterTypeConstants.AuthorizationFilter,
                        nameof(IAsyncAuthorizationFilter.OnAuthorizationAsync),
                        filter);

                    var task = filter.OnAuthorizationAsync(authorizationContext);
                    if (!task.IsCompletedSuccessfully)
                    {
                        next = State.AuthorizationAsyncEnd;
                        return task;
                    }

                    goto case State.AuthorizationAsyncEnd;
                }

            case State.AuthorizationAsyncEnd:
                {
                    Debug.Assert(state != null);
                    Debug.Assert(_authorizationContext != null);

                    var filter = (IAsyncAuthorizationFilter)state;
                    var authorizationContext = _authorizationContext;

                    _diagnosticListener.AfterOnAuthorizationAsync(authorizationContext, filter);
                    _logger.AfterExecutingMethodOnFilter(
                        FilterTypeConstants.AuthorizationFilter,
                        nameof(IAsyncAuthorizationFilter.OnAuthorizationAsync),
                        filter);

                    if (authorizationContext.Result != null)
                    {
                        goto case State.AuthorizationShortCircuit;
                    }

                    goto case State.AuthorizationNext;
                }

            case State.AuthorizationSync:
                {
                    Debug.Assert(state != null);
                    Debug.Assert(_authorizationContext != null);

                    var filter = (IAuthorizationFilter)state;
                    var authorizationContext = _authorizationContext;

                    _diagnosticListener.BeforeOnAuthorization(authorizationContext, filter);
                    _logger.BeforeExecutingMethodOnFilter(
                        FilterTypeConstants.AuthorizationFilter,
                        nameof(IAuthorizationFilter.OnAuthorization),
                        filter);

                    filter.OnAuthorization(authorizationContext);

                    _diagnosticListener.AfterOnAuthorization(authorizationContext, filter);
                    _logger.AfterExecutingMethodOnFilter(
                        FilterTypeConstants.AuthorizationFilter,
                        nameof(IAuthorizationFilter.OnAuthorization),
                        filter);

                    if (authorizationContext.Result != null)
                    {
                        goto case State.AuthorizationShortCircuit;
                    }

                    goto case State.AuthorizationNext;
                }

            case State.AuthorizationShortCircuit:
                {
                    Debug.Assert(state != null);
                    Debug.Assert(_authorizationContext != null);
                    Debug.Assert(_authorizationContext.Result != null);
                    Log.AuthorizationFailure(_logger, (IFilterMetadata)state);

                    // This is a short-circuit - execute relevant result filters + result and complete this invocation.
                    isCompleted = true;
                    _result = _authorizationContext.Result;
                    return InvokeAlwaysRunResultFilters();
                }

            case State.AuthorizationEnd:
                {
                    goto case State.ResourceBegin;
                }

            case State.ResourceBegin:
                {
                    _cursor.Reset();
                    goto case State.ResourceNext;
                }

            case State.ResourceNext:
                {
                    var current = _cursor.GetNextFilter<IResourceFilter, IAsyncResourceFilter>();
                    if (current.FilterAsync != null)
                    {
                        if (_resourceExecutingContext == null)
                        {
                            _resourceExecutingContext = new ResourceExecutingContextSealed(
                                _actionContext,
                                _filters,
                                _valueProviderFactories);
                        }

                        state = current.FilterAsync;
                        goto case State.ResourceAsyncBegin;
                    }
                    else if (current.Filter != null)
                    {
                        if (_resourceExecutingContext == null)
                        {
                            _resourceExecutingContext = new ResourceExecutingContextSealed(
                                _actionContext,
                                _filters,
                                _valueProviderFactories);
                        }

                        state = current.Filter;
                        goto case State.ResourceSyncBegin;
                    }
                    else
                    {
                        // All resource filters are currently on the stack - now execute the 'inside'.
                        goto case State.ResourceInside;
                    }
                }

            case State.ResourceAsyncBegin:
                {
                    Debug.Assert(state != null);
                    Debug.Assert(_resourceExecutingContext != null);

                    var filter = (IAsyncResourceFilter)state;
                    var resourceExecutingContext = _resourceExecutingContext;

                    _diagnosticListener.BeforeOnResourceExecution(resourceExecutingContext, filter);
                    _logger.BeforeExecutingMethodOnFilter(
                        FilterTypeConstants.ResourceFilter,
                        nameof(IAsyncResourceFilter.OnResourceExecutionAsync),
                        filter);

                    var task = filter.OnResourceExecutionAsync(resourceExecutingContext, InvokeNextResourceFilterAwaitedAsync);
                    if (!task.IsCompletedSuccessfully)
                    {
                        next = State.ResourceAsyncEnd;
                        return task;
                    }

                    goto case State.ResourceAsyncEnd;
                }

            case State.ResourceAsyncEnd:
                {
                    Debug.Assert(state != null);
                    Debug.Assert(_resourceExecutingContext != null);

                    var filter = (IAsyncResourceFilter)state;
                    if (_resourceExecutedContext == null)
                    {
                        // If we get here then the filter didn't call 'next' indicating a short circuit.
                        _resourceExecutedContext = new ResourceExecutedContextSealed(_resourceExecutingContext, _filters)
                        {
                            Canceled = true,
                            Result = _resourceExecutingContext.Result,
                        };

                        _diagnosticListener.AfterOnResourceExecution(_resourceExecutedContext, filter);
                        _logger.AfterExecutingMethodOnFilter(
                            FilterTypeConstants.ResourceFilter,
                            nameof(IAsyncResourceFilter.OnResourceExecutionAsync),
                            filter);

                        // A filter could complete a Task without setting a result
                        if (_resourceExecutingContext.Result != null)
                        {
                            goto case State.ResourceShortCircuit;
                        }
                    }

                    goto case State.ResourceEnd;
                }

            case State.ResourceSyncBegin:
                {
                    Debug.Assert(state != null);
                    Debug.Assert(_resourceExecutingContext != null);

                    var filter = (IResourceFilter)state;
                    var resourceExecutingContext = _resourceExecutingContext;

                    _diagnosticListener.BeforeOnResourceExecuting(resourceExecutingContext, filter);
                    _logger.BeforeExecutingMethodOnFilter(
                        FilterTypeConstants.ResourceFilter,
                        nameof(IResourceFilter.OnResourceExecuting),
                        filter);

                    filter.OnResourceExecuting(resourceExecutingContext);

                    _diagnosticListener.AfterOnResourceExecuting(resourceExecutingContext, filter);
                    _logger.AfterExecutingMethodOnFilter(
                        FilterTypeConstants.ResourceFilter,
                        nameof(IResourceFilter.OnResourceExecuting),
                        filter);

                    if (resourceExecutingContext.Result != null)
                    {
                        _resourceExecutedContext = new ResourceExecutedContextSealed(resourceExecutingContext, _filters)
                        {
                            Canceled = true,
                            Result = _resourceExecutingContext.Result,
                        };

                        goto case State.ResourceShortCircuit;
                    }

                    var task = InvokeNextResourceFilter();
                    if (!task.IsCompletedSuccessfully)
                    {
                        next = State.ResourceSyncEnd;
                        return task;
                    }

                    goto case State.ResourceSyncEnd;
                }

            case State.ResourceSyncEnd:
                {
                    Debug.Assert(state != null);
                    Debug.Assert(_resourceExecutingContext != null);
                    Debug.Assert(_resourceExecutedContext != null);

                    var filter = (IResourceFilter)state;
                    var resourceExecutedContext = _resourceExecutedContext;

                    _diagnosticListener.BeforeOnResourceExecuted(resourceExecutedContext, filter);
                    _logger.BeforeExecutingMethodOnFilter(
                        FilterTypeConstants.ResourceFilter,
                        nameof(IResourceFilter.OnResourceExecuted),
                        filter);

                    filter.OnResourceExecuted(resourceExecutedContext);

                    _diagnosticListener.AfterOnResourceExecuted(resourceExecutedContext, filter);
                    _logger.AfterExecutingMethodOnFilter(
                        FilterTypeConstants.ResourceFilter,
                        nameof(IResourceFilter.OnResourceExecuted),
                        filter);

                    goto case State.ResourceEnd;
                }

            case State.ResourceShortCircuit:
                {
                    Debug.Assert(state != null);
                    Debug.Assert(_resourceExecutingContext != null);
                    Debug.Assert(_resourceExecutedContext != null);
                    Log.ResourceFilterShortCircuited(_logger, (IFilterMetadata)state);

                    _result = _resourceExecutingContext.Result;
                    var task = InvokeAlwaysRunResultFilters();
                    if (!task.IsCompletedSuccessfully)
                    {
                        next = State.ResourceEnd;
                        return task;
                    }

                    goto case State.ResourceEnd;
                }

            case State.ResourceInside:
                {
                    goto case State.ExceptionBegin;
                }

            case State.ExceptionBegin:
                {
                    _cursor.Reset();
                    goto case State.ExceptionNext;
                }

            case State.ExceptionNext:
                {
                    var current = _cursor.GetNextFilter<IExceptionFilter, IAsyncExceptionFilter>();
                    if (current.FilterAsync != null)
                    {
                        state = current.FilterAsync;
                        goto case State.ExceptionAsyncBegin;
                    }
                    else if (current.Filter != null)
                    {
                        state = current.Filter;
                        goto case State.ExceptionSyncBegin;
                    }
                    else if (scope == Scope.Exception)
                    {
                        // All exception filters are on the stack already - so execute the 'inside'.
                        goto case State.ExceptionInside;
                    }
                    else
                    {
                        // There are no exception filters - so jump right to the action.
                        Debug.Assert(scope == Scope.Invoker || scope == Scope.Resource);
                        goto case State.ActionBegin;
                    }
                }

            case State.ExceptionAsyncBegin:
                {
                    var task = InvokeNextExceptionFilterAsync();
                    if (!task.IsCompletedSuccessfully)
                    {
                        next = State.ExceptionAsyncResume;
                        return task;
                    }

                    goto case State.ExceptionAsyncResume;
                }

            case State.ExceptionAsyncResume:
                {
                    Debug.Assert(state != null);

                    var filter = (IAsyncExceptionFilter)state;
                    var exceptionContext = _exceptionContext;

                    // When we get here we're 'unwinding' the stack of exception filters. If we have an unhandled exception,
                    // we'll call the filter. Otherwise there's nothing to do.
                    if (exceptionContext?.Exception != null && !exceptionContext.ExceptionHandled)
                    {
                        _diagnosticListener.BeforeOnExceptionAsync(exceptionContext, filter);
                        _logger.BeforeExecutingMethodOnFilter(
                            FilterTypeConstants.ExceptionFilter,
                            nameof(IAsyncExceptionFilter.OnExceptionAsync),
                            filter);

                        var task = filter.OnExceptionAsync(exceptionContext);
                        if (!task.IsCompletedSuccessfully)
                        {
                            next = State.ExceptionAsyncEnd;
                            return task;
                        }

                        goto case State.ExceptionAsyncEnd;
                    }

                    goto case State.ExceptionEnd;
                }

            case State.ExceptionAsyncEnd:
                {
                    Debug.Assert(state != null);
                    Debug.Assert(_exceptionContext != null);

                    var filter = (IAsyncExceptionFilter)state;
                    var exceptionContext = _exceptionContext;

                    _diagnosticListener.AfterOnExceptionAsync(exceptionContext, filter);
                    _logger.AfterExecutingMethodOnFilter(
                        FilterTypeConstants.ExceptionFilter,
                        nameof(IAsyncExceptionFilter.OnExceptionAsync),
                        filter);

                    if (exceptionContext.Exception == null || exceptionContext.ExceptionHandled)
                    {
                        // We don't need to do anything to trigger a short circuit. If there's another
                        // exception filter on the stack it will check the same set of conditions
                        // and then just skip itself.
                        _logger.ExceptionFilterShortCircuited(filter);
                    }

                    goto case State.ExceptionEnd;
                }

            case State.ExceptionSyncBegin:
                {
                    var task = InvokeNextExceptionFilterAsync();
                    if (!task.IsCompletedSuccessfully)
                    {
                        next = State.ExceptionSyncEnd;
                        return task;
                    }

                    goto case State.ExceptionSyncEnd;
                }

            case State.ExceptionSyncEnd:
                {
                    Debug.Assert(state != null);

                    var filter = (IExceptionFilter)state;
                    var exceptionContext = _exceptionContext;

                    // When we get here we're 'unwinding' the stack of exception filters. If we have an unhandled exception,
                    // we'll call the filter. Otherwise there's nothing to do.
                    if (exceptionContext?.Exception != null && !exceptionContext.ExceptionHandled)
                    {
                        _diagnosticListener.BeforeOnException(exceptionContext, filter);
                        _logger.BeforeExecutingMethodOnFilter(
                            FilterTypeConstants.ExceptionFilter,
                            nameof(IExceptionFilter.OnException),
                            filter);

                        filter.OnException(exceptionContext);

                        _diagnosticListener.AfterOnException(exceptionContext, filter);
                        _logger.AfterExecutingMethodOnFilter(
                            FilterTypeConstants.ExceptionFilter,
                            nameof(IExceptionFilter.OnException),
                            filter);

                        if (exceptionContext.Exception == null || exceptionContext.ExceptionHandled)
                        {
                            // We don't need to do anything to trigger a short circuit. If there's another
                            // exception filter on the stack it will check the same set of conditions
                            // and then just skip itself.
                            _logger.ExceptionFilterShortCircuited(filter);
                        }
                    }

                    goto case State.ExceptionEnd;
                }

            case State.ExceptionInside:
                {
                    goto case State.ActionBegin;
                }

            case State.ExceptionHandled:
                {
                    // We arrive in this state when an exception happened, but was handled by exception filters
                    // either by setting ExceptionHandled, or nulling out the Exception or setting a result
                    // on the ExceptionContext.
                    //
                    // We need to execute the result (if any) and then exit gracefully which unwinding Resource
                    // filters.

                    Debug.Assert(state != null);
                    Debug.Assert(_exceptionContext != null);

                    if (_exceptionContext.Result == null)
                    {
                        _exceptionContext.Result = new EmptyResult();
                    }

                    _result = _exceptionContext.Result;

                    var task = InvokeAlwaysRunResultFilters();
                    if (!task.IsCompletedSuccessfully)
                    {
                        next = State.ResourceInsideEnd;
                        return task;
                    }

                    goto case State.ResourceInsideEnd;
                }

            case State.ExceptionEnd:
                {
                    var exceptionContext = _exceptionContext;

                    if (scope == Scope.Exception)
                    {
                        isCompleted = true;
                        return Task.CompletedTask;
                    }

                    if (exceptionContext != null)
                    {
                        if (exceptionContext.Result != null ||
                            exceptionContext.Exception == null ||
                            exceptionContext.ExceptionHandled)
                        {
                            goto case State.ExceptionHandled;
                        }

                        Rethrow(exceptionContext);
                        Debug.Fail("unreachable");
                    }

                    var task = InvokeResultFilters();
                    if (!task.IsCompletedSuccessfully)
                    {
                        next = State.ResourceInsideEnd;
                        return task;
                    }
                    goto case State.ResourceInsideEnd;
                }

            case State.ActionBegin:
                {
                    var task = InvokeInnerFilterAsync();
                    if (!task.IsCompletedSuccessfully)
                    {
                        next = State.ActionEnd;
                        return task;
                    }

                    goto case State.ActionEnd;
                }

            case State.ActionEnd:
                {
                    if (scope == Scope.Exception)
                    {
                        // If we're inside an exception filter, let's allow those filters to 'unwind' before
                        // the result.
                        isCompleted = true;
                        return Task.CompletedTask;
                    }

                    Debug.Assert(scope == Scope.Invoker || scope == Scope.Resource);
                    var task = InvokeResultFilters();
                    if (!task.IsCompletedSuccessfully)
                    {
                        next = State.ResourceInsideEnd;
                        return task;
                    }
                    goto case State.ResourceInsideEnd;
                }

            case State.ResourceInsideEnd:
                {
                    if (scope == Scope.Resource)
                    {
                        _resourceExecutedContext = new ResourceExecutedContextSealed(_actionContext, _filters)
                        {
                            Result = _result,
                        };

                        goto case State.ResourceEnd;
                    }

                    goto case State.InvokeEnd;
                }

            case State.ResourceEnd:
                {
                    if (scope == Scope.Resource)
                    {
                        isCompleted = true;
                        return Task.CompletedTask;
                    }

                    Debug.Assert(scope == Scope.Invoker);
                    Rethrow(_resourceExecutedContext!);

                    goto case State.InvokeEnd;
                }

            case State.InvokeEnd:
                {
                    isCompleted = true;
                    return Task.CompletedTask;
                }

            default:
                throw new InvalidOperationException();
        }
    }

    private Task<ResourceExecutedContext> InvokeNextResourceFilterAwaitedAsync()
    {
        Debug.Assert(_resourceExecutingContext != null);

        if (_resourceExecutingContext.Result != null)
        {
            // If we get here, it means that an async filter set a result AND called next(). This is forbidden.
            return Throw();
        }

        var task = InvokeNextResourceFilter();
        if (!task.IsCompletedSuccessfully)
        {
            return Awaited(this, task);
        }

        Debug.Assert(_resourceExecutedContext != null);
        return Task.FromResult<ResourceExecutedContext>(_resourceExecutedContext);

        static async Task<ResourceExecutedContext> Awaited(ResourceInvoker invoker, Task task)
        {
            await task;

            Debug.Assert(invoker._resourceExecutedContext != null);
            return invoker._resourceExecutedContext;
        }
#pragma warning disable CS1998
        static async Task<ResourceExecutedContext> Throw()
        {
            var message = Resources.FormatAsyncResourceFilter_InvalidShortCircuit(
                typeof(IAsyncResourceFilter).Name,
                nameof(ResourceExecutingContext.Result),
                typeof(ResourceExecutingContext).Name,
                typeof(ResourceExecutionDelegate).Name);
            throw new InvalidOperationException(message);
        }
#pragma warning restore CS1998
    }

    private Task InvokeNextResourceFilter()
    {
        try
        {
            var scope = Scope.Resource;
            var next = State.ResourceNext;
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
        }
        catch (Exception exception)
        {
            _resourceExecutedContext = new ResourceExecutedContextSealed(_resourceExecutingContext!, _filters)
            {
                ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception),
            };
        }

        Debug.Assert(_resourceExecutedContext != null);
        return Task.CompletedTask;

        static async Task Awaited(ResourceInvoker invoker, Task lastTask, State next, Scope scope, object? state, bool isCompleted)
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
                invoker._resourceExecutedContext = new ResourceExecutedContextSealed(invoker._resourceExecutingContext!, invoker._filters)
                {
                    ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception),
                };
            }

            Debug.Assert(invoker._resourceExecutedContext != null);
        }
    }

    private Task InvokeNextExceptionFilterAsync()
    {
        try
        {
            var next = State.ExceptionNext;
            var state = (object?)null;
            var scope = Scope.Exception;
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

        static async Task Awaited(ResourceInvoker invoker, Task lastTask, State next, Scope scope, object? state, bool isCompleted)
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
                invoker._exceptionContext = new ExceptionContextSealed(invoker._actionContext, invoker._filters)
                {
                    ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception),
                };
            }
        }
    }

    private Task InvokeAlwaysRunResultFilters()
    {
        try
        {
            var next = State.ResultBegin;
            var scope = Scope.Invoker;
            var state = (object?)null;
            var isCompleted = false;

            while (!isCompleted)
            {
                var lastTask = ResultNext<IAlwaysRunResultFilter, IAsyncAlwaysRunResultFilter>(ref next, ref scope, ref state, ref isCompleted);
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

        static async Task Awaited(ResourceInvoker invoker, Task lastTask, State next, Scope scope, object? state, bool isCompleted)
        {
            await lastTask;

            while (!isCompleted)
            {
                await invoker.ResultNext<IAlwaysRunResultFilter, IAsyncAlwaysRunResultFilter>(ref next, ref scope, ref state, ref isCompleted);
            }
        }
    }

    private Task InvokeResultFilters()
    {
        try
        {
            var next = State.ResultBegin;
            var scope = Scope.Invoker;
            var state = (object?)null;
            var isCompleted = false;

            while (!isCompleted)
            {
                var lastTask = ResultNext<IResultFilter, IAsyncResultFilter>(ref next, ref scope, ref state, ref isCompleted);
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

        static async Task Awaited(ResourceInvoker invoker, Task lastTask, State next, Scope scope, object? state, bool isCompleted)
        {
            await lastTask;

            while (!isCompleted)
            {
                await invoker.ResultNext<IResultFilter, IAsyncResultFilter>(ref next, ref scope, ref state, ref isCompleted);
            }
        }
    }

    private Task ResultNext<TFilter, TFilterAsync>(ref State next, ref Scope scope, ref object? state, ref bool isCompleted)
        where TFilter : class, IResultFilter
        where TFilterAsync : class, IAsyncResultFilter
    {
        var resultFilterKind = typeof(TFilter) == typeof(IAlwaysRunResultFilter) ?
            FilterTypeConstants.AlwaysRunResultFilter :
            FilterTypeConstants.ResultFilter;

        switch (next)
        {
            case State.ResultBegin:
                {
                    _cursor.Reset();
                    goto case State.ResultNext;
                }

            case State.ResultNext:
                {
                    var current = _cursor.GetNextFilter<TFilter, TFilterAsync>();
                    if (current.FilterAsync != null)
                    {
                        if (_resultExecutingContext == null)
                        {
                            _resultExecutingContext = new ResultExecutingContextSealed(_actionContext, _filters, _result!, _instance!);
                        }

                        state = current.FilterAsync;
                        goto case State.ResultAsyncBegin;
                    }
                    else if (current.Filter != null)
                    {
                        if (_resultExecutingContext == null)
                        {
                            _resultExecutingContext = new ResultExecutingContextSealed(_actionContext, _filters, _result!, _instance!);
                        }

                        state = current.Filter;
                        goto case State.ResultSyncBegin;
                    }
                    else
                    {
                        goto case State.ResultInside;
                    }
                }

            case State.ResultAsyncBegin:
                {
                    Debug.Assert(state != null);
                    Debug.Assert(_resultExecutingContext != null);

                    var filter = (TFilterAsync)state;
                    var resultExecutingContext = _resultExecutingContext;

                    _diagnosticListener.BeforeOnResultExecution(resultExecutingContext, filter);
                    _logger.BeforeExecutingMethodOnFilter(
                        resultFilterKind,
                        nameof(IAsyncResultFilter.OnResultExecutionAsync),
                        filter);

                    var task = filter.OnResultExecutionAsync(resultExecutingContext, InvokeNextResultFilterAwaitedAsync<TFilter, TFilterAsync>);
                    if (!task.IsCompletedSuccessfully)
                    {
                        next = State.ResultAsyncEnd;
                        return task;
                    }

                    goto case State.ResultAsyncEnd;
                }

            case State.ResultAsyncEnd:
                {
                    Debug.Assert(state != null);
                    Debug.Assert(_resultExecutingContext != null);

                    var filter = (TFilterAsync)state;
                    var resultExecutingContext = _resultExecutingContext;
                    var resultExecutedContext = _resultExecutedContext;

                    if (resultExecutedContext == null || resultExecutingContext.Cancel)
                    {
                        // Short-circuited by not calling next || Short-circuited by setting Cancel == true
                        _logger.ResultFilterShortCircuited(filter);

                        _resultExecutedContext = new ResultExecutedContextSealed(
                            _actionContext,
                            _filters,
                            resultExecutingContext.Result,
                            _instance!)
                        {
                            Canceled = true,
                        };
                    }

                    _diagnosticListener.AfterOnResultExecution(_resultExecutedContext!, filter);
                    _logger.AfterExecutingMethodOnFilter(
                        resultFilterKind,
                        nameof(IAsyncResultFilter.OnResultExecutionAsync),
                        filter);

                    goto case State.ResultEnd;
                }

            case State.ResultSyncBegin:
                {
                    Debug.Assert(state != null);
                    Debug.Assert(_resultExecutingContext != null);

                    var filter = (TFilter)state;
                    var resultExecutingContext = _resultExecutingContext;

                    _diagnosticListener.BeforeOnResultExecuting(resultExecutingContext, filter);
                    _logger.BeforeExecutingMethodOnFilter(
                        resultFilterKind,
                        nameof(IResultFilter.OnResultExecuting),
                        filter);

                    filter.OnResultExecuting(resultExecutingContext);

                    _diagnosticListener.AfterOnResultExecuting(resultExecutingContext, filter);
                    _logger.AfterExecutingMethodOnFilter(
                        resultFilterKind,
                        nameof(IResultFilter.OnResultExecuting),
                        filter);

                    if (_resultExecutingContext.Cancel)
                    {
                        // Short-circuited by setting Cancel == true
                        _logger.ResultFilterShortCircuited(filter);

                        _resultExecutedContext = new ResultExecutedContextSealed(
                            resultExecutingContext,
                            _filters,
                            resultExecutingContext.Result,
                            _instance!)
                        {
                            Canceled = true,
                        };

                        goto case State.ResultEnd;
                    }

                    var task = InvokeNextResultFilterAsync<TFilter, TFilterAsync>();
                    if (!task.IsCompletedSuccessfully)
                    {
                        next = State.ResultSyncEnd;
                        return task;
                    }

                    goto case State.ResultSyncEnd;
                }

            case State.ResultSyncEnd:
                {
                    Debug.Assert(state != null);
                    Debug.Assert(_resultExecutingContext != null);
                    Debug.Assert(_resultExecutedContext != null);

                    var filter = (TFilter)state;
                    var resultExecutedContext = _resultExecutedContext;

                    _diagnosticListener.BeforeOnResultExecuted(resultExecutedContext, filter);
                    _logger.BeforeExecutingMethodOnFilter(
                        resultFilterKind,
                        nameof(IResultFilter.OnResultExecuted),
                        filter);

                    filter.OnResultExecuted(resultExecutedContext);

                    _diagnosticListener.AfterOnResultExecuted(resultExecutedContext, filter);
                    _logger.AfterExecutingMethodOnFilter(
                        resultFilterKind,
                        nameof(IResultFilter.OnResultExecuted),
                        filter);

                    goto case State.ResultEnd;
                }

            case State.ResultInside:
                {
                    // If we executed result filters then we need to grab the result from there.
                    if (_resultExecutingContext != null)
                    {
                        _result = _resultExecutingContext.Result;
                    }

                    if (_result == null)
                    {
                        // The empty result is always flowed back as the 'executed' result if we don't have one.
                        _result = new EmptyResult();
                    }

                    var task = InvokeResultAsync(_result);
                    if (!task.IsCompletedSuccessfully)
                    {
                        next = State.ResultEnd;
                        return task;
                    }

                    goto case State.ResultEnd;
                }

            case State.ResultEnd:
                {
                    var result = _result;
                    isCompleted = true;

                    if (scope == Scope.Result)
                    {
                        if (_resultExecutedContext == null)
                        {
                            _resultExecutedContext = new ResultExecutedContextSealed(_actionContext, _filters, result!, _instance!);
                        }

                        return Task.CompletedTask;
                    }

                    Rethrow(_resultExecutedContext!);
                    return Task.CompletedTask;
                }

            default:
                throw new InvalidOperationException(); // Unreachable.
        }
    }

    private Task InvokeNextResultFilterAsync<TFilter, TFilterAsync>()
        where TFilter : class, IResultFilter
        where TFilterAsync : class, IAsyncResultFilter
    {
        try
        {
            var next = State.ResultNext;
            var state = (object?)null;
            var scope = Scope.Result;
            var isCompleted = false;
            while (!isCompleted)
            {
                var lastTask = ResultNext<TFilter, TFilterAsync>(ref next, ref scope, ref state, ref isCompleted);
                if (!lastTask.IsCompletedSuccessfully)
                {
                    return Awaited(this, lastTask, next, scope, state, isCompleted);
                }
            }
        }
        catch (Exception exception)
        {
            _resultExecutedContext = new ResultExecutedContextSealed(_actionContext, _filters, _result!, _instance!)
            {
                ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception),
            };
        }

        Debug.Assert(_resultExecutedContext != null);

        return Task.CompletedTask;

        static async Task Awaited(ResourceInvoker invoker, Task lastTask, State next, Scope scope, object? state, bool isCompleted)
        {
            try
            {
                await lastTask;

                while (!isCompleted)
                {
                    await invoker.ResultNext<TFilter, TFilterAsync>(ref next, ref scope, ref state, ref isCompleted);
                }
            }
            catch (Exception exception)
            {
                invoker._resultExecutedContext = new ResultExecutedContextSealed(invoker._actionContext, invoker._filters, invoker._result!, invoker._instance!)
                {
                    ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception),
                };
            }

            Debug.Assert(invoker._resultExecutedContext != null);
        }
    }

    private Task<ResultExecutedContext> InvokeNextResultFilterAwaitedAsync<TFilter, TFilterAsync>()
        where TFilter : class, IResultFilter
        where TFilterAsync : class, IAsyncResultFilter
    {
        Debug.Assert(_resultExecutingContext != null);
        if (_resultExecutingContext.Cancel)
        {
            // If we get here, it means that an async filter set cancel == true AND called next().
            // This is forbidden.
            return Throw();
        }

        var task = InvokeNextResultFilterAsync<TFilter, TFilterAsync>();
        if (!task.IsCompletedSuccessfully)
        {
            return Awaited(this, task);
        }

        Debug.Assert(_resultExecutedContext != null);
        return Task.FromResult<ResultExecutedContext>(_resultExecutedContext);

        static async Task<ResultExecutedContext> Awaited(ResourceInvoker invoker, Task task)
        {
            await task;

            Debug.Assert(invoker._resultExecutedContext != null);
            return invoker._resultExecutedContext;
        }
#pragma warning disable CS1998
        static async Task<ResultExecutedContext> Throw()
        {
            var message = Resources.FormatAsyncResultFilter_InvalidShortCircuit(
                typeof(IAsyncResultFilter).Name,
                nameof(ResultExecutingContext.Cancel),
                typeof(ResultExecutingContext).Name,
                typeof(ResultExecutionDelegate).Name);

            throw new InvalidOperationException(message);
        }
#pragma warning restore CS1998
    }


    private static void Rethrow(ResourceExecutedContextSealed context)
    {
        if (context == null)
        {
            return;
        }

        if (context.ExceptionHandled)
        {
            return;
        }

        if (context.ExceptionDispatchInfo != null)
        {
            context.ExceptionDispatchInfo.Throw();
        }

        if (context.Exception != null)
        {
            throw context.Exception;
        }
    }

    private static void Rethrow(ExceptionContextSealed context)
    {
        if (context == null)
        {
            return;
        }

        if (context.ExceptionHandled)
        {
            return;
        }

        if (context.ExceptionDispatchInfo != null)
        {
            context.ExceptionDispatchInfo.Throw();
        }

        if (context.Exception != null)
        {
            throw context.Exception;
        }
    }

    private static void Rethrow(ResultExecutedContextSealed context)
    {
        if (context == null)
        {
            return;
        }

        if (context.ExceptionHandled)
        {
            return;
        }

        if (context.ExceptionDispatchInfo != null)
        {
            context.ExceptionDispatchInfo.Throw();
        }

        if (context.Exception != null)
        {
            throw context.Exception;
        }
    }

    private enum Scope
    {
        Invoker,
        Resource,
        Exception,
        Result,
    }

    private enum State
    {
        InvokeBegin,
        AuthorizationBegin,
        AuthorizationNext,
        AuthorizationAsyncBegin,
        AuthorizationAsyncEnd,
        AuthorizationSync,
        AuthorizationShortCircuit,
        AuthorizationEnd,
        ResourceBegin,
        ResourceNext,
        ResourceAsyncBegin,
        ResourceAsyncEnd,
        ResourceSyncBegin,
        ResourceSyncEnd,
        ResourceShortCircuit,
        ResourceInside,
        ResourceInsideEnd,
        ResourceEnd,
        ExceptionBegin,
        ExceptionNext,
        ExceptionAsyncBegin,
        ExceptionAsyncResume,
        ExceptionAsyncEnd,
        ExceptionSyncBegin,
        ExceptionSyncEnd,
        ExceptionInside,
        ExceptionHandled,
        ExceptionEnd,
        ActionBegin,
        ActionEnd,
        ResultBegin,
        ResultNext,
        ResultAsyncBegin,
        ResultAsyncEnd,
        ResultSyncBegin,
        ResultSyncEnd,
        ResultInside,
        ResultEnd,
        InvokeEnd,
    }

    private static class FilterTypeConstants
    {
        public const string AuthorizationFilter = "Authorization Filter";
        public const string ResourceFilter = "Resource Filter";
        public const string ActionFilter = "Action Filter";
        public const string ExceptionFilter = "Exception Filter";
        public const string ResultFilter = "Result Filter";
        public const string AlwaysRunResultFilter = "Always Run Result Filter";
    }

    private sealed class ResultExecutedContextSealed : ResultExecutedContext
    {
        public ResultExecutedContextSealed(
            ActionContext actionContext,
            IList<IFilterMetadata> filters,
            IActionResult result,
            object controller)
        : base(actionContext, filters, result, controller) { }
    }

    private sealed class ResultExecutingContextSealed : ResultExecutingContext
    {
        public ResultExecutingContextSealed(
            ActionContext actionContext,
            IList<IFilterMetadata> filters,
            IActionResult result,
            object controller)
            : base(actionContext, filters, result, controller)
        { }
    }

    private sealed class ExceptionContextSealed : ExceptionContext
    {
        public ExceptionContextSealed(ActionContext actionContext, IList<IFilterMetadata> filters) : base(actionContext, filters) { }
    }
    private sealed class ResourceExecutedContextSealed : ResourceExecutedContext
    {
        public ResourceExecutedContextSealed(ActionContext actionContext, IList<IFilterMetadata> filters) : base(actionContext, filters) { }
    }
    private sealed class ResourceExecutingContextSealed : ResourceExecutingContext
    {
        public ResourceExecutingContextSealed(ActionContext actionContext, IList<IFilterMetadata> filters, IList<IValueProviderFactory> valueProviderFactories) : base(actionContext, filters, valueProviderFactories) { }
    }
    private sealed class AuthorizationFilterContextSealed : AuthorizationFilterContext
    {
        public AuthorizationFilterContextSealed(ActionContext actionContext, IList<IFilterMetadata> filters) : base(actionContext, filters) { }
    }

    private static partial class Log
    {
        [LoggerMessage(3, LogLevel.Information, "Authorization failed for the request at filter '{AuthorizationFilter}'.", EventName = "AuthorizationFailure")]
        public static partial void AuthorizationFailure(ILogger logger, IFilterMetadata authorizationFilter);

        [LoggerMessage(4, LogLevel.Debug, "Request was short circuited at resource filter '{ResourceFilter}'.", EventName = "ResourceFilterShortCircuit")]
        public static partial void ResourceFilterShortCircuited(ILogger logger, IFilterMetadata resourceFilter);

        [LoggerMessage(5, LogLevel.Trace, "Before executing action result {ActionResult}.", EventName = "BeforeExecutingActionResult")]
        private static partial void BeforeExecutingActionResult(ILogger logger, Type actionResult);

        public static void BeforeExecutingActionResult(ILogger logger, IActionResult actionResult)
        {
            BeforeExecutingActionResult(logger, actionResult.GetType());
        }

        [LoggerMessage(6, LogLevel.Trace, "After executing action result {ActionResult}.", EventName = "AfterExecutingActionResult")]
        private static partial void AfterExecutingActionResult(ILogger logger, Type actionResult);

        public static void AfterExecutingActionResult(ILogger logger, IActionResult actionResult)
        {
            AfterExecutingActionResult(logger, actionResult.GetType());
        }
    }
}
