// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public abstract class ResourceInvoker
    {
        protected readonly DiagnosticSource _diagnosticSource;
        protected readonly ILogger _logger;
        protected readonly ActionContext _actionContext;
        protected readonly IFilterMetadata[] _filters;
        protected readonly IList<IValueProviderFactory> _valueProviderFactories;

        private AuthorizationFilterContext _authorizationContext;
        private ResourceExecutingContext _resourceExecutingContext;
        private ResourceExecutedContext _resourceExecutedContext;

        // Do not make this readonly, it's mutable. We don't want to make a copy.
        // https://blogs.msdn.microsoft.com/ericlippert/2008/05/14/mutating-readonly-structs/
        protected FilterCursor _cursor;
        protected IActionResult _result;

        public ResourceInvoker(
            DiagnosticSource diagnosticSource,
            ILogger logger,
            ActionContext actionContext,
            IFilterMetadata[] filters,
            IList<IValueProviderFactory> valueProviderFactories)
        {

            if (diagnosticSource == null)
            {
                throw new ArgumentNullException(nameof(diagnosticSource));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            if (filters == null)
            {
                throw new ArgumentNullException(nameof(filters));
            }

            if (valueProviderFactories == null)
            {
                throw new ArgumentNullException(nameof(valueProviderFactories));
            }

            _diagnosticSource = diagnosticSource;
            _logger = logger;
            _actionContext = actionContext;

            _filters = filters;
            _valueProviderFactories = valueProviderFactories;
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

                    var startTimestamp = _logger.IsEnabled(LogLevel.Information) ? Stopwatch.GetTimestamp() : 0;

                    try
                    {
                        await InvokeFilterPipelineAsync();
                    }
                    finally
                    {
                        ReleaseResources();
                        _logger.ExecutedAction(_actionContext.ActionDescriptor, startTimestamp);
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

            try
            {
                await result.ExecuteResultAsync(actionContext);
            }
            finally
            {
                _diagnosticSource.AfterActionResult(actionContext, result);
            }
        }

        private Task Next(ref State next, ref Scope scope, ref object state, ref bool isCompleted)
        {
            var diagnosticSource = _diagnosticSource;
            var logger = _logger;

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

                        filter.OnAuthorization(authorizationContext);

                        _diagnosticSource.AfterOnAuthorization(authorizationContext, filter);

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

                        _logger.AuthorizationFailure((IFilterMetadata)state);

                        // If an authorization filter short circuits, the result is the last thing we execute
                        // so just return that task instead of calling back into the state machine.
                        isCompleted = true;
                        return InvokeResultAsync(_authorizationContext.Result);
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
                        else if (scope == Scope.Resource)
                        {
                            // All resource filters are currently on the stack - now execute the 'inside'.
                            Debug.Assert(_resourceExecutingContext != null);
                            goto case State.ResourceInside;
                        }
                        else
                        {
                            // There are no resource filters - so jump right to 'inside'.
                            Debug.Assert(scope == Scope.Invoker);
                            next = State.InvokeEnd;
                            return InvokeInnerFilterAsync();
                        }
                    }

                case State.ResourceAsyncBegin:
                    {
                        Debug.Assert(state != null);
                        Debug.Assert(_resourceExecutingContext != null);

                        var filter = (IAsyncResourceFilter)state;
                        var resourceExecutingContext = _resourceExecutingContext;

                        _diagnosticSource.BeforeOnResourceExecution(resourceExecutingContext, filter);

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

                        filter.OnResourceExecuting(resourceExecutingContext);

                        _diagnosticSource.AfterOnResourceExecuting(resourceExecutingContext, filter);

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

                        filter.OnResourceExecuted(resourceExecutedContext);

                        _diagnosticSource.AfterOnResourceExecuted(resourceExecutedContext, filter);

                        goto case State.ResourceEnd;
                    }

                case State.ResourceShortCircuit:
                    {
                        Debug.Assert(state != null);
                        Debug.Assert(_resourceExecutingContext != null);
                        Debug.Assert(_resourceExecutedContext != null);

                        _logger.ResourceFilterShortCircuited((IFilterMetadata)state);

                        var task = InvokeResultAsync(_resourceExecutingContext.Result);
                        if (task.Status != TaskStatus.RanToCompletion)
                        {
                            next = State.ResourceEnd;
                            return task;
                        }

                        goto case State.ResourceEnd;
                    }

                case State.ResourceInside:
                    {
                        next = State.ResourceOutside;
                        return InvokeInnerFilterAsync();
                    }
                case State.ResourceOutside:
                    {
                        var result = _result;

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
                            return TaskCache.CompletedTask;
                        }

                        Debug.Assert(scope == Scope.Invoker);
                        Rethrow(_resourceExecutedContext);

                        goto case State.InvokeEnd;
                    }

                case State.InvokeEnd:
                    {
                        isCompleted = true;
                        return TaskCache.CompletedTask;
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

        private enum Scope
        {
            Invoker,
            Resource,
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
            ResourceOutside,
            ResourceEnd,
            InvokeEnd,
        }
    }
}
