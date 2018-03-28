// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public abstract class ResourceInvoker
    {
        protected readonly DiagnosticSource _diagnosticSource;
        protected readonly ILogger _logger;
        protected readonly IActionResultTypeMapper _mapper;
        protected readonly ActionContext _actionContext;
        protected readonly IFilterMetadata[] _filters;
        protected readonly IList<IValueProviderFactory> _valueProviderFactories;

        private AuthorizationFilterContext _authorizationContext;
        private ResourceExecutingContext _resourceExecutingContext;
        private ResourceExecutedContext _resourceExecutedContext;
        private ExceptionContext _exceptionContext;
        private ResultExecutingContext _resultExecutingContext;
        private ResultExecutedContext _resultExecutedContext;

        // Do not make this readonly, it's mutable. We don't want to make a copy.
        // https://blogs.msdn.microsoft.com/ericlippert/2008/05/14/mutating-readonly-structs/
        protected FilterCursor _cursor;
        protected IActionResult _result;
        protected object _instance;

        public ResourceInvoker(
            DiagnosticSource diagnosticSource,
            ILogger logger,
            IActionResultTypeMapper mapper,
            ActionContext actionContext,
            IFilterMetadata[] filters,
            IList<IValueProviderFactory> valueProviderFactories)
        {
            _diagnosticSource = diagnosticSource ?? throw new ArgumentNullException(nameof(diagnosticSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _actionContext = actionContext ?? throw new ArgumentNullException(nameof(actionContext));

            _filters = filters ?? throw new ArgumentNullException(nameof(filters));
            _valueProviderFactories = valueProviderFactories ?? throw new ArgumentNullException(nameof(valueProviderFactories));
            _cursor = new FilterCursor(filters);
        }

        public virtual async Task InvokeAsync()
        {
            try
            {
                _diagnosticSource.BeforeAction(
                    _actionContext.ActionDescriptor,
                    _actionContext.HttpContext,
                    _actionContext.RouteData);

                using (_logger.ActionScope(_actionContext.ActionDescriptor))
                {
                    _logger.ExecutingAction(_actionContext.ActionDescriptor);

                    _logger.AuthorizationFiltersExecutionPlan(_filters);
                    _logger.ResourceFiltersExecutionPlan(_filters);
                    _logger.ActionFiltersExecutionPlan(_filters);
                    _logger.ExceptionFiltersExecutionPlan(_filters);
                    _logger.ResultFiltersExecutionPlan(_filters);

                    var stopwatch = ValueStopwatch.StartNew();

                    try
                    {
                        await InvokeFilterPipelineAsync();
                    }
                    finally
                    {
                        ReleaseResources();
                        _logger.ExecutedAction(_actionContext.ActionDescriptor, stopwatch.GetElapsedTime());
                    }
                }
            }
            finally
            {
                _diagnosticSource.AfterAction(
                    _actionContext.ActionDescriptor,
                    _actionContext.HttpContext,
                    _actionContext.RouteData);
            }
        }

        /// <summary>
        /// In derived types, releases resources such as controller, model, or page instances created as
        /// part of invoking the inner pipeline.
        /// </summary>
        protected abstract void ReleaseResources();

        private async Task InvokeFilterPipelineAsync()
        {
            var next = State.InvokeBegin;

            // The `scope` tells the `Next` method who the caller is, and what kind of state to initialize to
            // communicate a result. The outermost scope is `Scope.Invoker` and doesn't require any type
            // of context or result other than throwing.
            var scope = Scope.Invoker;

            // The `state` is used for internal state handling during transitions between states. In practice this
            // means storing a filter instance in `state` and then retrieving it in the next state.
            var state = (object)null;

            // `isCompleted` will be set to true when we've reached a terminal state.
            var isCompleted = false;

            while (!isCompleted)
            {
                await Next(ref next, ref scope, ref state, ref isCompleted);
            }
        }

        protected abstract Task InvokeInnerFilterAsync();

        protected async Task InvokeResultAsync(IActionResult result)
        {
            var actionContext = _actionContext;

            _diagnosticSource.BeforeActionResult(actionContext, result);
            _logger.BeforeExecutingActionResult(result);

            try
            {
                await result.ExecuteResultAsync(actionContext);
            }
            finally
            {
                _diagnosticSource.AfterActionResult(actionContext, result);
                _logger.AfterExecutingActionResult(result);
            }
        }

        private Task Next(ref State next, ref Scope scope, ref object state, ref bool isCompleted)
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
                                _authorizationContext = new AuthorizationFilterContext(_actionContext, _filters);
                            }

                            state = current.FilterAsync;
                            goto case State.AuthorizationAsyncBegin;
                        }
                        else if (current.Filter != null)
                        {
                            if (_authorizationContext == null)
                            {
                                _authorizationContext = new AuthorizationFilterContext(_actionContext, _filters);
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

                        _diagnosticSource.BeforeOnAuthorizationAsync(authorizationContext, filter);
                        _logger.BeforeExecutingMethodOnFilter(
                            FilterTypeConstants.AuthorizationFilter,
                            nameof(IAsyncAuthorizationFilter.OnAuthorizationAsync),
                            filter);

                        var task = filter.OnAuthorizationAsync(authorizationContext);
                        if (task.Status != TaskStatus.RanToCompletion)
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

                        _diagnosticSource.AfterOnAuthorizationAsync(authorizationContext, filter);
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

                        _diagnosticSource.BeforeOnAuthorization(authorizationContext, filter);
                        _logger.BeforeExecutingMethodOnFilter(
                            FilterTypeConstants.AuthorizationFilter,
                            nameof(IAuthorizationFilter.OnAuthorization),
                            filter);

                        filter.OnAuthorization(authorizationContext);

                        _diagnosticSource.AfterOnAuthorization(authorizationContext, filter);
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

                        _logger.AuthorizationFailure((IFilterMetadata)state);

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
                                _resourceExecutingContext = new ResourceExecutingContext(
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
                                _resourceExecutingContext = new ResourceExecutingContext(
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

                        _diagnosticSource.BeforeOnResourceExecution(resourceExecutingContext, filter);
                        _logger.BeforeExecutingMethodOnFilter(
                            FilterTypeConstants.ResourceFilter,
                            nameof(IAsyncResourceFilter.OnResourceExecutionAsync),
                            filter);

                        var task = filter.OnResourceExecutionAsync(resourceExecutingContext, InvokeNextResourceFilterAwaitedAsync);
                        if (task.Status != TaskStatus.RanToCompletion)
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
                            _resourceExecutedContext = new ResourceExecutedContext(_resourceExecutingContext, _filters)
                            {
                                Canceled = true,
                                Result = _resourceExecutingContext.Result,
                            };

                            _diagnosticSource.AfterOnResourceExecution(_resourceExecutedContext, filter);
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

                        _diagnosticSource.BeforeOnResourceExecuting(resourceExecutingContext, filter);
                        _logger.BeforeExecutingMethodOnFilter(
                            FilterTypeConstants.ResourceFilter,
                            nameof(IResourceFilter.OnResourceExecuting),
                            filter);

                        filter.OnResourceExecuting(resourceExecutingContext);

                        _diagnosticSource.AfterOnResourceExecuting(resourceExecutingContext, filter);
                        _logger.AfterExecutingMethodOnFilter(
                            FilterTypeConstants.ResourceFilter,
                            nameof(IResourceFilter.OnResourceExecuting),
                            filter);

                        if (resourceExecutingContext.Result != null)
                        {
                            _resourceExecutedContext = new ResourceExecutedContext(resourceExecutingContext, _filters)
                            {
                                Canceled = true,
                                Result = _resourceExecutingContext.Result,
                            };

                            goto case State.ResourceShortCircuit;
                        }

                        var task = InvokeNextResourceFilter();
                        if (task.Status != TaskStatus.RanToCompletion)
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

                        _diagnosticSource.BeforeOnResourceExecuted(resourceExecutedContext, filter);
                        _logger.BeforeExecutingMethodOnFilter(
                            FilterTypeConstants.ResourceFilter,
                            nameof(IResourceFilter.OnResourceExecuted),
                            filter);

                        filter.OnResourceExecuted(resourceExecutedContext);

                        _diagnosticSource.AfterOnResourceExecuted(resourceExecutedContext, filter);
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

                        _logger.ResourceFilterShortCircuited((IFilterMetadata)state);

                        _result = _resourceExecutingContext.Result;
                        var task = InvokeAlwaysRunResultFilters();
                        if (task.Status != TaskStatus.RanToCompletion)
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
                        if (task.Status != TaskStatus.RanToCompletion)
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
                            _diagnosticSource.BeforeOnExceptionAsync(exceptionContext, filter);
                            _logger.BeforeExecutingMethodOnFilter(
                                FilterTypeConstants.ExceptionFilter,
                                nameof(IAsyncExceptionFilter.OnExceptionAsync),
                                filter);

                            var task = filter.OnExceptionAsync(exceptionContext);
                            if (task.Status != TaskStatus.RanToCompletion)
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

                        _diagnosticSource.AfterOnExceptionAsync(exceptionContext, filter);
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
                        if (task.Status != TaskStatus.RanToCompletion)
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
                            _diagnosticSource.BeforeOnException(exceptionContext, filter);
                            _logger.BeforeExecutingMethodOnFilter(
                                FilterTypeConstants.ExceptionFilter,
                                nameof(IExceptionFilter.OnException),
                                filter);

                            filter.OnException(exceptionContext);

                            _diagnosticSource.AfterOnException(exceptionContext, filter);
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
                        if (task.Status != TaskStatus.RanToCompletion)
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
                        if (task.Status != TaskStatus.RanToCompletion)
                        {
                            next = State.ResourceInsideEnd;
                            return task;
                        }
                        goto case State.ResourceInsideEnd;
                    }

                case State.ActionBegin:
                    {
                        var task = InvokeInnerFilterAsync();
                        if (task.Status != TaskStatus.RanToCompletion)
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
                        if (task.Status != TaskStatus.RanToCompletion)
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
                            _resourceExecutedContext = new ResourceExecutedContext(_actionContext, _filters)
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
                        Rethrow(_resourceExecutedContext);

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

        private async Task<ResourceExecutedContext> InvokeNextResourceFilterAwaitedAsync()
        {
            Debug.Assert(_resourceExecutingContext != null);

            if (_resourceExecutingContext.Result != null)
            {
                // If we get here, it means that an async filter set a result AND called next(). This is forbidden.
                var message = Resources.FormatAsyncResourceFilter_InvalidShortCircuit(
                    typeof(IAsyncResourceFilter).Name,
                    nameof(ResourceExecutingContext.Result),
                    typeof(ResourceExecutingContext).Name,
                    typeof(ResourceExecutionDelegate).Name);
                throw new InvalidOperationException(message);
            }

            await InvokeNextResourceFilter();

            Debug.Assert(_resourceExecutedContext != null);
            return _resourceExecutedContext;
        }

        private async Task InvokeNextResourceFilter()
        {
            try
            {
                var scope = Scope.Resource;
                var next = State.ResourceNext;
                var state = (object)null;
                var isCompleted = false;
                while (!isCompleted)
                {
                    await Next(ref next, ref scope, ref state, ref isCompleted);
                }
            }
            catch (Exception exception)
            {
                _resourceExecutedContext = new ResourceExecutedContext(_resourceExecutingContext, _filters)
                {
                    ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception),
                };
            }

            Debug.Assert(_resourceExecutedContext != null);
        }

        private async Task InvokeNextExceptionFilterAsync()
        {
            try
            {
                var next = State.ExceptionNext;
                var state = (object)null;
                var scope = Scope.Exception;
                var isCompleted = false;
                while (!isCompleted)
                {
                    await Next(ref next, ref scope, ref state, ref isCompleted);
                }
            }
            catch (Exception exception)
            {
                _exceptionContext = new ExceptionContext(_actionContext, _filters)
                {
                    ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception),
                };
            }
        }

        private async Task InvokeAlwaysRunResultFilters()
        {
            var next = State.ResultBegin;
            var scope = Scope.Invoker;
            var state = (object)null;
            var isCompleted = false;

            while (!isCompleted)
            {
                await ResultNext<IAlwaysRunResultFilter, IAsyncAlwaysRunResultFilter>(ref next, ref scope, ref state, ref isCompleted);
            }
        }

        private async Task InvokeResultFilters()
        {
            var next = State.ResultBegin;
            var scope = Scope.Invoker;
            var state = (object)null;
            var isCompleted = false;

            while (!isCompleted)
            {
                await ResultNext<IResultFilter, IAsyncResultFilter>(ref next, ref scope, ref state, ref isCompleted);
            }
        }

        private Task ResultNext<TFilter, TFilterAsync>(ref State next, ref Scope scope, ref object state, ref bool isCompleted)
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
                                _resultExecutingContext = new ResultExecutingContext(_actionContext, _filters, _result, _instance);
                            }

                            state = current.FilterAsync;
                            goto case State.ResultAsyncBegin;
                        }
                        else if (current.Filter != null)
                        {
                            if (_resultExecutingContext == null)
                            {
                                _resultExecutingContext = new ResultExecutingContext(_actionContext, _filters, _result, _instance);
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

                        _diagnosticSource.BeforeOnResultExecution(resultExecutingContext, filter);
                        _logger.BeforeExecutingMethodOnFilter(
                            resultFilterKind,
                            nameof(IAsyncResultFilter.OnResultExecutionAsync),
                            filter);

                        var task = filter.OnResultExecutionAsync(resultExecutingContext, InvokeNextResultFilterAwaitedAsync<TFilter, TFilterAsync>);
                        if (task.Status != TaskStatus.RanToCompletion)
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

                            _resultExecutedContext = new ResultExecutedContext(
                                _actionContext,
                                _filters,
                                resultExecutingContext.Result,
                                _instance)
                            {
                                Canceled = true,
                            };
                        }

                        _diagnosticSource.AfterOnResultExecution(_resultExecutedContext, filter);
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

                        _diagnosticSource.BeforeOnResultExecuting(resultExecutingContext, filter);
                        _logger.BeforeExecutingMethodOnFilter(
                            resultFilterKind,
                            nameof(IResultFilter.OnResultExecuting),
                            filter);

                        filter.OnResultExecuting(resultExecutingContext);

                        _diagnosticSource.AfterOnResultExecuting(resultExecutingContext, filter);
                        _logger.AfterExecutingMethodOnFilter(
                            resultFilterKind,
                            nameof(IResultFilter.OnResultExecuting),
                            filter);

                        if (_resultExecutingContext.Cancel)
                        {
                            // Short-circuited by setting Cancel == true
                            _logger.ResultFilterShortCircuited(filter);

                            _resultExecutedContext = new ResultExecutedContext(
                                resultExecutingContext,
                                _filters,
                                resultExecutingContext.Result,
                                _instance)
                            {
                                Canceled = true,
                            };

                            goto case State.ResultEnd;
                        }

                        var task = InvokeNextResultFilterAsync<TFilter, TFilterAsync>();
                        if (task.Status != TaskStatus.RanToCompletion)
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

                        _diagnosticSource.BeforeOnResultExecuted(resultExecutedContext, filter);
                        _logger.BeforeExecutingMethodOnFilter(
                            resultFilterKind,
                            nameof(IResultFilter.OnResultExecuted),
                            filter);

                        filter.OnResultExecuted(resultExecutedContext);

                        _diagnosticSource.AfterOnResultExecuted(resultExecutedContext, filter);
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
                        if (task.Status != TaskStatus.RanToCompletion)
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
                                _resultExecutedContext = new ResultExecutedContext(_actionContext, _filters, result, _instance);
                            }

                            return Task.CompletedTask;
                        }

                        Rethrow(_resultExecutedContext);
                        return Task.CompletedTask;
                    }

                default:
                    throw new InvalidOperationException(); // Unreachable.
            }
        }

        private async Task InvokeNextResultFilterAsync<TFilter, TFilterAsync>()
            where TFilter : class, IResultFilter
            where TFilterAsync : class, IAsyncResultFilter
        {
            try
            {
                var next = State.ResultNext;
                var state = (object)null;
                var scope = Scope.Result;
                var isCompleted = false;
                while (!isCompleted)
                {
                    await ResultNext<TFilter, TFilterAsync>(ref next, ref scope, ref state, ref isCompleted);
                }
            }
            catch (Exception exception)
            {
                _resultExecutedContext = new ResultExecutedContext(_actionContext, _filters, _result, _instance)
                {
                    ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception),
                };
            }

            Debug.Assert(_resultExecutedContext != null);
        }

        private async Task<ResultExecutedContext> InvokeNextResultFilterAwaitedAsync<TFilter, TFilterAsync>()
            where TFilter : class, IResultFilter
            where TFilterAsync : class, IAsyncResultFilter
        {
            Debug.Assert(_resultExecutingContext != null);
            if (_resultExecutingContext.Cancel)
            {
                // If we get here, it means that an async filter set cancel == true AND called next().
                // This is forbidden.
                var message = Resources.FormatAsyncResultFilter_InvalidShortCircuit(
                    typeof(IAsyncResultFilter).Name,
                    nameof(ResultExecutingContext.Cancel),
                    typeof(ResultExecutingContext).Name,
                    typeof(ResultExecutionDelegate).Name);

                throw new InvalidOperationException(message);
            }

            await InvokeNextResultFilterAsync<TFilter, TFilterAsync>();

            Debug.Assert(_resultExecutedContext != null);
            return _resultExecutedContext;
        }

        private static void Rethrow(ResourceExecutedContext context)
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

        private static void Rethrow(ExceptionContext context)
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

        private static void Rethrow(ResultExecutedContext context)
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
    }
}
