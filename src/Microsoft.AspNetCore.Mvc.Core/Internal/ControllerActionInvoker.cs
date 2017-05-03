// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ControllerActionInvoker : ResourceInvoker, IActionInvoker
    {
        private readonly ControllerActionInvokerCacheEntry _cacheEntry;
        private readonly ControllerContext _controllerContext;

        private object _controller;
        private Dictionary<string, object> _arguments;

        private ExceptionContext _exceptionContext;

        private ActionExecutingContext _actionExecutingContext;
        private ActionExecutedContext _actionExecutedContext;

        private ResultExecutingContext _resultExecutingContext;
        private ResultExecutedContext _resultExecutedContext;

        internal ControllerActionInvoker(
            ILogger logger,
            DiagnosticSource diagnosticSource,
            ControllerContext controllerContext,
            ControllerActionInvokerCacheEntry cacheEntry,
            IFilterMetadata[] filters)
            : base(diagnosticSource, logger, controllerContext, filters, controllerContext.ValueProviderFactories)
        {
            if (cacheEntry == null)
            {
                throw new ArgumentNullException(nameof(cacheEntry));
            }

            _cacheEntry = cacheEntry;
            _controllerContext = controllerContext;
        }

        protected override void ReleaseResources()
        {
            if (_controller != null && _cacheEntry.ControllerReleaser != null)
            {
                _cacheEntry.ControllerReleaser(_controllerContext, _controller);
            }
        }

        private Task Next(ref State next, ref Scope scope, ref object state, ref bool isCompleted)
        {
            var diagnosticSource = _diagnosticSource;
            var logger = _logger;

            switch (next)
            {
                case State.ResourceInsideBegin:
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
                            // There are no exception filters - so jump right to 'inside'.
                            Debug.Assert(scope == Scope.Resource);
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
                            // We don't need to do anthing to trigger a short circuit. If there's another
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

                            filter.OnException(exceptionContext);

                            _diagnosticSource.AfterOnException(exceptionContext, filter);

                            if (exceptionContext.Exception == null || exceptionContext.ExceptionHandled)
                            {
                                // We don't need to do anthing to trigger a short circuit. If there's another
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

                        if (scope == Scope.Resource)
                        {
                            Debug.Assert(_exceptionContext.Result != null);
                            _result = _exceptionContext.Result;
                        }

                        var task = InvokeResultAsync(_exceptionContext.Result);
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
                            return TaskCache.CompletedTask;
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

                        goto case State.ResultBegin;
                    }

                case State.ActionBegin:
                    {
                        var controllerContext = _controllerContext;

                        _cursor.Reset();

                        _controller = _cacheEntry.ControllerFactory(controllerContext);

                        _arguments = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

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

                        Debug.Assert(scope == Scope.Resource);
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

                        goto case State.ResourceInsideEnd;
                    }

                case State.ResourceInsideEnd:
                    {
                        isCompleted = true;
                        return TaskCache.CompletedTask;
                    }

                default:
                    throw new InvalidOperationException();
            }
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
            var executor = _cacheEntry.ActionMethodExecutor;
            var controller = _controller;
            var arguments = _arguments;
            var orderedArguments = PrepareArguments(arguments, executor);

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
                    // Sync method returning void
                    executor.Execute(controller, orderedArguments);
                    result = new EmptyResult();
                }
                else if (returnType == typeof(Task))
                {
                    // Async method returning Task
                    // Avoid extra allocations by calling Execute rather than ExecuteAsync and casting to Task.
                    await (Task)executor.Execute(controller, orderedArguments);
                    result = new EmptyResult();
                }
                else if (returnType == typeof(Task<IActionResult>))
                {
                    // Async method returning Task<IActionResult>
                    // Avoid extra allocations by calling Execute rather than ExecuteAsync and casting to Task<IActionResult>.
                    result = await (Task<IActionResult>)executor.Execute(controller, orderedArguments);
                    if (result == null)
                    {
                        throw new InvalidOperationException(
                            Resources.FormatActionResult_ActionReturnValueCannotBeNull(typeof(IActionResult)));
                    }
                }
                else if (IsResultIActionResult(executor))
                {
                    if (executor.IsMethodAsync)
                    {
                        // Async method returning awaitable-of-IActionResult (e.g., Task<ViewResult>)
                        // We have to use ExecuteAsync because we don't know the awaitable's type at compile time.
                        result = (IActionResult)await executor.ExecuteAsync(controller, orderedArguments);
                    }
                    else
                    {
                        // Sync method returning IActionResult (e.g., ViewResult)
                        result = (IActionResult)executor.Execute(controller, orderedArguments);
                    }

                    if (result == null)
                    {
                        throw new InvalidOperationException(
                            Resources.FormatActionResult_ActionReturnValueCannotBeNull(executor.AsyncResultType ?? returnType));
                    }
                }
                else if (!executor.IsMethodAsync)
                {
                    // Sync method returning arbitrary object
                    var resultAsObject = executor.Execute(controller, orderedArguments);
                    result = resultAsObject as IActionResult ?? new ObjectResult(resultAsObject)
                    {
                        DeclaredType = returnType,
                    };
                }
                else if (executor.AsyncResultType == typeof(void))
                {
                    // Async method returning awaitable-of-void
                    await executor.ExecuteAsync(controller, orderedArguments);
                    result = new EmptyResult();
                }
                else
                {
                    // Async method returning awaitable-of-nonvoid
                    var resultAsObject = await executor.ExecuteAsync(controller, orderedArguments);
                    result = resultAsObject as IActionResult ?? new ObjectResult(resultAsObject)
                    {
                        DeclaredType = executor.AsyncResultType,
                    };
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

        private static bool IsResultIActionResult(ObjectMethodExecutor executor)
        {
            var resultType = executor.AsyncResultType ?? executor.MethodReturnType;
            return typeof(IActionResult).IsAssignableFrom(resultType);
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

        /// <remarks><see cref="ResourceInvoker.InvokeFilterPipelineAsync"/> for details on what the
        /// variables in this method represent.</remarks>
        protected override async Task InvokeInnerFilterAsync()
        {
            var next = State.ResourceInsideBegin;
            var scope = Scope.Resource;
            var state = (object)null;
            var isCompleted = false;

            while (!isCompleted)
            {
                await Next(ref next, ref scope, ref state, ref isCompleted);
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

        private Task BindArgumentsAsync()
        {
            // Perf: Avoid allocating async state machines where possible. We only need the state
            // machine if you need to bind properties or arguments.
            var actionDescriptor = _controllerContext.ActionDescriptor;
            if (actionDescriptor.BoundProperties.Count == 0 &&
                actionDescriptor.Parameters.Count == 0)
            {
                return TaskCache.CompletedTask;
            }

            Debug.Assert(_cacheEntry.ControllerBinderDelegate != null);
            return _cacheEntry.ControllerBinderDelegate(_controllerContext, _controller, _arguments);
        }

        private static object[] PrepareArguments(
            IDictionary<string, object> actionParameters,
            ObjectMethodExecutor actionMethodExecutor)
        {
            var declaredParameterInfos = actionMethodExecutor.MethodParameters;
            var count = declaredParameterInfos.Length;
            if (count == 0)
            {
                return null;
            }

            var arguments = new object[count];
            for (var index = 0; index < count; index++)
            {
                var parameterInfo = declaredParameterInfos[index];

                if (!actionParameters.TryGetValue(parameterInfo.Name, out var value))
                {
                    value = actionMethodExecutor.GetDefaultValueForParameter(index);
                }

                arguments[index] = value;
            }

            return arguments;
        }

        private enum Scope
        {
            Resource,
            Exception,
            Action,
            Result,
        }

        private enum State
        {
            ResourceInsideBegin,
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
            ResourceInsideEnd,
        }
    }
}
