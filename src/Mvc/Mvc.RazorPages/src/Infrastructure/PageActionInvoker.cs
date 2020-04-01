// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    internal class PageActionInvoker : ResourceInvoker, IActionInvoker
    {
        private readonly IPageHandlerMethodSelector _selector;
        private readonly PageContext _pageContext;
        private readonly ParameterBinder _parameterBinder;
        private readonly ITempDataDictionaryFactory _tempDataFactory;
        private readonly HtmlHelperOptions _htmlHelperOptions;
        private readonly CompiledPageActionDescriptor _actionDescriptor;

        private Dictionary<string, object> _arguments;
        private HandlerMethodDescriptor _handler;
        private PageBase _page;
        private object _pageModel;
        private ViewContext _viewContext;

        private PageHandlerSelectedContext _handlerSelectedContext;
        private PageHandlerExecutingContext _handlerExecutingContext;
        private PageHandlerExecutedContext _handlerExecutedContext;

        public PageActionInvoker(
            IPageHandlerMethodSelector handlerMethodSelector,
            DiagnosticListener diagnosticListener,
            ILogger logger,
            IActionContextAccessor actionContextAccessor,
            IActionResultTypeMapper mapper,
            PageContext pageContext,
            IFilterMetadata[] filterMetadata,
            PageActionInvokerCacheEntry cacheEntry,
            ParameterBinder parameterBinder,
            ITempDataDictionaryFactory tempDataFactory,
            HtmlHelperOptions htmlHelperOptions)
            : base(
                  diagnosticListener,
                  logger,
                  actionContextAccessor,
                  mapper,
                  pageContext,
                  filterMetadata,
                  pageContext.ValueProviderFactories)
        {
            _selector = handlerMethodSelector;
            _pageContext = pageContext;
            CacheEntry = cacheEntry;
            _parameterBinder = parameterBinder;
            _tempDataFactory = tempDataFactory;
            _htmlHelperOptions = htmlHelperOptions;

            _actionDescriptor = pageContext.ActionDescriptor;
        }

        // Internal for testing
        internal PageActionInvokerCacheEntry CacheEntry { get; }

        private bool HasPageModel => _actionDescriptor.HandlerTypeInfo != _actionDescriptor.PageTypeInfo;

        // Internal for testing
        internal PageContext PageContext => _pageContext;

        /// <remarks>
        /// <see cref="ResourceInvoker"/> for details on what the variables in this method represent.
        /// </remarks>
        protected override async Task InvokeInnerFilterAsync()
        {
            var next = State.PageBegin;
            var scope = Scope.Invoker;
            var state = (object)null;
            var isCompleted = false;

            while (!isCompleted)
            {
                await Next(ref next, ref scope, ref state, ref isCompleted);
            }
        }

        protected override void ReleaseResources()
        {
            if (_pageModel != null && CacheEntry.ReleaseModel != null)
            {
                CacheEntry.ReleaseModel(_pageContext, _pageModel);
            }

            if (_page != null && CacheEntry.ReleasePage != null)
            {
                CacheEntry.ReleasePage(_pageContext, _viewContext, _page);
            }
        }

        protected override Task InvokeResultAsync(IActionResult result)
        {
            // We also have some special initialization we need to do for PageResult.
            if (result is PageResult pageResult)
            {
                // If we used a PageModel then the Page isn't initialized yet.
                if (_viewContext == null)
                {
                    _viewContext = new ViewContext(
                        _pageContext,
                        NullView.Instance,
                        _pageContext.ViewData,
                        _tempDataFactory.GetTempData(_pageContext.HttpContext),
                        TextWriter.Null,
                        _htmlHelperOptions);
                    _viewContext.ExecutingFilePath = _pageContext.ActionDescriptor.RelativePath;
                }

                if (_page == null)
                {
                    _page = (PageBase)CacheEntry.PageFactory(_pageContext, _viewContext);
                }
                pageResult.Page = _page;
                pageResult.ViewData = pageResult.ViewData ?? _pageContext.ViewData;
            }

            return base.InvokeResultAsync(result);
        }

        private object CreateInstance()
        {
            if (HasPageModel)
            {
                _logger.ExecutingPageModelFactory(_pageContext);

                // Since this is a PageModel, we need to activate it, and then run a handler method on the model.
                _pageModel = CacheEntry.ModelFactory(_pageContext);

                _logger.ExecutedPageModelFactory(_pageContext);

                _pageContext.ViewData.Model = _pageModel;

                return _pageModel;
            }
            else
            {
                // Since this is a Page without a PageModel, we need to create the Page before running a handler method.
                _viewContext = new ViewContext(
                    _pageContext,
                    NullView.Instance,
                    _pageContext.ViewData,
                    _tempDataFactory.GetTempData(_pageContext.HttpContext),
                    TextWriter.Null,
                    _htmlHelperOptions);
                _viewContext.ExecutingFilePath = _pageContext.ActionDescriptor.RelativePath;

                _logger.ExecutingPageFactory(_pageContext);

                _page = (PageBase)CacheEntry.PageFactory(_pageContext, _viewContext);

                _logger.ExecutedPageFactory(_pageContext);

                if (_actionDescriptor.ModelTypeInfo == _actionDescriptor.PageTypeInfo)
                {
                    _pageContext.ViewData.Model = _page;
                }

                return _page;
            }
        }

        private HandlerMethodDescriptor SelectHandler()
        {
            return _selector.Select(_pageContext);
        }

        private Task BindArgumentsAsync()
        {
            // Perf: Avoid allocating async state machines where possible. We only need the state
            // machine if you need to bind properties or arguments.
            if (_actionDescriptor.BoundProperties.Count == 0 && (_handler == null || _handler.Parameters.Count == 0))
            {
                return Task.CompletedTask;
            }

            return BindArgumentsCoreAsync();
        }

        private async Task BindArgumentsCoreAsync()
        {
            await CacheEntry.PropertyBinder(_pageContext, _instance);

            if (_handler == null)
            {
                return;
            }

            // We do two separate cache lookups, once for the binder and once for the executor.
            // Reducing it to a single lookup requires a lot of code change with little value.
            PageHandlerBinderDelegate handlerBinder = null;
            for (var i = 0; i < _actionDescriptor.HandlerMethods.Count; i++)
            {
                if (object.ReferenceEquals(_handler, _actionDescriptor.HandlerMethods[i]))
                {
                    handlerBinder = CacheEntry.HandlerBinders[i];
                    break;
                }
            }

            await handlerBinder(_pageContext, _arguments);
        }

        private static object[] PrepareArguments(
            IDictionary<string, object> argumentsInDictionary,
            HandlerMethodDescriptor handler)
        {
            if (handler.Parameters.Count == 0)
            {
                return null;
            }

            var arguments = new object[handler.Parameters.Count];
            for (var i = 0; i < arguments.Length; i++)
            {
                var parameter = handler.Parameters[i];

                if (argumentsInDictionary.TryGetValue(parameter.ParameterInfo.Name, out var value))
                {
                    // Do nothing, already set the value.
                }
                else if (!ParameterDefaultValue.TryGetDefaultValue(parameter.ParameterInfo, out value) &&
                    parameter.ParameterInfo.ParameterType.IsValueType)
                {
                    value = Activator.CreateInstance(parameter.ParameterInfo.ParameterType);
                }

                arguments[i] = value;
            }

            return arguments;
        }

        private async Task InvokeHandlerMethodAsync()
        {
            var handler = _handler;
            if (_handler != null)
            {
                var arguments = PrepareArguments(_arguments, handler);

                PageHandlerExecutorDelegate executor = null;
                for (var i = 0; i < _actionDescriptor.HandlerMethods.Count; i++)
                {
                    if (object.ReferenceEquals(handler, _actionDescriptor.HandlerMethods[i]))
                    {
                        executor = CacheEntry.HandlerExecutors[i];
                        break;
                    }
                }

                Debug.Assert(executor != null, "We should always find a executor for a handler");

                _diagnosticListener.BeforeHandlerMethod(_pageContext, handler, _arguments, _instance);
                _logger.ExecutingHandlerMethod(_pageContext, handler, arguments);

                try
                {
                    _result = await executor(_instance, arguments);
                    _logger.ExecutedHandlerMethod(_pageContext, handler, _result);
                }
                finally
                {
                    _diagnosticListener.AfterHandlerMethod(_pageContext, handler, _arguments, _instance, _result);
                }
            }

            // Pages have an implicit 'return Page()' even without a handler method.
            if (_result == null)
            {
                _logger.ExecutingImplicitHandlerMethod(_pageContext);
                _result = new PageResult();
                _logger.ExecutedImplicitHandlerMethod(_result);
            }

            // Ensure ViewData is set on PageResult for backwards compatibility (For example, Identity UI accesses
            // ViewData in a PageFilter's PageHandlerExecutedMethod)
            if (_result is PageResult pageResult)
            {
                pageResult.ViewData = pageResult.ViewData ?? _pageContext.ViewData;
            }
        }

        private Task Next(ref State next, ref Scope scope, ref object state, ref bool isCompleted)
        {
            switch (next)
            {
                case State.PageBegin:
                    {
                        _instance = CreateInstance();

                        goto case State.PageSelectHandlerBegin;
                    }

                case State.PageSelectHandlerBegin:
                    {
                        _cursor.Reset();

                        _handler = SelectHandler();

                        goto case State.PageSelectHandlerNext;
                    }

                case State.PageSelectHandlerNext:

                    var currentSelector = _cursor.GetNextFilter<IPageFilter, IAsyncPageFilter>();
                    if (currentSelector.FilterAsync != null)
                    {
                        if (_handlerSelectedContext == null)
                        {
                            _handlerSelectedContext = new PageHandlerSelectedContext(_pageContext, _filters, _instance)
                            {
                                HandlerMethod = _handler,
                            };
                        }

                        state = currentSelector.FilterAsync;
                        goto case State.PageSelectHandlerAsyncBegin;
                    }
                    else if (currentSelector.Filter != null)
                    {
                        if (_handlerSelectedContext == null)
                        {
                            _handlerSelectedContext = new PageHandlerSelectedContext(_pageContext, _filters, _instance)
                            {
                                HandlerMethod = _handler,
                            };
                        }

                        state = currentSelector.Filter;
                        goto case State.PageSelectHandlerSync;
                    }
                    else
                    {
                        goto case State.PageSelectHandlerEnd;
                    }

                case State.PageSelectHandlerAsyncBegin:
                    {
                        Debug.Assert(state != null);
                        Debug.Assert(_handlerSelectedContext != null);

                        var filter = (IAsyncPageFilter)state;
                        var handlerSelectedContext = _handlerSelectedContext;

                        _diagnosticListener.BeforeOnPageHandlerSelection(handlerSelectedContext, filter);
                        _logger.BeforeExecutingMethodOnFilter(
                            PageLoggerExtensions.PageFilter,
                            nameof(IAsyncPageFilter.OnPageHandlerSelectionAsync),
                            filter);

                        var task = filter.OnPageHandlerSelectionAsync(handlerSelectedContext);
                        if (task.Status != TaskStatus.RanToCompletion)
                        {
                            next = State.PageSelectHandlerAsyncEnd;
                            return task;
                        }

                        goto case State.PageSelectHandlerAsyncEnd;
                    }

                case State.PageSelectHandlerAsyncEnd:
                    {
                        Debug.Assert(state != null);
                        Debug.Assert(_handlerSelectedContext != null);

                        var filter = (IAsyncPageFilter)state;

                        _diagnosticListener.AfterOnPageHandlerSelection(_handlerSelectedContext, filter);
                        _logger.AfterExecutingMethodOnFilter(
                            PageLoggerExtensions.PageFilter,
                            nameof(IAsyncPageFilter.OnPageHandlerSelectionAsync),
                            filter);

                        goto case State.PageSelectHandlerNext;
                    }

                case State.PageSelectHandlerSync:
                    {
                        Debug.Assert(state != null);
                        Debug.Assert(_handlerSelectedContext != null);

                        var filter = (IPageFilter)state;
                        var handlerSelectedContext = _handlerSelectedContext;

                        _diagnosticListener.BeforeOnPageHandlerSelected(handlerSelectedContext, filter);
                        _logger.BeforeExecutingMethodOnFilter(
                            PageLoggerExtensions.PageFilter,
                            nameof(IPageFilter.OnPageHandlerSelected),
                            filter);

                        filter.OnPageHandlerSelected(handlerSelectedContext);

                        _diagnosticListener.AfterOnPageHandlerSelected(handlerSelectedContext, filter);

                        goto case State.PageSelectHandlerNext;
                    }

                case State.PageSelectHandlerEnd:
                    {
                        if (_handlerSelectedContext != null)
                        {
                            _handler = _handlerSelectedContext.HandlerMethod;
                        }

                        _arguments = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                        _cursor.Reset();

                        var task = BindArgumentsAsync();
                        if (task.Status != TaskStatus.RanToCompletion)
                        {
                            next = State.PageNext;
                            return task;
                        }

                        goto case State.PageNext;
                    }

                case State.PageNext:
                    {
                        var current = _cursor.GetNextFilter<IPageFilter, IAsyncPageFilter>();
                        if (current.FilterAsync != null)
                        {
                            if (_handlerExecutingContext == null)
                            {
                                _handlerExecutingContext = new PageHandlerExecutingContext(_pageContext, _filters, _handler, _arguments, _instance);
                            }

                            state = current.FilterAsync;
                            goto case State.PageAsyncBegin;
                        }
                        else if (current.Filter != null)
                        {
                            if (_handlerExecutingContext == null)
                            {
                                _handlerExecutingContext = new PageHandlerExecutingContext(_pageContext, _filters, _handler, _arguments, _instance);
                            }

                            state = current.Filter;
                            goto case State.PageSyncBegin;
                        }
                        else
                        {
                            goto case State.PageInside;
                        }
                    }

                case State.PageAsyncBegin:
                    {
                        Debug.Assert(state != null);
                        Debug.Assert(_handlerExecutingContext != null);

                        var filter = (IAsyncPageFilter)state;
                        var handlerExecutingContext = _handlerExecutingContext;

                        _diagnosticListener.BeforeOnPageHandlerExecution(handlerExecutingContext, filter);
                        _logger.BeforeExecutingMethodOnFilter(
                            PageLoggerExtensions.PageFilter,
                            nameof(IAsyncPageFilter.OnPageHandlerExecutionAsync),
                            filter);

                        var task = filter.OnPageHandlerExecutionAsync(handlerExecutingContext, InvokeNextPageFilterAwaitedAsync);
                        if (task.Status != TaskStatus.RanToCompletion)
                        {
                            next = State.PageAsyncEnd;
                            return task;
                        }

                        goto case State.PageAsyncEnd;
                    }

                case State.PageAsyncEnd:
                    {
                        Debug.Assert(state != null);
                        Debug.Assert(_handlerExecutingContext != null);

                        var filter = (IAsyncPageFilter)state;

                        if (_handlerExecutedContext == null)
                        {
                            // If we get here then the filter didn't call 'next' indicating a short circuit.
                            _logger.PageFilterShortCircuited(filter);

                            _handlerExecutedContext = new PageHandlerExecutedContext(
                                _pageContext,
                                _filters,
                                _handler,
                                _instance)
                            {
                                Canceled = true,
                                Result = _handlerExecutingContext.Result,
                            };
                        }

                        _diagnosticListener.AfterOnPageHandlerExecution(_handlerExecutedContext, filter);
                        _logger.AfterExecutingMethodOnFilter(
                           PageLoggerExtensions.PageFilter,
                           nameof(IAsyncPageFilter.OnPageHandlerExecutionAsync),
                           filter);

                        goto case State.PageEnd;
                    }

                case State.PageSyncBegin:
                    {
                        Debug.Assert(state != null);
                        Debug.Assert(_handlerExecutingContext != null);

                        var filter = (IPageFilter)state;
                        var handlerExecutingContext = _handlerExecutingContext;

                        _diagnosticListener.BeforeOnPageHandlerExecuting(handlerExecutingContext, filter);
                        _logger.BeforeExecutingMethodOnFilter(
                           PageLoggerExtensions.PageFilter,
                           nameof(IPageFilter.OnPageHandlerExecuting),
                           filter);

                        filter.OnPageHandlerExecuting(handlerExecutingContext);

                        _diagnosticListener.AfterOnPageHandlerExecuting(handlerExecutingContext, filter);
                        _logger.AfterExecutingMethodOnFilter(
                           PageLoggerExtensions.PageFilter,
                           nameof(IPageFilter.OnPageHandlerExecuting),
                           filter);

                        if (handlerExecutingContext.Result != null)
                        {
                            // Short-circuited by setting a result.
                            _logger.PageFilterShortCircuited(filter);

                            _handlerExecutedContext = new PageHandlerExecutedContext(
                                _pageContext,
                                _filters,
                                _handler,
                                _instance)
                            {
                                Canceled = true,
                                Result = _handlerExecutingContext.Result,
                            };

                            goto case State.PageEnd;
                        }

                        var task = InvokeNextPageFilterAsync();
                        if (task.Status != TaskStatus.RanToCompletion)
                        {
                            next = State.PageSyncEnd;
                            return task;
                        }

                        goto case State.PageSyncEnd;
                    }

                case State.PageSyncEnd:
                    {
                        Debug.Assert(state != null);
                        Debug.Assert(_handlerExecutingContext != null);
                        Debug.Assert(_handlerExecutedContext != null);

                        var filter = (IPageFilter)state;
                        var handlerExecutedContext = _handlerExecutedContext;

                        _diagnosticListener.BeforeOnPageHandlerExecuted(handlerExecutedContext, filter);
                        _logger.BeforeExecutingMethodOnFilter(
                           PageLoggerExtensions.PageFilter,
                           nameof(IPageFilter.OnPageHandlerExecuted),
                           filter);

                        filter.OnPageHandlerExecuted(handlerExecutedContext);

                        _diagnosticListener.AfterOnPageHandlerExecuted(handlerExecutedContext, filter);
                        _logger.AfterExecutingMethodOnFilter(
                           PageLoggerExtensions.PageFilter,
                           nameof(IPageFilter.OnPageHandlerExecuted),
                           filter);

                        goto case State.PageEnd;
                    }

                case State.PageInside:
                    {
                        var task = InvokeHandlerMethodAsync();
                        if (task.Status != TaskStatus.RanToCompletion)
                        {
                            next = State.PageEnd;
                            return task;
                        }

                        goto case State.PageEnd;
                    }

                case State.PageEnd:
                    {
                        if (scope == Scope.Page)
                        {
                            if (_handlerExecutedContext == null)
                            {
                                _handlerExecutedContext = new PageHandlerExecutedContext(_pageContext, _filters, _handler, _instance)
                                {
                                    Result = _result,
                                };
                            }

                            isCompleted = true;
                            return Task.CompletedTask;
                        }

                        var handlerExecutedContext = _handlerExecutedContext;
                        Rethrow(handlerExecutedContext);

                        if (handlerExecutedContext != null)
                        {
                            _result = handlerExecutedContext.Result;
                        }

                        isCompleted = true;
                        return Task.CompletedTask;
                    }

                default:
                    throw new InvalidOperationException();
            }
        }

        private async Task InvokeNextPageFilterAsync()
        {
            try
            {
                var next = State.PageNext;
                var state = (object)null;
                var scope = Scope.Page;
                var isCompleted = false;
                while (!isCompleted)
                {
                    await Next(ref next, ref scope, ref state, ref isCompleted);
                }
            }
            catch (Exception exception)
            {
                _handlerExecutedContext = new PageHandlerExecutedContext(_pageContext, _filters, _handler, _instance)
                {
                    ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception),
                };
            }

            Debug.Assert(_handlerExecutedContext != null);
        }

        private async Task<PageHandlerExecutedContext> InvokeNextPageFilterAwaitedAsync()
        {
            Debug.Assert(_handlerExecutingContext != null);
            if (_handlerExecutingContext.Result != null)
            {
                // If we get here, it means that an async filter set a result AND called next(). This is forbidden.
                var message = Resources.FormatAsyncPageFilter_InvalidShortCircuit(
                    typeof(IAsyncPageFilter).Name,
                    nameof(PageHandlerExecutingContext.Result),
                    typeof(PageHandlerExecutingContext).Name,
                    typeof(PageHandlerExecutionDelegate).Name);

                throw new InvalidOperationException(message);
            }

            await InvokeNextPageFilterAsync();

            Debug.Assert(_handlerExecutedContext != null);
            return _handlerExecutedContext;
        }

        private static void Rethrow(PageHandlerExecutedContext context)
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
            Page,
        }

        private enum State
        {
            PageBegin,
            PageSelectHandlerBegin,
            PageSelectHandlerNext,
            PageSelectHandlerAsyncBegin,
            PageSelectHandlerAsyncEnd,
            PageSelectHandlerSync,
            PageSelectHandlerEnd,
            PageNext,
            PageAsyncBegin,
            PageAsyncEnd,
            PageSyncBegin,
            PageSyncEnd,
            PageInside,
            PageEnd,
        }
    }
}
