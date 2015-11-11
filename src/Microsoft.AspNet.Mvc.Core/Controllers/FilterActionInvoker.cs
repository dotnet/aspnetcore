// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Diagnostics;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Mvc.Formatters;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.AspNet.Mvc.Logging;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.Controllers
{
    public abstract class FilterActionInvoker : IActionInvoker
    {
        private static readonly IFilterMetadata[] EmptyFilterArray = new IFilterMetadata[0];

        private readonly IReadOnlyList<IFilterProvider> _filterProviders;
        private readonly IReadOnlyList<IInputFormatter> _inputFormatters;
        private readonly IReadOnlyList<IModelBinder> _modelBinders;
        private readonly IReadOnlyList<IModelValidatorProvider> _modelValidatorProviders;
        private readonly IReadOnlyList<IValueProviderFactory> _valueProviderFactories;
        private readonly DiagnosticSource _diagnosticSource;
        private readonly int _maxModelValidationErrors;

        private IFilterMetadata[] _filters;
        private FilterCursor _cursor;

        private AuthorizationContext _authorizationContext;

        private ResourceExecutingContext _resourceExecutingContext;
        private ResourceExecutedContext _resourceExecutedContext;

        private ExceptionContext _exceptionContext;

        private ActionExecutingContext _actionExecutingContext;
        private ActionExecutedContext _actionExecutedContext;

        private ResultExecutingContext _resultExecutingContext;
        private ResultExecutedContext _resultExecutedContext;

        public FilterActionInvoker(
            ActionContext actionContext,
            IReadOnlyList<IFilterProvider> filterProviders,
            IReadOnlyList<IInputFormatter> inputFormatters,
            IReadOnlyList<IModelBinder> modelBinders,
            IReadOnlyList<IModelValidatorProvider> modelValidatorProviders,
            IReadOnlyList<IValueProviderFactory> valueProviderFactories,
            ILogger logger,
            DiagnosticSource diagnosticSource,
            int maxModelValidationErrors)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            if (filterProviders == null)
            {
                throw new ArgumentNullException(nameof(filterProviders));
            }

            if (inputFormatters == null)
            {
                throw new ArgumentNullException(nameof(inputFormatters));
            }

            if (modelBinders == null)
            {
                throw new ArgumentNullException(nameof(modelBinders));
            }

            if (modelValidatorProviders == null)
            {
                throw new ArgumentNullException(nameof(modelValidatorProviders));
            }

            if (valueProviderFactories == null)
            {
                throw new ArgumentNullException(nameof(valueProviderFactories));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (diagnosticSource == null)
            {
                throw new ArgumentNullException(nameof(diagnosticSource));
            }

            Context = new ControllerContext(actionContext);

            _filterProviders = filterProviders;
            _inputFormatters = inputFormatters;
            _modelBinders = modelBinders;
            _modelValidatorProviders = modelValidatorProviders;
            _valueProviderFactories = valueProviderFactories;
            Logger = logger;
            _diagnosticSource = diagnosticSource;
            _maxModelValidationErrors = maxModelValidationErrors;
        }

        protected ControllerContext Context { get; }

        protected object Instance { get; private set; }

        protected ILogger Logger { get; }

        /// <summary>
        /// Called to create an instance of an object which will act as the reciever of the action invocation.
        /// </summary>
        /// <returns>The constructed instance or <c>null</c>.</returns>
        protected abstract object CreateInstance();

        /// <summary>
        /// Called to create an instance of an object which will act as the reciever of the action invocation.
        /// </summary>
        /// <param name="instance">The instance to release.</param>
        /// <remarks>This method will not be called if <see cref="CreateInstance"/> returns <c>null</c>.</remarks>
        protected abstract void ReleaseInstance(object instance);

        protected abstract Task<IActionResult> InvokeActionAsync(ActionExecutingContext actionExecutingContext);

        protected abstract Task<IDictionary<string, object>> BindActionArgumentsAsync();

        public virtual async Task InvokeAsync()
        {
            _filters = GetFilters();
            _cursor = new FilterCursor(_filters);

            Context.ModelState.MaxAllowedErrors = _maxModelValidationErrors;

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
                if (Instance != null)
                {
                    ReleaseInstance(Instance);
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
        }

        private IFilterMetadata[] GetFilters()
        {
            var filterDescriptors = Context.ActionDescriptor.FilterDescriptors;
            var items = new List<FilterItem>(filterDescriptors.Count);
            for (var i = 0; i < filterDescriptors.Count; i++)
            {
                items.Add(new FilterItem(filterDescriptors[i]));
            }

            var context = new FilterProviderContext(Context, items);
            for (var i = 0; i < _filterProviders.Count; i++)
            {
                _filterProviders[i].OnProvidersExecuting(context);
            }

            for (var i = _filterProviders.Count - 1; i >= 0; i--)
            {
                _filterProviders[i].OnProvidersExecuted(context);
            }

            var count = 0;
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].Filter != null)
                {
                    count++;
                }
            }

            if (count == 0)
            {
                return EmptyFilterArray;
            }
            else
            {
                var filters = new IFilterMetadata[count];
                for (int i = 0, j = 0; i < items.Count; i++)
                {
                    var filter = items[i].Filter;
                    if (filter != null)
                    {
                        filters[j++] = filter;
                    }
                }

                return filters;
            }
        }

        private Task InvokeAllAuthorizationFiltersAsync()
        {
            _cursor.Reset();

            _authorizationContext = new AuthorizationContext(Context, _filters);
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
                _diagnosticSource.BeforeOnAuthorizationAsync(
                    _authorizationContext,
                    current.FilterAsync);

                await current.FilterAsync.OnAuthorizationAsync(_authorizationContext);

                _diagnosticSource.AfterOnAuthorizationAsync(
                    _authorizationContext,
                    current.FilterAsync);

                if (_authorizationContext.Result == null)
                {
                    // Only keep going if we don't have a result
                    await InvokeAuthorizationFilterAsync();
                }
                else
                {
                    Logger.AuthorizationFailure(current.FilterAsync);
                }
            }
            else if (current.Filter != null)
            {
                _diagnosticSource.BeforeOnAuthorization(
                    _authorizationContext,
                    current.Filter);

                current.Filter.OnAuthorization(_authorizationContext);

                _diagnosticSource.AfterOnAuthorization(
                    _authorizationContext,
                    current.Filter);

                if (_authorizationContext.Result == null)
                {
                    // Only keep going if we don't have a result
                    await InvokeAuthorizationFilterAsync();
                }
                else
                {
                    Logger.AuthorizationFailure(current.Filter);
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

            var context = new ResourceExecutingContext(Context, _filters);

            context.InputFormatters = new FormatterCollection<IInputFormatter>(
                new CopyOnWriteList<IInputFormatter>(_inputFormatters));
            context.ModelBinders = new CopyOnWriteList<IModelBinder>(_modelBinders);
            context.ValidatorProviders = new CopyOnWriteList<IModelValidatorProvider>(_modelValidatorProviders);
            context.ValueProviderFactories = new CopyOnWriteList<IValueProviderFactory>(_valueProviderFactories);

            _resourceExecutingContext = context;
            return InvokeResourceFilterAsync();
        }

        private async Task<ResourceExecutedContext> InvokeResourceFilterAsync()
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

            var item = _cursor.GetNextFilter<IResourceFilter, IAsyncResourceFilter>();
            try
            {
                if (item.FilterAsync != null)
                {
                    _diagnosticSource.BeforeOnResourceExecution(
                        _resourceExecutingContext,
                        item.FilterAsync);

                    await item.FilterAsync.OnResourceExecutionAsync(
                        _resourceExecutingContext,
                        InvokeResourceFilterAsync);

                    _diagnosticSource.AfterOnResourceExecution(
                        _resourceExecutingContext.ActionDescriptor,
                        _resourceExecutedContext,
                        item.FilterAsync);

                    if (_resourceExecutedContext == null)
                    {
                        // If we get here then the filter didn't call 'next' indicating a short circuit
                        if (_resourceExecutingContext.Result != null)
                        {
                            Logger.ResourceFilterShortCircuited(item.FilterAsync);

                            await InvokeResultAsync(_resourceExecutingContext.Result);
                        }

                        _resourceExecutedContext = new ResourceExecutedContext(_resourceExecutingContext, _filters)
                        {
                            Canceled = true,
                            Result = _resourceExecutingContext.Result,
                        };
                    }
                }
                else if (item.Filter != null)
                {
                    _diagnosticSource.BeforeOnResourceExecuting(
                        _resourceExecutingContext,
                        item.Filter);

                    item.Filter.OnResourceExecuting(_resourceExecutingContext);

                    _diagnosticSource.AfterOnResourceExecuting(
                        _resourceExecutingContext,
                        item.Filter);

                    if (_resourceExecutingContext.Result != null)
                    {
                        // Short-circuited by setting a result.
                        Logger.ResourceFilterShortCircuited(item.Filter);

                        await InvokeResultAsync(_resourceExecutingContext.Result);

                        _resourceExecutedContext = new ResourceExecutedContext(_resourceExecutingContext, _filters)
                        {
                            Canceled = true,
                            Result = _resourceExecutingContext.Result,
                        };
                    }
                    else
                    {
                        _diagnosticSource.BeforeOnResourceExecuted(
                            _resourceExecutingContext.ActionDescriptor,
                            _resourceExecutedContext,
                            item.Filter);

                        item.Filter.OnResourceExecuted(await InvokeResourceFilterAsync());

                        _diagnosticSource.AfterOnResourceExecuted(
                            _resourceExecutingContext.ActionDescriptor,
                            _resourceExecutedContext,
                            item.Filter);
                    }
                }
                else
                {
                    // We've reached the end of resource filters, so move to setting up state to invoke model
                    // binding.
                    Context.InputFormatters = _resourceExecutingContext.InputFormatters;
                    Context.ModelBinders = _resourceExecutingContext.ModelBinders;
                    Context.ValidatorProviders = _resourceExecutingContext.ValidatorProviders;

                    var valueProviders = new List<IValueProvider>();

                    for (var i = 0; i < _resourceExecutingContext.ValueProviderFactories.Count; i++)
                    {
                        var factory = _resourceExecutingContext.ValueProviderFactories[i];
                        var valueProvider = await factory.GetValueProviderAsync(Context);
                        if (valueProvider != null)
                        {
                            valueProviders.Add(valueProvider);
                        }
                    }
                    Context.ValueProviders = valueProviders;

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
            return _resourceExecutedContext;
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
                    _diagnosticSource.BeforeOnExceptionAsync(
                        _exceptionContext,
                        current.FilterAsync);

                    // Exception filters only run when there's an exception - unsetting it will short-circuit
                    // other exception filters.
                    await current.FilterAsync.OnExceptionAsync(_exceptionContext);

                    _diagnosticSource.AfterOnExceptionAsync(
                        _exceptionContext,
                        current.FilterAsync);

                    if (_exceptionContext.Exception == null)
                    {
                        Logger.ExceptionFilterShortCircuited(current.FilterAsync);
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
                    _diagnosticSource.BeforeOnException(
                        _exceptionContext,
                        current.Filter);

                    // Exception filters only run when there's an exception - unsetting it will short-circuit
                    // other exception filters.
                    current.Filter.OnException(_exceptionContext);

                    _diagnosticSource.AfterOnException(
                        _exceptionContext,
                        current.Filter);

                    if (_exceptionContext.Exception == null)
                    {
                        Logger.ExceptionFilterShortCircuited(current.Filter);
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
                _exceptionContext = new ExceptionContext(Context, _filters);

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

            Instance = CreateInstance();

            var arguments = await BindActionArgumentsAsync();

            _actionExecutingContext = new ActionExecutingContext(
                Context,
                _filters,
                arguments,
                Instance);

            await InvokeActionFilterAsync();
        }

        private async Task<ActionExecutedContext> InvokeActionFilterAsync()
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

            var item = _cursor.GetNextFilter<IActionFilter, IAsyncActionFilter>();
            try
            {
                if (item.FilterAsync != null)
                {
                    _diagnosticSource.BeforeOnActionExecution(
                        _actionExecutingContext,
                        item.FilterAsync);

                    await item.FilterAsync.OnActionExecutionAsync(_actionExecutingContext, InvokeActionFilterAsync);

                    _diagnosticSource.AfterOnActionExecution(
                        _actionExecutingContext.ActionDescriptor,
                        _actionExecutedContext,
                        item.FilterAsync);

                    if (_actionExecutedContext == null)
                    {
                        // If we get here then the filter didn't call 'next' indicating a short circuit
                        Logger.ActionFilterShortCircuited(item.FilterAsync);

                        _actionExecutedContext = new ActionExecutedContext(
                            _actionExecutingContext,
                            _filters,
                            Instance)
                        {
                            Canceled = true,
                            Result = _actionExecutingContext.Result,
                        };
                    }
                }
                else if (item.Filter != null)
                {
                    _diagnosticSource.BeforeOnActionExecuting(
                        _actionExecutingContext,
                        item.Filter);

                    item.Filter.OnActionExecuting(_actionExecutingContext);

                    _diagnosticSource.AfterOnActionExecuting(
                        _actionExecutingContext,
                        item.Filter);

                    if (_actionExecutingContext.Result != null)
                    {
                        // Short-circuited by setting a result.
                        Logger.ActionFilterShortCircuited(item.Filter);

                        _actionExecutedContext = new ActionExecutedContext(
                            _actionExecutingContext,
                            _filters,
                            Instance)
                        {
                            Canceled = true,
                            Result = _actionExecutingContext.Result,
                        };
                    }
                    else
                    {
                        _diagnosticSource.BeforeOnActionExecuted(
                            _actionExecutingContext.ActionDescriptor,
                            _actionExecutedContext,
                            item.Filter);

                        item.Filter.OnActionExecuted(await InvokeActionFilterAsync());

                        _diagnosticSource.BeforeOnActionExecuted(
                            _actionExecutingContext.ActionDescriptor,
                            _actionExecutedContext,
                            item.Filter);
                    }
                }
                else
                {
                    // All action filters have run, execute the action method.
                    IActionResult result = null;

                    try
                    {
                        _diagnosticSource.BeforeActionMethod(
                            Context,
                            _actionExecutingContext.ActionArguments,
                            _actionExecutingContext.Controller);

                        result = await InvokeActionAsync(_actionExecutingContext);
                    }
                    finally
                    {
                        _diagnosticSource.AfterActionMethod(
                            Context,
                            _actionExecutingContext.ActionArguments,
                            _actionExecutingContext.Controller,
                            result);
                    }

                    _actionExecutedContext = new ActionExecutedContext(
                        _actionExecutingContext,
                        _filters,
                        Instance)
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
                    Instance)
                {
                    ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception)
                };
            }
            return _actionExecutedContext;
        }

        private async Task InvokeAllResultFiltersAsync(IActionResult result)
        {
            _cursor.Reset();

            _resultExecutingContext = new ResultExecutingContext(Context, _filters, result, Instance);
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

        private async Task<ResultExecutedContext> InvokeResultFilterAsync()
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

            try
            {
                var item = _cursor.GetNextFilter<IResultFilter, IAsyncResultFilter>();
                if (item.FilterAsync != null)
                {
                    _diagnosticSource.BeforeOnResultExecution(
                        _resultExecutingContext,
                        item.FilterAsync);

                    await item.FilterAsync.OnResultExecutionAsync(_resultExecutingContext, InvokeResultFilterAsync);

                    _diagnosticSource.AfterOnResultExecution(
                        _resultExecutingContext.ActionDescriptor,
                        _resultExecutedContext,
                        item.FilterAsync);

                    if (_resultExecutedContext == null || _resultExecutingContext.Cancel == true)
                    {
                        // Short-circuited by not calling next || Short-circuited by setting Cancel == true
                        Logger.ResourceFilterShortCircuited(item.FilterAsync);

                        _resultExecutedContext = new ResultExecutedContext(
                            _resultExecutingContext,
                            _filters,
                            _resultExecutingContext.Result,
                            Instance)
                        {
                            Canceled = true,
                        };
                    }
                }
                else if (item.Filter != null)
                {
                    _diagnosticSource.BeforeOnResultExecuting(
                        _resultExecutingContext,
                        item.Filter);

                    item.Filter.OnResultExecuting(_resultExecutingContext);

                    _diagnosticSource.AfterOnResultExecuting(
                        _resultExecutingContext,
                        item.Filter);

                    if (_resultExecutingContext.Cancel == true)
                    {
                        // Short-circuited by setting Cancel == true
                        Logger.ResourceFilterShortCircuited(item.Filter);

                        _resultExecutedContext = new ResultExecutedContext(
                            _resultExecutingContext,
                            _filters,
                            _resultExecutingContext.Result,
                            Instance)
                        {
                            Canceled = true,
                        };
                    }
                    else
                    {
                        _diagnosticSource.BeforeOnResultExecuted(
                            _resultExecutingContext.ActionDescriptor,
                            _resultExecutedContext,
                            item.Filter);

                        item.Filter.OnResultExecuted(await InvokeResultFilterAsync());

                        _diagnosticSource.AfterOnResultExecuted(
                            _resultExecutingContext.ActionDescriptor,
                            _resultExecutedContext,
                            item.Filter);
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
                        Instance);
                }
            }
            catch (Exception exception)
            {
                _resultExecutedContext = new ResultExecutedContext(
                    _resultExecutingContext,
                    _filters,
                    _resultExecutingContext.Result,
                    Instance)
                {
                    ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception)
                };
            }

            return _resultExecutedContext;
        }

        private async Task InvokeResultAsync(IActionResult result)
        {
            _diagnosticSource.BeforeActionResult(Context, result);

            try
            {
                await result.ExecuteResultAsync(Context);
            }
            finally
            {
                _diagnosticSource.AfterActionResult(Context, result);
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
