// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    internal class PageRequestDelegateFactory : IRequestDelegateFactory
    {
        private readonly PageActionInvokerCache _cache;
        private readonly IReadOnlyList<IValueProviderFactory> _valueProviderFactories;
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly ITempDataDictionaryFactory _tempDataFactory;
        private readonly MvcViewOptions _mvcViewOptions;
        private readonly IPageHandlerMethodSelector _selector;
        private readonly DiagnosticListener _diagnosticListener;
        private readonly ILogger<PageActionInvoker> _logger;
        private readonly IActionResultTypeMapper _mapper;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly bool _enableActionInvokers;

        public PageRequestDelegateFactory(
            PageActionInvokerCache cache,
            IModelMetadataProvider modelMetadataProvider,
            ITempDataDictionaryFactory tempDataFactory,
            IOptions<MvcOptions> mvcOptions,
            IOptions<MvcViewOptions> mvcViewOptions,
            IPageHandlerMethodSelector selector,
            DiagnosticListener diagnosticListener,
            ILoggerFactory loggerFactory,
            IActionResultTypeMapper mapper,
            IActionContextAccessor actionContextAccessor = null)
        {
            _cache = cache;
            _valueProviderFactories = mvcOptions.Value.ValueProviderFactories.ToArray();
            _modelMetadataProvider = modelMetadataProvider;
            _tempDataFactory = tempDataFactory;
            _mvcViewOptions = mvcViewOptions.Value;
            _enableActionInvokers = mvcOptions.Value.EnableActionInvokers;
            _selector = selector;
            _diagnosticListener = diagnosticListener;
            _logger = loggerFactory.CreateLogger<PageActionInvoker>();
            _mapper = mapper;
            _actionContextAccessor = actionContextAccessor ?? ActionContextAccessor.Null;
        }

        public RequestDelegate CreateRequestDelegate(ActionDescriptor actionDescriptor, RouteValueDictionary dataTokens)
        {
            if (_enableActionInvokers || actionDescriptor is not CompiledPageActionDescriptor page)
            {
                return null;
            }

            // Compiled pages are the only ones that run

            return context =>
            {
                RouteData routeData = null;

                if (dataTokens is null or { Count: 0 })
                {
                    routeData = new RouteData(context.Request.RouteValues);
                }
                else
                {
                    routeData = new RouteData();
                    routeData.PushState(router: null, context.Request.RouteValues, dataTokens);
                }

                var actionContext = new ActionContext(context, routeData, page);

                var (cacheEntry, filters) = _cache.GetCachedResult(actionContext);

                var pageContext = new PageContext(actionContext)
                {
                    ActionDescriptor = cacheEntry.ActionDescriptor,
                    ValueProviderFactories = new CopyOnWriteList<IValueProviderFactory>(_valueProviderFactories),
                    ViewData = cacheEntry.ViewDataFactory(_modelMetadataProvider, actionContext.ModelState),
                    ViewStartFactories = cacheEntry.ViewStartFactories.ToList(),
                };

                var pageInvoker = new PageActionInvoker(
                    _selector,
                    _diagnosticListener,
                    _logger,
                    _actionContextAccessor,
                    _mapper,
                    pageContext,
                    filters,
                    cacheEntry,
                    _tempDataFactory,
                    _mvcViewOptions.HtmlHelperOptions);

                return pageInvoker.InvokeAsync();
            };
        }
    }
}
