// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageActionInvoker : ResourceInvoker, IActionInvoker
    {
        private readonly IPageHandlerMethodSelector _selector;
        private readonly PageContext _pageContext;
        private readonly ParameterBinder _parameterBinder;
        private readonly ITempDataDictionaryFactory _tempDataFactory;
        private readonly HtmlHelperOptions _htmlHelperOptions;

        private CompiledPageActionDescriptor _actionDescriptor;
        private Page _page;
        private object _model;
        private ViewContext _viewContext;
        private ExceptionContext _exceptionContext;

        public PageActionInvoker(
            IPageHandlerMethodSelector handlerMethodSelector,
            DiagnosticSource diagnosticSource,
            ILogger logger,
            PageContext pageContext,
            IFilterMetadata[] filterMetadata,
            IList<IValueProviderFactory> valueProviderFactories,
            PageActionInvokerCacheEntry cacheEntry,
            ParameterBinder parameterBinder,
            ITempDataDictionaryFactory tempDataFactory,
            HtmlHelperOptions htmlHelperOptions)
            : base(
                  diagnosticSource,
                  logger,
                  pageContext,
                  filterMetadata,
                  valueProviderFactories)
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

        // Internal for testing
        internal PageContext PageContext => _pageContext;

        /// <remarks>
        /// <see cref="ResourceInvoker"/> for details on what the variables in this method represent.
        /// </remarks>
        protected override async Task InvokeInnerFilterAsync()
        {
            var next = State.ResourceInnerBegin;
            var scope = Scope.Resource;
            var state = (object)null;
            var isCompleted = false;

            while (!isCompleted)
            {
                await Next(ref next, ref scope, ref state, ref isCompleted);
            }
        }

        protected override void ReleaseResources()
        {
            if (_model != null && CacheEntry.ReleaseModel != null)
            {
                CacheEntry.ReleaseModel(_pageContext, _model);
            }

            if (_page != null && CacheEntry.ReleasePage != null)
            {
                CacheEntry.ReleasePage(_pageContext, _viewContext, _page);
            }
        }

        private Task Next(ref State next, ref Scope scope, ref object state, ref bool isCompleted)
        {
            var diagnosticSource = _diagnosticSource;
            var logger = _logger;

            switch (next)
            {
                case State.ResourceInnerBegin:
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
                            goto case State.PageBegin;
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
                        goto case State.PageBegin;
                    }

                case State.ExceptionShortCircuit:
                    {
                        Debug.Assert(state != null);
                        Debug.Assert(_exceptionContext != null);

                        if (scope == Scope.Resource)
                        {
                            Debug.Assert(_exceptionContext.Result != null);
                            _result = _exceptionContext.Result;
                        }

                        var task = InvokeResultAsync(_exceptionContext.Result);
                        if (task.Status != TaskStatus.RanToCompletion)
                        {
                            next = State.ResourceInnerEnd;
                            return task;
                        }

                        goto case State.ResourceInnerEnd;
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

                        goto case State.ResourceInnerEnd;
                    }

                case State.PageBegin:
                    {
                        var pageContext = _pageContext;

                        _cursor.Reset();

                        next = State.PageEnd;
                        return ExecutePageAsync();
                    }

                case State.PageEnd:
                    {
                        if (scope == Scope.Exception)
                        {
                            // If we're inside an exception filter, let's allow those filters to 'unwind' before
                            // the result.
                            isCompleted = true;
                            return TaskCache.CompletedTask;
                        }

                        Debug.Assert(scope == Scope.Resource);
                        goto case State.ResourceInnerEnd;
                    }

                case State.ResourceInnerEnd:
                    {
                        isCompleted = true;
                        return TaskCache.CompletedTask;
                    }

                default:
                    throw new InvalidOperationException();
            }
        }

        private Task ExecutePageAsync()
        {
            _pageContext.ValueProviderFactories = _valueProviderFactories;

            // There's a fork in the road here between the case where we have a full-fledged PageModel
            // vs just a Page. We need to know up front because we want to execute handler methods
            // on the PageModel without instantiating the Page or ViewContext.
            var hasPageModel = _actionDescriptor.HandlerTypeInfo != _actionDescriptor.PageTypeInfo;
            if (hasPageModel)
            {
                return ExecutePageWithPageModelAsync();
            }
            else
            {
                return ExecutePageWithoutPageModelAsync();
            }
        }

        private async Task ExecutePageWithPageModelAsync()
        {
            // Since this is a PageModel, we need to activate it, and then run a handler method on the model.
            //
            // We also know that the model is the pagemodel at this point.
            Debug.Assert(_actionDescriptor.ModelTypeInfo == _actionDescriptor.HandlerTypeInfo);
            _model = CacheEntry.ModelFactory(_pageContext);
            _pageContext.ViewData.Model = _model;

            if (CacheEntry.PropertyBinder != null)
            {
                await CacheEntry.PropertyBinder(_pageContext, _model);
            }

            // This is a workaround for not yet having proper filter for Pages.
            PageSaveTempDataPropertyFilter propertyFilter = null;
            for (var i = 0; i < _filters.Length; i++)
            {
                propertyFilter = _filters[i] as PageSaveTempDataPropertyFilter;
                if (propertyFilter != null)
                {
                    break;
                }
            }

            if (propertyFilter != null)
            {
                propertyFilter.Subject = _model;
                propertyFilter.ApplyTempDataChanges(_pageContext.HttpContext);
            }

            _result = await ExecuteHandlerMethod(_model);
            if (_result is PageResult pageResult)
            {
                // If we get here, we are going to render the page, so we need to create it and then initialize
                // the context so we can run the result.
                _viewContext = new ViewContext(
                    _pageContext,
                    NullView.Instance,
                    _pageContext.ViewData,
                    _tempDataFactory.GetTempData(_pageContext.HttpContext),
                    TextWriter.Null,
                    _htmlHelperOptions);

                _page = (Page)CacheEntry.PageFactory(_pageContext, _viewContext);

                pageResult.Page = _page;
                pageResult.ViewData = pageResult.ViewData ?? _pageContext.ViewData;
            }

            await _result.ExecuteResultAsync(_pageContext);
        }

        private async Task ExecutePageWithoutPageModelAsync()
        {
            // Since this is a Page without a PageModel, we need to create the Page before running a handler method.
            _viewContext = new ViewContext(
                _pageContext,
                NullView.Instance,
                _pageContext.ViewData,
                _tempDataFactory.GetTempData(_pageContext.HttpContext),
                TextWriter.Null,
                _htmlHelperOptions);

            _page = (Page)CacheEntry.PageFactory(_pageContext, _viewContext);

            if (_actionDescriptor.ModelTypeInfo == _actionDescriptor.PageTypeInfo)
            {
                _model = _page;
                _pageContext.ViewData.Model = _model;
            }

            if (CacheEntry.PropertyBinder != null)
            {
                await CacheEntry.PropertyBinder(_pageContext, _model);
            }

            // This is a workaround for not yet having proper filter for Pages.
            PageSaveTempDataPropertyFilter propertyFilter = null;
            for (var i = 0; i < _filters.Length; i++)
            {
                propertyFilter = _filters[i] as PageSaveTempDataPropertyFilter;
                if (propertyFilter != null)
                {
                    break;
                }
            }

            if (propertyFilter != null)
            {
                propertyFilter.Subject = _model;
                propertyFilter.ApplyTempDataChanges(_pageContext.HttpContext);
            }

            _result = await ExecuteHandlerMethod(_model);
            if (_result is PageResult pageResult)
            {
                // If we get here we're going to render the page so we need to initialize the context.
                pageResult.Page = _page;
                pageResult.ViewData = pageResult.ViewData ?? _pageContext.ViewData;
            }

            await _result.ExecuteResultAsync(_pageContext);
        }

        private async Task<object[]> GetArguments(HandlerMethodDescriptor handler)
        {
            var arguments = new object[handler.Parameters.Count];
            var valueProvider = await CompositeValueProvider.CreateAsync(_pageContext, _pageContext.ValueProviderFactories);

            for (var i = 0; i < handler.Parameters.Count; i++)
            {
                var parameter = handler.Parameters[i];

                var result = await _parameterBinder.BindModelAsync(
                    _pageContext,
                    valueProvider,
                    parameter,
                    value: null);

                if (result.IsModelSet)
                {
                    arguments[i] = result.Model;
                }
                else if (parameter.ParameterInfo.HasDefaultValue)
                {
                    arguments[i] = parameter.ParameterInfo.DefaultValue;
                }
                else if (parameter.ParameterType.GetTypeInfo().IsValueType)
                {
                    arguments[i] = Activator.CreateInstance(parameter.ParameterType);
                }
            }

            return arguments;
        }

        private async Task<IActionResult> ExecuteHandlerMethod(object instance)
        {
            IActionResult result = null;

            var handler = _selector.Select(_pageContext);
            if (handler != null)
            {
                var arguments = await GetArguments(handler);

                Func<object, object[], Task<IActionResult>> executor = null;
                for (var i = 0; i < _actionDescriptor.HandlerMethods.Count; i++)
                {
                    if (object.ReferenceEquals(handler, _actionDescriptor.HandlerMethods[i]))
                    {
                        executor = CacheEntry.Executors[i];
                        break;
                    }
                }
                
                result = await executor(instance, arguments);
            }

            if (result == null)
            {
                result = new PageResult();
            }

            return result;
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

        private enum Scope
        {
            Resource,
            Exception,
            Page,
        }

        private enum State
        {
            ResourceInnerBegin,
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
            PageBegin,
            PageEnd,
            ResourceInnerEnd,
        }
    }
}
