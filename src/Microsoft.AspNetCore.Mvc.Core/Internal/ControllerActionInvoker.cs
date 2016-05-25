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
using Microsoft.Extensions.Internal;
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

                    await InvokeAllAuthorizationFiltersAsync();

                    // If Authorization Filters return a result, it's a short circuit because
                    // authorization failed. We don't execute Result Filters around the result.
                    Debug.Assert(_authorizationContext != null);
                    if (_authorizationContext.Result != null)
                    {
                        await InvokeResultAsync(_authorizationContext.Result);
                        return;
                    }

                    try
                    {
                        await InvokeAllResourceFiltersAsync();
                    }
                    finally
                    {
                        // Release the instance after all filters have run. We don't need to surround
                        // Authorizations filters because the instance will be created much later than
                        // that.
                        if (_controller != null)
                        {
                            _controllerFactory.ReleaseController(_controllerContext, _controller);
                        }
                    }

                    // We've reached the end of resource filters. If there's an unhandled exception on the context then
                    // it should be thrown and middleware has a chance to handle it.
                    Debug.Assert(_resourceExecutedContext != null);
                    if (_resourceExecutedContext.Exception != null && !_resourceExecutedContext.ExceptionHandled)
                    {
                        if (_resourceExecutedContext.ExceptionDispatchInfo == null)
                        {
                            throw _resourceExecutedContext.Exception;
                        }
                        else
                        {
                            _resourceExecutedContext.ExceptionDispatchInfo.Throw();
                        }
                    }

                    _logger.ExecutedAction(_controllerContext.ActionDescriptor, startTimestamp);
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

        private Task InvokeAllAuthorizationFiltersAsync()
        {
            _cursor.Reset();

            _authorizationContext = new AuthorizationFilterContext(_controllerContext, _filters);
            return InvokeAuthorizationFilterAsync();
        }

        private async Task InvokeAuthorizationFilterAsync()
        {
            // We should never get here if we already have a result.
            Debug.Assert(_authorizationContext != null);
            Debug.Assert(_authorizationContext.Result == null);

            var current = _cursor.GetNextFilter<IAuthorizationFilter, IAsyncAuthorizationFilter>();
            if (current.FilterAsync != null)
            {
                _diagnosticSource.BeforeOnAuthorizationAsync(_authorizationContext, current.FilterAsync);

                await current.FilterAsync.OnAuthorizationAsync(_authorizationContext);

                _diagnosticSource.AfterOnAuthorizationAsync(_authorizationContext, current.FilterAsync);

                if (_authorizationContext.Result == null)
                {
                    // Only keep going if we don't have a result
                    await InvokeAuthorizationFilterAsync();
                }
                else
                {
                    _logger.AuthorizationFailure(current.FilterAsync);
                }
            }
            else if (current.Filter != null)
            {
                _diagnosticSource.BeforeOnAuthorization(_authorizationContext, current.Filter);

                current.Filter.OnAuthorization(_authorizationContext);

                _diagnosticSource.AfterOnAuthorization(_authorizationContext, current.Filter);

                if (_authorizationContext.Result == null)
                {
                    // Only keep going if we don't have a result
                    await InvokeAuthorizationFilterAsync();
                }
                else
                {
                    _logger.AuthorizationFailure(current.Filter);
                }
            }
            else
            {
                // We've run out of Authorization Filters - if we haven't short circuited by now then this
                // request is authorized.
            }
        }

        private Task InvokeAllResourceFiltersAsync()
        {
            _cursor.Reset();

            _resourceExecutingContext = new ResourceExecutingContext(
                _controllerContext,
                _filters,
                _controllerContext.ValueProviderFactories);

            return InvokeResourceFilterAsync();
        }

        private async Task<ResourceExecutedContext> InvokeResourceFilterAwaitedAsync()
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

            await InvokeResourceFilterAsync();

            Debug.Assert(_resourceExecutedContext != null);
            return _resourceExecutedContext;
        }

        private async Task InvokeResourceFilterAsync()
        {
            Debug.Assert(_resourceExecutingContext != null);

            var item = _cursor.GetNextFilter<IResourceFilter, IAsyncResourceFilter>();
            try
            {
                if (item.FilterAsync != null)
                {
                    _diagnosticSource.BeforeOnResourceExecution(_resourceExecutingContext, item.FilterAsync);

                    await item.FilterAsync.OnResourceExecutionAsync(_resourceExecutingContext, InvokeResourceFilterAwaitedAsync);

                    if (_resourceExecutedContext == null)
                    {
                        // If we get here then the filter didn't call 'next' indicating a short circuit
                        _resourceExecutedContext = new ResourceExecutedContext(_resourceExecutingContext, _filters)
                        {
                            Canceled = true,
                            Result = _resourceExecutingContext.Result,
                        };
                    }

                    _diagnosticSource.AfterOnResourceExecution(_resourceExecutedContext, item.FilterAsync);

                    if (_resourceExecutingContext.Result != null)
                    {
                        _logger.ResourceFilterShortCircuited(item.FilterAsync);

                        await InvokeResultAsync(_resourceExecutingContext.Result);
                    }
                }
                else if (item.Filter != null)
                {
                    _diagnosticSource.BeforeOnResourceExecuting(_resourceExecutingContext, item.Filter);

                    item.Filter.OnResourceExecuting(_resourceExecutingContext);

                    _diagnosticSource.AfterOnResourceExecuting(_resourceExecutingContext, item.Filter);

                    if (_resourceExecutingContext.Result != null)
                    {
                        // Short-circuited by setting a result.
                        _logger.ResourceFilterShortCircuited(item.Filter);

                        await InvokeResultAsync(_resourceExecutingContext.Result);

                        _resourceExecutedContext = new ResourceExecutedContext(_resourceExecutingContext, _filters)
                        {
                            Canceled = true,
                            Result = _resourceExecutingContext.Result,
                        };
                    }
                    else
                    {
                        await InvokeResourceFilterAsync();
                        Debug.Assert(_resourceExecutedContext != null);

                        _diagnosticSource.BeforeOnResourceExecuted(_resourceExecutedContext, item.Filter);

                        item.Filter.OnResourceExecuted(_resourceExecutedContext);

                        _diagnosticSource.AfterOnResourceExecuted(_resourceExecutedContext, item.Filter);
                    }
                }
                else
                {
                    // >> ExceptionFilters >> Model Binding >> ActionFilters >> Action
                    await InvokeAllExceptionFiltersAsync();

                    // If Exception Filters provide a result, it's a short-circuit due to an exception.
                    // We don't execute Result Filters around the result.
                    Debug.Assert(_exceptionContext != null);
                    if (_exceptionContext.Result != null)
                    {
                        // This means that exception filters returned a result to 'handle' an error.
                        // We're not interested in seeing the exception details since it was handled.
                        await InvokeResultAsync(_exceptionContext.Result);

                        _resourceExecutedContext = new ResourceExecutedContext(_resourceExecutingContext, _filters)
                        {
                            Result = _exceptionContext.Result,
                        };
                    }
                    else if (_exceptionContext.Exception != null)
                    {
                        // If we get here, this means that we have an unhandled exception.
                        // Exception filted didn't handle this, so send it on to resource filters.
                        _resourceExecutedContext = new ResourceExecutedContext(_resourceExecutingContext, _filters);

                        // Preserve the stack trace if possible.
                        _resourceExecutedContext.Exception = _exceptionContext.Exception;
                        if (_exceptionContext.ExceptionDispatchInfo != null)
                        {
                            _resourceExecutedContext.ExceptionDispatchInfo = _exceptionContext.ExceptionDispatchInfo;
                        }
                    }
                    else
                    {
                        // We have a successful 'result' from the action or an Action Filter, so run
                        // Result Filters.
                        Debug.Assert(_actionExecutedContext != null);
                        var result = _actionExecutedContext.Result;

                        // >> ResultFilters >> (Result)
                        await InvokeAllResultFiltersAsync(result);

                        _resourceExecutedContext = new ResourceExecutedContext(_resourceExecutingContext, _filters)
                        {
                            Result = _resultExecutedContext.Result,
                        };
                    }
                }
            }
            catch (Exception exception)
            {
                _resourceExecutedContext = new ResourceExecutedContext(_resourceExecutingContext, _filters)
                {
                    ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception)
                };
            }

            Debug.Assert(_resourceExecutedContext != null);
        }

        private Task InvokeAllExceptionFiltersAsync()
        {
            _cursor.Reset();

            return InvokeExceptionFilterAsync();
        }

        private async Task InvokeExceptionFilterAsync()
        {
            var current = _cursor.GetNextFilter<IExceptionFilter, IAsyncExceptionFilter>();
            if (current.FilterAsync != null)
            {
                // Exception filters run "on the way out" - so the filter is run after the rest of the
                // pipeline.
                await InvokeExceptionFilterAsync();

                Debug.Assert(_exceptionContext != null);
                if (_exceptionContext.Exception != null)
                {
                    _diagnosticSource.BeforeOnExceptionAsync(_exceptionContext, current.FilterAsync);

                    // Exception filters only run when there's an exception - unsetting it will short-circuit
                    // other exception filters.
                    await current.FilterAsync.OnExceptionAsync(_exceptionContext);

                    _diagnosticSource.AfterOnExceptionAsync(_exceptionContext, current.FilterAsync);

                    if (_exceptionContext.Exception == null)
                    {
                        _logger.ExceptionFilterShortCircuited(current.FilterAsync);
                    }
                }
            }
            else if (current.Filter != null)
            {
                // Exception filters run "on the way out" - so the filter is run after the rest of the
                // pipeline.
                await InvokeExceptionFilterAsync();

                Debug.Assert(_exceptionContext != null);
                if (_exceptionContext.Exception != null)
                {
                    _diagnosticSource.BeforeOnException(_exceptionContext, current.Filter);

                    // Exception filters only run when there's an exception - unsetting it will short-circuit
                    // other exception filters.
                    current.Filter.OnException(_exceptionContext);

                    _diagnosticSource.AfterOnException(_exceptionContext, current.Filter);

                    if (_exceptionContext.Exception == null)
                    {
                        _logger.ExceptionFilterShortCircuited(current.Filter);
                    }
                }
            }
            else
            {
                // We've reached the 'end' of the exception filter pipeline - this means that one stack frame has
                // been built for each exception. When we return from here, these frames will either:
                //
                // 1) Call the filter (if we have an exception)
                // 2) No-op (if we don't have an exception)
                Debug.Assert(_exceptionContext == null);
                _exceptionContext = new ExceptionContext(_controllerContext, _filters);

                try
                {
                    await InvokeAllActionFiltersAsync();

                    // Action filters might 'return' an unhandled exception instead of throwing
                    Debug.Assert(_actionExecutedContext != null);
                    if (_actionExecutedContext.Exception != null && !_actionExecutedContext.ExceptionHandled)
                    {
                        _exceptionContext.Exception = _actionExecutedContext.Exception;
                        if (_actionExecutedContext.ExceptionDispatchInfo != null)
                        {
                            _exceptionContext.ExceptionDispatchInfo = _actionExecutedContext.ExceptionDispatchInfo;
                        }
                    }
                }
                catch (Exception exception)
                {
                    _exceptionContext.ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception);
                }
            }
        }

        private async Task InvokeAllActionFiltersAsync()
        {
            _cursor.Reset();

            _controller = _controllerFactory.CreateController(_controllerContext);

            var arguments = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            await _controllerArgumentBinder.BindArgumentsAsync(_controllerContext, _controller, arguments);
            _actionExecutingContext = new ActionExecutingContext(_controllerContext, _filters, arguments, _controller);

            await InvokeActionFilterAsync();
        }

        private async Task<ActionExecutedContext> InvokeActionFilterAwaitedAsync()
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

            await InvokeActionFilterAsync();

            Debug.Assert(_actionExecutedContext != null);
            return _actionExecutedContext;
        }

        private async Task InvokeActionFilterAsync()
        {
            Debug.Assert(_actionExecutingContext != null);

            var item = _cursor.GetNextFilter<IActionFilter, IAsyncActionFilter>();
            try
            {
                if (item.FilterAsync != null)
                {
                    _diagnosticSource.BeforeOnActionExecution(_actionExecutingContext, item.FilterAsync);

                    await item.FilterAsync.OnActionExecutionAsync(_actionExecutingContext, InvokeActionFilterAwaitedAsync);

                    if (_actionExecutedContext == null)
                    {
                        // If we get here then the filter didn't call 'next' indicating a short circuit
                        _logger.ActionFilterShortCircuited(item.FilterAsync);

                        _actionExecutedContext = new ActionExecutedContext(
                            _actionExecutingContext,
                            _filters,
                            _controller)
                        {
                            Canceled = true,
                            Result = _actionExecutingContext.Result,
                        };
                    }

                    _diagnosticSource.AfterOnActionExecution(_actionExecutedContext, item.FilterAsync);
                }
                else if (item.Filter != null)
                {
                    _diagnosticSource.BeforeOnActionExecuting(_actionExecutingContext, item.Filter);

                    item.Filter.OnActionExecuting(_actionExecutingContext);

                    _diagnosticSource.AfterOnActionExecuting(_actionExecutingContext, item.Filter);

                    if (_actionExecutingContext.Result != null)
                    {
                        // Short-circuited by setting a result.
                        _logger.ActionFilterShortCircuited(item.Filter);

                        _actionExecutedContext = new ActionExecutedContext(
                            _actionExecutingContext,
                            _filters,
                            _controller)
                        {
                            Canceled = true,
                            Result = _actionExecutingContext.Result,
                        };
                    }
                    else
                    {
                        await InvokeActionFilterAsync();
                        Debug.Assert(_actionExecutedContext != null);

                        _diagnosticSource.BeforeOnActionExecuted(_actionExecutedContext, item.Filter);

                        item.Filter.OnActionExecuted(_actionExecutedContext);

                        _diagnosticSource.BeforeOnActionExecuted(_actionExecutedContext, item.Filter);
                    }
                }
                else
                {
                    // All action filters have run, execute the action method.
                    IActionResult result = null;

                    try
                    {
                        _diagnosticSource.BeforeActionMethod(
                            _controllerContext,
                            _actionExecutingContext.ActionArguments,
                            _actionExecutingContext.Controller);

                        var actionMethodInfo = _controllerContext.ActionDescriptor.MethodInfo;

                        var arguments = ControllerActionExecutor.PrepareArguments(
                            _actionExecutingContext.ActionArguments,
                            _executor);

                        _logger.ActionMethodExecuting(_actionExecutingContext, arguments);

                        var returnType = _executor.MethodReturnType;

                        if (returnType == typeof(void))
                        {
                            _executor.Execute(_controller, arguments);
                            result = new EmptyResult();
                        }
                        else if (returnType == typeof(Task))
                        {
                            await (Task)_executor.Execute(_controller, arguments);
                            result = new EmptyResult();
                        }
                        else if (_executor.TaskGenericType == typeof(IActionResult))
                        {
                            result = await (Task<IActionResult>)_executor.Execute(_controller, arguments);
                            if (result == null)
                            {
                                throw new InvalidOperationException(
                                    Resources.FormatActionResult_ActionReturnValueCannotBeNull(typeof(IActionResult)));
                            }
                        }
                        else if (_executor.IsTypeAssignableFromIActionResult)
                        {
                            if (_executor.IsMethodAsync)
                            {
                                result = (IActionResult)await _executor.ExecuteAsync(_controller, arguments);
                            }
                            else
                            {
                                result = (IActionResult)_executor.Execute(_controller, arguments);
                            }

                            if (result == null)
                            {
                                throw new InvalidOperationException(
                                    Resources.FormatActionResult_ActionReturnValueCannotBeNull(_executor.TaskGenericType ?? returnType));
                            }
                        }
                        else if (!_executor.IsMethodAsync)
                        {
                            var resultAsObject = _executor.Execute(_controller, arguments);
                            result = new ObjectResult(resultAsObject)
                            {
                                DeclaredType = returnType,
                            };
                        }
                        else if (_executor.TaskGenericType != null)
                        {
                            var resultAsObject = await _executor.ExecuteAsync(_controller, arguments);
                            result = new ObjectResult(resultAsObject)
                            {
                                DeclaredType = _executor.TaskGenericType,
                            };
                        }
                        else
                        {
                            // This will be the case for types which have derived from Task and Task<T> or non Task types.
                            throw new InvalidOperationException(Resources.FormatActionExecutor_UnexpectedTaskInstance(
                                _executor.MethodInfo.Name,
                                _executor.MethodInfo.DeclaringType));
                        }

                        _logger.ActionMethodExecuted(_actionExecutingContext, result);
                    }
                    finally
                    {
                        _diagnosticSource.AfterActionMethod(
                            _controllerContext,
                            _actionExecutingContext.ActionArguments,
                            _actionExecutingContext.Controller,
                            result);
                    }

                    _actionExecutedContext = new ActionExecutedContext(
                        _actionExecutingContext,
                        _filters,
                        _controller)
                    {
                        Result = result
                    };
                }
            }
            catch (Exception exception)
            {
                // Exceptions thrown by the action method OR filters bubble back up through ActionExcecutedContext.
                _actionExecutedContext = new ActionExecutedContext(
                    _actionExecutingContext,
                    _filters,
                    _controller)
                {
                    ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception)
                };
            }

            Debug.Assert(_actionExecutedContext != null);
        }

        private async Task InvokeAllResultFiltersAsync(IActionResult result)
        {
            _cursor.Reset();

            _resultExecutingContext = new ResultExecutingContext(_controllerContext, _filters, result, _controller);
            await InvokeResultFilterAsync();

            Debug.Assert(_resultExecutingContext != null);
            if (_resultExecutedContext.Exception != null && !_resultExecutedContext.ExceptionHandled)
            {
                // There's an unhandled exception in filters
                if (_resultExecutedContext.ExceptionDispatchInfo != null)
                {
                    _resultExecutedContext.ExceptionDispatchInfo.Throw();
                }
                else
                {
                    throw _resultExecutedContext.Exception;
                }
            }
        }

        private async Task<ResultExecutedContext> InvokeResultFilterAwaitedAsync()
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

            await InvokeResultFilterAsync();

            Debug.Assert(_resultExecutedContext != null);
            return _resultExecutedContext;
        }

        private async Task InvokeResultFilterAsync()
        {
            Debug.Assert(_resultExecutingContext != null);

            try
            {
                var item = _cursor.GetNextFilter<IResultFilter, IAsyncResultFilter>();
                if (item.FilterAsync != null)
                {
                    _diagnosticSource.BeforeOnResultExecution(_resultExecutingContext, item.FilterAsync);

                    await item.FilterAsync.OnResultExecutionAsync(_resultExecutingContext, InvokeResultFilterAwaitedAsync);

                    if (_resultExecutedContext == null || _resultExecutingContext.Cancel == true)
                    {
                        // Short-circuited by not calling next || Short-circuited by setting Cancel == true
                        _logger.ResourceFilterShortCircuited(item.FilterAsync);

                        _resultExecutedContext = new ResultExecutedContext(
                            _resultExecutingContext,
                            _filters,
                            _resultExecutingContext.Result,
                            _controller)
                        {
                            Canceled = true,
                        };
                    }

                    _diagnosticSource.AfterOnResultExecution(_resultExecutedContext, item.FilterAsync);
                }
                else if (item.Filter != null)
                {
                    _diagnosticSource.BeforeOnResultExecuting(_resultExecutingContext, item.Filter);

                    item.Filter.OnResultExecuting(_resultExecutingContext);

                    _diagnosticSource.AfterOnResultExecuting(_resultExecutingContext, item.Filter);

                    if (_resultExecutingContext.Cancel == true)
                    {
                        // Short-circuited by setting Cancel == true
                        _logger.ResourceFilterShortCircuited(item.Filter);

                        _resultExecutedContext = new ResultExecutedContext(
                            _resultExecutingContext,
                            _filters,
                            _resultExecutingContext.Result,
                            _controller)
                        {
                            Canceled = true,
                        };
                    }
                    else
                    {
                        await InvokeResultFilterAsync();
                        Debug.Assert(_resultExecutedContext != null);

                        _diagnosticSource.BeforeOnResultExecuted(_resultExecutedContext, item.Filter);

                        item.Filter.OnResultExecuted(_resultExecutedContext);

                        _diagnosticSource.AfterOnResultExecuted(_resultExecutedContext, item.Filter);
                    }
                }
                else
                {
                    _cursor.Reset();

                    // The empty result is always flowed back as the 'executed' result
                    if (_resultExecutingContext.Result == null)
                    {
                        _resultExecutingContext.Result = new EmptyResult();
                    }

                    await InvokeResultAsync(_resultExecutingContext.Result);

                    Debug.Assert(_resultExecutedContext == null);
                    _resultExecutedContext = new ResultExecutedContext(
                        _resultExecutingContext,
                        _filters,
                        _resultExecutingContext.Result,
                        _controller);
                }
            }
            catch (Exception exception)
            {
                _resultExecutedContext = new ResultExecutedContext(
                    _resultExecutingContext,
                    _filters,
                    _resultExecutingContext.Result,
                    _controller)
                {
                    ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception)
                };
            }

            Debug.Assert(_resultExecutedContext != null);
        }

        private async Task InvokeResultAsync(IActionResult result)
        {
            _diagnosticSource.BeforeActionResult(_controllerContext, result);

            try
            {
                await result.ExecuteResultAsync(_controllerContext);
            }
            finally
            {
                _diagnosticSource.AfterActionResult(_controllerContext, result);
            }
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
