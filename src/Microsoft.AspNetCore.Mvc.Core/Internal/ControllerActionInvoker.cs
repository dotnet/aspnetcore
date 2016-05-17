// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
#if NETSTANDARD1_6
using System.Reflection;
#endif
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ControllerActionInvoker : IActionInvoker
    {
        private readonly IControllerFactory _controllerFactory;
        private readonly IControllerArgumentBinder _controllerArgumentBinder;
        private readonly DiagnosticSource _diagnosticSource;
        private readonly ILogger _logger;

        private readonly ControllerContext _controllerContext;
        private readonly IFilterMetadata[] _filters;
        private readonly ObjectMethodExecutor _executor;

        // Do not make this readonly, it's mutable. We don't want to make a copy.
        // https://blogs.msdn.microsoft.com/ericlippert/2008/05/14/mutating-readonly-structs/
        private FilterCursor _cursor;
        private object _controller;
        private Dictionary<string, object> _arguments;
        private IActionResult _result;

        private AuthorizationFilterContext _authorizationContext;

        private ResourceExecutingContext _resourceExecutingContext;
        private ResourceExecutedContext _resourceExecutedContext;

        private ExceptionContext _exceptionContext;

        private ActionExecutingContext _actionExecutingContext;
        private ActionExecutedContext _actionExecutedContext;

        private ResultExecutingContext _resultExecutingContext;
        private ResultExecutedContext _resultExecutedContext;

        public ControllerActionInvoker(
            ControllerActionInvokerCache cache,
            IControllerFactory controllerFactory,
            IControllerArgumentBinder controllerArgumentBinder,
            ILogger logger,
            DiagnosticSource diagnosticSource,
            ActionContext actionContext,
            IReadOnlyList<IValueProviderFactory> valueProviderFactories,
            int maxModelValidationErrors)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            if (controllerFactory == null)
            {
                throw new ArgumentNullException(nameof(controllerFactory));
            }

            if (controllerArgumentBinder == null)
            {
                throw new ArgumentNullException(nameof(controllerArgumentBinder));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (diagnosticSource == null)
            {
                throw new ArgumentNullException(nameof(diagnosticSource));
            }

            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            if (valueProviderFactories == null)
            {
                throw new ArgumentNullException(nameof(valueProviderFactories));
            }

            _controllerFactory = controllerFactory;
            _controllerArgumentBinder = controllerArgumentBinder;
            _logger = logger;
            _diagnosticSource = diagnosticSource;

            _controllerContext = new ControllerContext(actionContext);
            _controllerContext.ModelState.MaxAllowedErrors = maxModelValidationErrors;

            // PERF: These are rarely going to be changed, so let's go copy-on-write.
            _controllerContext.ValueProviderFactories = new CopyOnWriteList<IValueProviderFactory>(valueProviderFactories);

            var cacheEntry = cache.GetState(_controllerContext);
            _filters = cacheEntry.Filters;
            _executor = cacheEntry.ActionMethodExecutor;
            _cursor = new FilterCursor(_filters);
        }

        public virtual async Task InvokeAsync()
        {
            try
            {
                _diagnosticSource.BeforeAction(
                    _controllerContext.ActionDescriptor,
                    _controllerContext.HttpContext,
                    _controllerContext.RouteData);

                using (_logger.ActionScope(_controllerContext.ActionDescriptor))
                {
                    _logger.ExecutingAction(_controllerContext.ActionDescriptor);

                    var startTimestamp = _logger.IsEnabled(LogLevel.Information) ? Stopwatch.GetTimestamp() : 0;

                    // The invoker is implemented using a 'Taskerator' or perhaps an 'Asyncerator' (both terms are correct
                    // and in common usage). This method is the main 'driver' loop and will call into the `Next` method
                    // (`await`ing the result) until a terminal state is reached.
                    //
                    // The `Next` method walks through the state transitions of the invoker and returns a `Task` when there's
                    // actual async work that we need to await. As an optimization that Next method won't return a `Task`
                    // that completes synchronously.
                    //
                    // Additionally the `Next` funtion will be called recursively when we're 'inside' a filter invocation.
                    // Executing 'inside' a filter requires an async method call within a `try`/`catch` for error handling, so
                    // we have to recurse. Each 'frame' calls into `Next` with a value of `Scope` that communicates what kind
                    // of 'frame' is executing. This has an effect on the state machine transitions as well as what kinds of
                    // contexts need to be constructed to communicate the result of execution of the 'frame'.

                    // When returning, the `Next` method will set `next` to the state to goto on the subsequent invocation.
                    // This is similar to `Task.ContinueWith`, but since we have a fixed number of states we can avoid
                    // the overhead of actually using `Task.ContinueWith`.
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

                    try
                    {
                        while (!isCompleted)
                        {
                            await Next(ref next, ref scope, ref state, ref isCompleted);
                        }
                    }
                    finally
                    {
                        if (_controller != null)
                        {
                            _controllerFactory.ReleaseController(_controllerContext, _controller);
                        }

                        _logger.ExecutedAction(_controllerContext.ActionDescriptor, startTimestamp);
                    }
                }
            }
            finally
            {
                _diagnosticSource.AfterAction(
                    _controllerContext.ActionDescriptor,
                    _controllerContext.HttpContext,
                    _controllerContext.RouteData);
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
                                _authorizationContext = new AuthorizationFilterContext(_controllerContext, _filters);
                            }

                            state = current.FilterAsync;
                            goto case State.AuthorizationAsyncBegin;
                        }
                        else if (current.Filter != null)
                        {
                            if (_authorizationContext == null)
                            {
                                _authorizationContext = new AuthorizationFilterContext(_controllerContext, _filters);
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
                                    _controllerContext,
                                    _filters,
                                    _controllerContext.ValueProviderFactories);
                            }

                            state = current.FilterAsync;
                            goto case State.ResourceAsyncBegin;
                        }
                        else if (current.Filter != null)
                        {
                            if (_resourceExecutingContext == null)
                            {
                                _resourceExecutingContext = new ResourceExecutingContext(
                                    _controllerContext,
                                    _filters,
                                    _controllerContext.ValueProviderFactories);
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
                            goto case State.ExceptionBegin;
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
                        }

                        _diagnosticSource.AfterOnResourceExecution(_resourceExecutedContext, filter);

                        if (_resourceExecutingContext.Result != null)
                        {
                            goto case State.ResourceShortCircuit;
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
                        goto case State.ExceptionBegin;
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
                            // There are no exception filters - so jump right to 'inside'.
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

                        if (exceptionContext.Exception == null || exceptionContext.ExceptionHandled)
                        {
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

                            filter.OnException(exceptionContext);

                            _diagnosticSource.AfterOnException(exceptionContext, filter);

                            if (exceptionContext.Exception == null || exceptionContext.ExceptionHandled)
                            {
                                _logger.ExceptionFilterShortCircuited(filter);
                            }
                        }

                        goto case State.ExceptionEnd;
                    }

                case State.ExceptionInside:
                    {
                        goto case State.ActionBegin;
                    }

                case State.ExceptionShortCircuit:
                    {
                        Debug.Assert(state != null);
                        Debug.Assert(_exceptionContext != null);

                        Task task;
                        if (scope == Scope.Resource)
                        {
                            Debug.Assert(_exceptionContext.Result != null);
                            _resourceExecutedContext = new ResourceExecutedContext(_controllerContext, _filters)
                            {
                                Result = _exceptionContext.Result,
                            };

                            task = InvokeResultAsync(_exceptionContext.Result);
                            if (task.Status != TaskStatus.RanToCompletion)
                            {
                                next = State.ResourceEnd;
                                return task;
                            }

                            goto case State.ResourceEnd;
                        }

                        task = InvokeResultAsync(_exceptionContext.Result);
                        if (task.Status != TaskStatus.RanToCompletion)
                        {
                            next = State.InvokeEnd;
                            return task;
                        }

                        goto case State.ResourceEnd;
                    }

                case State.ExceptionEnd:
                    {
                        var exceptionContext = _exceptionContext;

                        if (scope == Scope.Exception)
                        {
                            isCompleted = true;
                            return TaskCache.CompletedTask;
                        }

                        if (exceptionContext != null)
                        {
                            if (exceptionContext.Result != null && !exceptionContext.ExceptionHandled)
                            {
                                goto case State.ExceptionShortCircuit;
                            }

                            Rethrow(exceptionContext);
                        }

                        goto case State.ResultBegin;
                    }

                case State.ActionBegin:
                    {
                        var controllerContext = _controllerContext;

                        _cursor.Reset();

                        _controller = _controllerFactory.CreateController(controllerContext);

                        _arguments = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                        var task = _controllerArgumentBinder.BindArgumentsAsync(controllerContext, _controller, _arguments);
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
                                _actionExecutingContext = new ActionExecutingContext(_controllerContext, _filters, _arguments, _controller);
                            }

                            state = current.FilterAsync;
                            goto case State.ActionAsyncBegin;
                        }
                        else if (current.Filter != null)
                        {
                            if (_actionExecutingContext == null)
                            {
                                _actionExecutingContext = new ActionExecutingContext(_controllerContext, _filters, _arguments, _controller);
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

                        _diagnosticSource.BeforeOnActionExecution(actionExecutingContext, filter);

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

                            _actionExecutedContext = new ActionExecutedContext(
                                _controllerContext,
                                _filters,
                                _controller)
                            {
                                Canceled = true,
                                Result = _actionExecutingContext.Result,
                            };
                        }

                        _diagnosticSource.AfterOnActionExecution(_actionExecutedContext, filter);

                        goto case State.ActionEnd;
                    }

                case State.ActionSyncBegin:
                    {
                        Debug.Assert(state != null);
                        Debug.Assert(_actionExecutingContext != null);

                        var filter = (IActionFilter)state;
                        var actionExecutingContext = _actionExecutingContext;

                        _diagnosticSource.BeforeOnActionExecuting(actionExecutingContext, filter);

                        filter.OnActionExecuting(actionExecutingContext);

                        _diagnosticSource.AfterOnActionExecuting(actionExecutingContext, filter);

                        if (actionExecutingContext.Result != null)
                        {
                            // Short-circuited by setting a result.
                            _logger.ActionFilterShortCircuited(filter);

                            _actionExecutedContext = new ActionExecutedContext(
                                _actionExecutingContext,
                                _filters,
                                _controller)
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

                        _diagnosticSource.BeforeOnActionExecuted(actionExecutedContext, filter);

                        filter.OnActionExecuted(actionExecutedContext);

                        _diagnosticSource.BeforeOnActionExecuted(actionExecutedContext, filter);

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
                                _actionExecutedContext = new ActionExecutedContext(_controllerContext, _filters, _controller)
                                {
                                    Result = _result,
                                };
                            }

                            isCompleted = true;
                            return TaskCache.CompletedTask;
                        }

                        var actionExecutedContext = _actionExecutedContext;
                        Rethrow(actionExecutedContext);

                        if (actionExecutedContext != null)
                        {
                            _result = actionExecutedContext.Result;
                        }

                        if (scope == Scope.Exception)
                        {
                            // If we're inside an exception filter, let's allow those filters to 'unwind' before
                            // the result.
                            isCompleted = true;
                            return TaskCache.CompletedTask;
                        }

                        Debug.Assert(scope == Scope.Invoker || scope == Scope.Resource);
                        goto case State.ResultBegin;
                    }

                case State.ResultBegin:
                    {
                        _cursor.Reset();
                        goto case State.ResultNext;
                    }

                case State.ResultNext:
                    {
                        var current = _cursor.GetNextFilter<IResultFilter, IAsyncResultFilter>();
                        if (current.FilterAsync != null)
                        {
                            if (_resultExecutingContext == null)
                            {
                                _resultExecutingContext = new ResultExecutingContext(_controllerContext, _filters, _result, _controller);
                            }

                            state = current.FilterAsync;
                            goto case State.ResultAsyncBegin;
                        }
                        else if (current.Filter != null)
                        {
                            if (_resultExecutingContext == null)
                            {
                                _resultExecutingContext = new ResultExecutingContext(_controllerContext, _filters, _result, _controller);
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

                        var filter = (IAsyncResultFilter)state;
                        var resultExecutingContext = _resultExecutingContext;

                        _diagnosticSource.BeforeOnResultExecution(resultExecutingContext, filter);

                        var task = filter.OnResultExecutionAsync(resultExecutingContext, InvokeNextResultFilterAwaitedAsync);
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

                        var filter = (IAsyncResultFilter)state;
                        var resultExecutingContext = _resultExecutingContext;
                        var resultExecutedContext = _resultExecutedContext;

                        if (resultExecutedContext == null || resultExecutingContext.Cancel == true)
                        {
                            // Short-circuited by not calling next || Short-circuited by setting Cancel == true
                            _logger.ResourceFilterShortCircuited(filter);

                            _resultExecutedContext = new ResultExecutedContext(
                                _controllerContext,
                                _filters,
                                resultExecutingContext.Result,
                                _controller)
                            {
                                Canceled = true,
                            };
                        }

                        _diagnosticSource.AfterOnResultExecution(_resultExecutedContext, filter);
                        goto case State.ResultEnd;
                    }

                case State.ResultSyncBegin:
                    {
                        Debug.Assert(state != null);
                        Debug.Assert(_resultExecutingContext != null);

                        var filter = (IResultFilter)state;
                        var resultExecutingContext = _resultExecutingContext;

                        _diagnosticSource.BeforeOnResultExecuting(resultExecutingContext, filter);

                        filter.OnResultExecuting(resultExecutingContext);

                        _diagnosticSource.AfterOnResultExecuting(resultExecutingContext, filter);

                        if (_resultExecutingContext.Cancel == true)
                        {
                            // Short-circuited by setting Cancel == true
                            _logger.ResourceFilterShortCircuited(filter);

                            _resultExecutedContext = new ResultExecutedContext(
                                resultExecutingContext,
                                _filters,
                                resultExecutingContext.Result,
                                _controller)
                            {
                                Canceled = true,
                            };

                            goto case State.ResultEnd;
                        }

                        var task = InvokeNextResultFilterAsync();
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

                        var filter = (IResultFilter)state;
                        var resultExecutedContext = _resultExecutedContext;

                        _diagnosticSource.BeforeOnResultExecuted(resultExecutedContext, filter);

                        filter.OnResultExecuted(resultExecutedContext);

                        _diagnosticSource.AfterOnResultExecuted(resultExecutedContext, filter);

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

                        if (scope == Scope.Result)
                        {
                            if (_resultExecutedContext == null)
                            {
                                _resultExecutedContext = new ResultExecutedContext(_controllerContext, _filters, result, _controller);
                            }

                            isCompleted = true;
                            return TaskCache.CompletedTask;
                        }

                        Rethrow(_resultExecutedContext);

                        if (scope == Scope.Resource)
                        {
                            _resourceExecutedContext = new ResourceExecutedContext(_controllerContext, _filters)
                            {
                                Result = result,
                            };

                            goto case State.ResourceEnd;
                        }

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

        private async Task InvokeNextResourceFilter()
        {
            try
            {
                var next = State.ResourceNext;
                var state = (object)null;
                var scope = Scope.Resource;
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
                _exceptionContext = new ExceptionContext(_controllerContext, _filters)
                {
                    ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception),
                };
            }
        }

        private async Task InvokeNextActionFilterAsync()
        {
            try
            {
                var next = State.ActionNext;
                var state = (object)null;
                var scope = Scope.Action;
                var isCompleted = false;
                while (!isCompleted)
                {
                    await Next(ref next, ref scope, ref state, ref isCompleted);
                }
            }
            catch (Exception exception)
            {
                _actionExecutedContext = new ActionExecutedContext(_controllerContext, _filters, _controller)
                {
                    ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception),
                };
            }

            Debug.Assert(_actionExecutedContext != null);
        }

        private async Task<ActionExecutedContext> InvokeNextActionFilterAwaitedAsync()
        {
            Debug.Assert(_actionExecutingContext != null);
            if (_actionExecutingContext.Result != null)
            {
                // If we get here, it means that an async filter set a result AND called next(). This is forbidden.
                var message = Resources.FormatAsyncActionFilter_InvalidShortCircuit(
                    typeof(IAsyncActionFilter).Name,
                    nameof(ActionExecutingContext.Result),
                    typeof(ActionExecutingContext).Name,
                    typeof(ActionExecutionDelegate).Name);

                throw new InvalidOperationException(message);
            }

            await InvokeNextActionFilterAsync();

            Debug.Assert(_actionExecutedContext != null);
            return _actionExecutedContext;
        }

        private async Task InvokeActionMethodAsync()
        {
            var controllerContext = _controllerContext;
            var executor = _executor;
            var controller = _controller;
            var arguments = _arguments;
            var orderedArguments = ControllerActionExecutor.PrepareArguments(arguments, executor);

            var diagnosticSource = _diagnosticSource;
            var logger = _logger;

            IActionResult result = null;
            try
            {
                diagnosticSource.BeforeActionMethod(
                    controllerContext,
                    arguments,
                    controller);
                logger.ActionMethodExecuting(controllerContext, orderedArguments);
                
                var returnType = executor.MethodReturnType;
                if (returnType == typeof(void))
                {
                    executor.Execute(controller, orderedArguments);
                    result = new EmptyResult();
                }
                else if (returnType == typeof(Task))
                {
                    await (Task)executor.Execute(controller, orderedArguments);
                    result = new EmptyResult();
                }
                else if (executor.TaskGenericType == typeof(IActionResult))
                {
                    result = await (Task<IActionResult>)executor.Execute(controller, orderedArguments);
                    if (result == null)
                    {
                        throw new InvalidOperationException(
                            Resources.FormatActionResult_ActionReturnValueCannotBeNull(typeof(IActionResult)));
                    }
                }
                else if (executor.IsTypeAssignableFromIActionResult)
                {
                    if (_executor.IsMethodAsync)
                    {
                        result = (IActionResult)await _executor.ExecuteAsync(controller, orderedArguments);
                    }
                    else
                    {
                        result = (IActionResult)_executor.Execute(controller, orderedArguments);
                    }

                    if (result == null)
                    {
                        throw new InvalidOperationException(
                            Resources.FormatActionResult_ActionReturnValueCannotBeNull(_executor.TaskGenericType ?? returnType));
                    }
                }
                else if (!executor.IsMethodAsync)
                {
                    var resultAsObject = executor.Execute(controller, orderedArguments);
                    result = resultAsObject as IActionResult ?? new ObjectResult(resultAsObject)
                    {
                        DeclaredType = returnType,
                    };
                }
                else if (executor.TaskGenericType != null)
                {
                    var resultAsObject = await executor.ExecuteAsync(controller, orderedArguments);
                    result = resultAsObject as IActionResult ?? new ObjectResult(resultAsObject)
                    {
                        DeclaredType = executor.TaskGenericType,
                    };
                }
                else
                {
                    // This will be the case for types which have derived from Task and Task<T> or non Task types.
                    throw new InvalidOperationException(Resources.FormatActionExecutor_UnexpectedTaskInstance(
                        executor.MethodInfo.Name,
                        executor.MethodInfo.DeclaringType));
                }

                _result = result;
                logger.ActionMethodExecuted(controllerContext, result);
            }
            finally
            {
                diagnosticSource.AfterActionMethod(
                    controllerContext,
                    arguments,
                    controllerContext,
                    result);
            }
        }

        private async Task InvokeNextResultFilterAsync()
        {
            try
            {
                var next = State.ResultNext;
                var state = (object)null;
                var scope = Scope.Result;
                var isCompleted = false;
                while (!isCompleted)
                {
                    await Next(ref next, ref scope, ref state, ref isCompleted);
                }
            }
            catch (Exception exception)
            {
                _resultExecutedContext = new ResultExecutedContext(_controllerContext, _filters, _result, _controller)
                {
                    ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception),
                };
            }

            Debug.Assert(_resultExecutedContext != null);
        }

        private async Task<ResultExecutedContext> InvokeNextResultFilterAwaitedAsync()
        {
            Debug.Assert(_resultExecutingContext != null);
            if (_resultExecutingContext.Cancel == true)
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

            await InvokeNextResultFilterAsync();

            Debug.Assert(_resultExecutedContext != null);
            return _resultExecutedContext;
        }

        private async Task InvokeResultAsync(IActionResult result)
        {
            var controllerContext = _controllerContext;

            _diagnosticSource.BeforeActionResult(controllerContext, result);

            try
            {
                await result.ExecuteResultAsync(controllerContext);
            }
            finally
            {
                _diagnosticSource.AfterActionResult(controllerContext, result);
            }
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

        private static void Rethrow(ActionExecutedContext context)
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
            Action,
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
            ResourceEnd,
            ExceptionBegin,
            ExceptionNext,
            ExceptionAsyncBegin,
            ExceptionAsyncResume,
            ExceptionAsyncEnd,
            ExceptionSyncBegin,
            ExceptionSyncEnd,
            ExceptionInside,
            ExceptionShortCircuit,
            ExceptionEnd,
            ActionBegin,
            ActionNext,
            ActionAsyncBegin,
            ActionAsyncEnd,
            ActionSyncBegin,
            ActionSyncEnd,
            ActionInside,
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

        /// <summary>
        /// A one-way cursor for filters.
        /// </summary>
        /// <remarks>
        /// This will iterate the filter collection once per-stage, and skip any filters that don't have
        /// the one of interfaces that applies to the current stage.
        ///
        /// Filters are always executed in the following order, but short circuiting plays a role.
        ///
        /// Indentation reflects nesting.
        ///
        /// 1. Exception Filters
        ///     2. Authorization Filters
        ///     3. Action Filters
        ///        Action
        ///
        /// 4. Result Filters
        ///    Result
        ///
        /// </remarks>
        private struct FilterCursor
        {
            private int _index;
            private readonly IFilterMetadata[] _filters;

            public FilterCursor(int index, IFilterMetadata[] filters)
            {
                _index = index;
                _filters = filters;
            }

            public FilterCursor(IFilterMetadata[] filters)
            {
                _index = 0;
                _filters = filters;
            }

            public void Reset()
            {
                _index = 0;
            }

            public FilterCursorItem<TFilter, TFilterAsync> GetNextFilter<TFilter, TFilterAsync>()
                where TFilter : class
                where TFilterAsync : class
            {
                while (_index < _filters.Length)
                {
                    var filter = _filters[_index] as TFilter;
                    var filterAsync = _filters[_index] as TFilterAsync;

                    _index += 1;

                    if (filter != null || filterAsync != null)
                    {
                        return new FilterCursorItem<TFilter, TFilterAsync>(_index, filter, filterAsync);
                    }
                }

                return default(FilterCursorItem<TFilter, TFilterAsync>);
            }
        }

        private struct FilterCursorItem<TFilter, TFilterAsync>
        {
            public readonly int Index;
            public readonly TFilter Filter;
            public readonly TFilterAsync FilterAsync;

            public FilterCursorItem(int index, TFilter filter, TFilterAsync filterAsync)
            {
                Index = index;
                Filter = filter;
                FilterAsync = filterAsync;
            }
        }
    }
}
