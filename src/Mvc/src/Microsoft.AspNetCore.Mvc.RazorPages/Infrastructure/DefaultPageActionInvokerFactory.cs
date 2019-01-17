// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    internal class DefaultPageActionInvokerFactory : IPageActionInvokerFactory
    {
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly ITempDataDictionaryFactory _tempDataFactory;
        private readonly HtmlHelperOptions _htmlHelperOptions;
        private readonly IPageHandlerMethodSelector _selector;
        private readonly DiagnosticListener _diagnosticListener;
        private readonly ILogger<DefaultPageActionInvoker> _logger;
        private readonly IActionResultTypeMapper _mapper;
        private readonly IReadOnlyList<IValueProviderFactory> _valueProviderFactories;

        public DefaultPageActionInvokerFactory(
            IModelMetadataProvider modelMetadataProvider,
            ITempDataDictionaryFactory tempDataFactory,
            IPageHandlerMethodSelector selector,
            IActionResultTypeMapper mapper,
            IOptions<MvcOptions> mvcOptions,
            IOptions<HtmlHelperOptions> htmlHelperOptions,
            DiagnosticListener diagnosticListener,
            ILoggerFactory loggerFactory)
        {
            _valueProviderFactories = mvcOptions.Value.ValueProviderFactories.ToArray();
            _modelMetadataProvider = modelMetadataProvider;
            _tempDataFactory = tempDataFactory;
            _htmlHelperOptions = htmlHelperOptions.Value;
            _selector = selector;
            _diagnosticListener = diagnosticListener;
            _logger = loggerFactory.CreateLogger<DefaultPageActionInvoker>();
            _mapper = mapper;
        }

        public IActionInvoker CreateInvoker(
           ActionContext actionContext,
           PageActionInvokerCacheEntry cacheEntry,
           IFilterMetadata[] filters)
        {
            var pageContext = new PageContext(actionContext)
            {
                ActionDescriptor = cacheEntry.ActionDescriptor,
                ValueProviderFactories = new CopyOnWriteList<IValueProviderFactory>(_valueProviderFactories),
                ViewData = cacheEntry.ViewDataFactory(_modelMetadataProvider, actionContext.ModelState),
                ViewStartFactories = cacheEntry.ViewStartFactories.ToList(),
            };

            return new DefaultPageActionInvoker(
                _selector,
                _diagnosticListener,
                _logger,
                _mapper,
                pageContext,
                filters,
                cacheEntry,
                _tempDataFactory,
                _htmlHelperOptions);
        }
    }
}
