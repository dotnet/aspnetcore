// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    public abstract class FilterActionInvoker : IActionInvoker
    {
        private readonly INestedProviderManager<FilterProviderContext> _filterProvider;

        private IFilter[] _filters;
        private FilterCursor _cursor;

        private ExceptionContext _exceptionContext;

        private AuthorizationContext _authorizationContext;

        private ActionExecutingContext _actionExecutingContext;
        private ActionExecutedContext _actionExecutedContext;

        private ResultExecutingContext _resultExecutingContext;
        private ResultExecutedContext _resultExecutedContext;

        public FilterActionInvoker(
            [NotNull] ActionContext actionContext,
            [NotNull] INestedProviderManager<FilterProviderContext> filterProvider)
        {
            ActionContext = actionContext;
            _filterProvider = filterProvider;
        }

        protected ActionContext ActionContext { get; private set; }

        protected abstract Task<IActionResult> InvokeActionAsync(ActionExecutingContext actionExecutingContext);

        protected abstract Task<IDictionary<string, object>> GetActionArgumentsAsync([NotNull] ActionContext context);

        public virtual async Task InvokeAsync()
        {
            _filters = GetFilters();
            _cursor = new FilterCursor(_filters);

            // >> ExceptionFilters >> AuthorizationFilters >> ActionFilters >> Action
            await InvokeActionExceptionFilters();

            // If Exception Filters or Authorization Filters provide a result, it's a short-circuit, we don't execute
            // result filters around it.
            if (_authorizationContext.Result != null)
            {
                await _authorizationContext.Result.ExecuteResultAsync(ActionContext);
            }
            else if (_exceptionContext.Result != null)
            {
                await _exceptionContext.Result.ExecuteResultAsync(ActionContext);
            }
            else if (_exceptionContext.Exception != null)
            {
                // If we get here, this means that we have an unhandled exception
                if (_exceptionContext.ExceptionDispatchInfo != null)
                {
                    _exceptionContext.ExceptionDispatchInfo.Throw();
                }
                else
                {
                    throw _exceptionContext.Exception;
                }
            }
            else
            {
                var result = _actionExecutedContext.Result;

                // >> ResultFilters >> (Result)
                await InvokeActionResultWithFilters(result);
            }
        }

        private IFilter[] GetFilters()
        {
            var filterProviderContext = new FilterProviderContext(
                ActionContext,
                ActionContext.ActionDescriptor.FilterDescriptors.Select(fd => new FilterItem(fd)).ToList());

            _filterProvider.Invoke(filterProviderContext);

            return filterProviderContext.Results.Select(item => item.Filter).Where(filter => filter != null).ToArray();
        }

        private async Task InvokeActionExceptionFilters()
        {
            _cursor.SetStage(FilterStage.ExceptionFilters);

            await InvokeExceptionFilter();
        }

        private async Task InvokeExceptionFilter()
        {
            var current = _cursor.GetNextFilter<IExceptionFilter, IAsyncExceptionFilter>();
            if (current.FilterAsync != null)
            {
                // Exception filters run "on the way out" - so the filter is run after the rest of the
                // pipeline.
                await InvokeExceptionFilter();

                Contract.Assert(_exceptionContext != null);
                if (_exceptionContext.Exception != null)
                {
                    // Exception filters only run when there's an exception - unsetting it will short-circuit
                    // other exception filters.
                    await current.FilterAsync.OnExceptionAsync(_exceptionContext);
                }
            }
            else if (current.Filter != null)
            {
                // Exception filters run "on the way out" - so the filter is run after the rest of the
                // pipeline.
                await InvokeExceptionFilter();

                Contract.Assert(_exceptionContext != null);
                if (_exceptionContext.Exception != null)
                {
                    // Exception filters only run when there's an exception - unsetting it will short-circuit
                    // other exception filters.
                    current.Filter.OnException(_exceptionContext);
                }
            }
            else
            {
                // We've reached the 'end' of the exception filter pipeline - this means that one stack frame has
                // been built for each exception. When we return from here, these frames will either:
                //
                // 1) Call the filter (if we have an exception)
                // 2) No-op (if we don't have an exception)
                Contract.Assert(_exceptionContext == null);
                _exceptionContext = new ExceptionContext(ActionContext, _filters);

                try
                {
                    await InvokeActionAuthorizationFilters();

                    Contract.Assert(_authorizationContext != null);
                    if (_authorizationContext.Result == null)
                    {
                        // Authorization passed, run authorization filters and the action
                        await InvokeActionMethodWithFilters();

                        // Action filters might 'return' an unahndled exception instead of throwing
                        Contract.Assert(_actionExecutedContext != null);
                        if (_actionExecutedContext.Exception != null && !_actionExecutedContext.ExceptionHandled)
                        {
                            _exceptionContext.Exception = _actionExecutedContext.Exception;
                            if (_actionExecutedContext.ExceptionDispatchInfo != null)
                            {
                                _exceptionContext.ExceptionDispatchInfo = _actionExecutedContext.ExceptionDispatchInfo;
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    _exceptionContext.ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception);
                }
            }
        }

        private async Task InvokeActionAuthorizationFilters()
        {
            _cursor.SetStage(FilterStage.AuthorizationFilters);

            _authorizationContext = new AuthorizationContext(ActionContext, _filters);
            await InvokeAuthorizationFilter();
        }

        private async Task InvokeAuthorizationFilter()
        {
            // We should never get here if we already have a result.
            Contract.Assert(_authorizationContext != null);
            Contract.Assert(_authorizationContext.Result == null);

            var current = _cursor.GetNextFilter<IAuthorizationFilter, IAsyncAuthorizationFilter>();
            if (current.FilterAsync != null)
            {
                await current.FilterAsync.OnAuthorizationAsync(_authorizationContext);

                if (_authorizationContext.Result == null)
                {
                    // Only keep going if we don't have a result
                    await InvokeAuthorizationFilter();
                }
            }
            else if (current.Filter != null)
            {
                current.Filter.OnAuthorization(_authorizationContext);

                if (_authorizationContext.Result == null)
                {
                    // Only keep going if we don't have a result
                    await InvokeAuthorizationFilter();
                }
            }
            else
            {
                // We've run out of Authorization Filters - if we haven't short circuited by now then this
                // request is authorized.
            }
        }

        private async Task InvokeActionMethodWithFilters()
        {
            _cursor.SetStage(FilterStage.ActionFilters);
            var arguments = await GetActionArgumentsAsync(ActionContext);
            _actionExecutingContext = new ActionExecutingContext(ActionContext, _filters, arguments);
            await InvokeActionMethodFilter();
        }

        private async Task<ActionExecutedContext> InvokeActionMethodFilter()
        {
            Contract.Assert(_actionExecutingContext != null);
            if (_actionExecutingContext.Result != null)
            {
                // If we get here, it means that an async filter set a result AND called next(). This is forbidden.
                var message = Resources.FormatAsyncActionFilter_InvalidShortCircuit(
                    typeof(IAsyncActionFilter).Name,
                    "Result",
                    typeof(ActionExecutingContext).Name,
                    typeof(ActionExecutionDelegate).Name);

                throw new InvalidOperationException(message);
            }

            var item = _cursor.GetNextFilter<IActionFilter, IAsyncActionFilter>();
            try
            {
                if (item.FilterAsync != null)
                {
                    await item.FilterAsync.OnActionExecutionAsync(_actionExecutingContext, InvokeActionMethodFilter);

                    if (_actionExecutedContext == null)
                    {
                        // If we get here then the filter didn't call 'next' indicating a short circuit
                        _actionExecutedContext = new ActionExecutedContext(_actionExecutingContext, _filters)
                        {
                            Canceled = true,
                            Result = _actionExecutingContext.Result,
                        };
                    }
                }
                else if (item.Filter != null)
                {
                    item.Filter.OnActionExecuting(_actionExecutingContext);

                    if (_actionExecutingContext.Result != null)
                    {
                        // Short-circuited by setting a result.
                        _actionExecutedContext = new ActionExecutedContext(_actionExecutingContext, _filters)
                        {
                            Canceled = true,
                            Result = _actionExecutingContext.Result,
                        };
                    }
                    else
                    {
                        item.Filter.OnActionExecuted(await InvokeActionMethodFilter());
                    }
                }
                else
                {
                    // All action filters have run, execute the action method.
                    _actionExecutedContext = new ActionExecutedContext(_actionExecutingContext, _filters)
                    {
                        Result = await InvokeActionAsync(_actionExecutingContext),
                    };
                }
            }
            catch (Exception exception)
            {
                // Exceptions thrown by the action method OR filters bubble back up through ActionExcecutedContext.
                _actionExecutedContext = new ActionExecutedContext(_actionExecutingContext, _filters)
                {
                    ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception)
                };
            }
            return _actionExecutedContext;
        }

        private async Task InvokeActionResultWithFilters(IActionResult result)
        {
            _cursor.SetStage(FilterStage.ResultFilters);

            _resultExecutingContext = new ResultExecutingContext(ActionContext, _filters, result);
            await InvokeActionResultFilter();

            Contract.Assert(_resultExecutingContext != null);
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

        private async Task<ResultExecutedContext> InvokeActionResultFilter()
        {
            Contract.Assert(_resultExecutingContext != null);
            if (_resultExecutingContext.Cancel == true)
            {
                // If we get here, it means that an async filter set cancel == true AND called next().
                // This is forbidden.
                var message = Resources.FormatAsyncResultFilter_InvalidShortCircuit(
                    typeof(IAsyncResultFilter).Name,
                    "Cancel",
                    typeof(ResultExecutingContext).Name,
                    typeof(ResultExecutionDelegate).Name);

                throw new InvalidOperationException(message);
            }

            try
            {
                var item = _cursor.GetNextFilter<IResultFilter, IAsyncResultFilter>();
                if (item.FilterAsync != null)
                {
                    await item.FilterAsync.OnResultExecutionAsync(_resultExecutingContext, InvokeActionResultFilter);

                    if (_resultExecutedContext == null)
                    {
                        // Short-circuited by not calling next
                        _resultExecutedContext = new ResultExecutedContext(
                            _resultExecutingContext,
                            _filters,
                            _resultExecutingContext.Result)
                        {
                            Canceled = true,
                        };
                    }
                    else if (_resultExecutingContext.Cancel == true)
                    {
                        // Short-circuited by setting Cancel == true
                        _resultExecutedContext = new ResultExecutedContext(
                            _resultExecutingContext,
                            _filters,
                            _resultExecutingContext.Result)
                        {
                            Canceled = true,
                        };
                    }
                }
                else if (item.Filter != null)
                {
                    item.Filter.OnResultExecuting(_resultExecutingContext);

                    if (_resultExecutingContext.Cancel == true)
                    {
                        // Short-circuited by setting Cancel == true
                        _resultExecutedContext = new ResultExecutedContext(
                            _resultExecutingContext,
                            _filters,
                            _resultExecutingContext.Result)
                        {
                            Canceled = true,
                        };
                    }
                    else
                    {
                        item.Filter.OnResultExecuted(await InvokeActionResultFilter());
                    }
                }
                else
                {
                    await InvokeActionResult();

                    Contract.Assert(_resultExecutedContext == null);
                    _resultExecutedContext = new ResultExecutedContext(
                        _resultExecutingContext,
                        _filters,
                        _resultExecutingContext.Result);
                }
            }
            catch (Exception exception)
            {
                _resultExecutedContext = new ResultExecutedContext(
                    _resultExecutingContext,
                    _filters,
                    _resultExecutingContext.Result)
                {
                    ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception)
                };
            }

            return _resultExecutedContext;
        }

        private async Task InvokeActionResult()
        {
            _cursor.SetStage(FilterStage.ActionResult);

            // The empty result is always flowed back as the 'executed' result
            if (_resultExecutingContext.Result == null)
            {
                _resultExecutingContext.Result = new EmptyResult();
            }

            await _resultExecutingContext.Result.ExecuteResultAsync(_resultExecutingContext);
        }

        private enum FilterStage
        {
            Undefined,
            ExceptionFilters,
            AuthorizationFilters,
            ActionFilters,
            ActionMethod,
            ResultFilters,
            ActionResult
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
            private FilterStage _stage;
            private int _index;
            private readonly IFilter[] _filters;

            public FilterCursor(FilterStage stage, int index, IFilter[] filters)
            {
                _stage = stage;
                _index = index;
                _filters = filters;
            }

            public FilterCursor(IFilter[] filters)
            {
                _stage = FilterStage.Undefined;
                _index = 0;
                _filters = filters;
            }

            public void SetStage(FilterStage stage)
            {
                _stage = stage;
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
                        return new FilterCursorItem<TFilter, TFilterAsync>(_stage, _index, filter, filterAsync);
                    }
                }

                return default(FilterCursorItem<TFilter, TFilterAsync>);
            }

            public bool StillAt<TFilter, TFilterAsync>(FilterCursorItem<TFilter, TFilterAsync> current)
            {
                return current.Stage == _stage && current.Index == _index;
            }
        }

        private struct FilterCursorItem<TFilter, TFilterAsync>
        {
            public readonly FilterStage Stage;
            public readonly int Index;
            public readonly TFilter Filter;
            public readonly TFilterAsync FilterAsync;

            public FilterCursorItem(FilterStage stage, int index, TFilter filter, TFilterAsync filterAsync)
            {
                Stage = stage;
                Index = index;
                Filter = filter;
                FilterAsync = filterAsync;
            }
        }
    }
}
