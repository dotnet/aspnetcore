// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageActionInvokerProvider : IActionInvokerProvider
    {
        private const string PageStartFileName = "_PageStart.cshtml";
        private readonly IPageLoader _loader;
        private readonly IPageFactoryProvider _pageFactoryProvider;
        private readonly IPageModelFactoryProvider _modelFactoryProvider;
        private readonly IRazorPageFactoryProvider _razorPageFactoryProvider;
        private readonly IActionDescriptorCollectionProvider _collectionProvider;
        private readonly IFilterProvider[] _filterProviders;
        private readonly IReadOnlyList<IValueProviderFactory> _valueProviderFactories;
        private readonly ParameterBinder _parameterBinder;
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly ITempDataDictionaryFactory _tempDataFactory;
        private readonly HtmlHelperOptions _htmlHelperOptions;
        private readonly RazorPagesOptions _razorPagesOptions;
        private readonly IPageHandlerMethodSelector _selector;
        private readonly RazorProject _razorProject;
        private readonly DiagnosticSource _diagnosticSource;
        private readonly ILogger<PageActionInvoker> _logger;
        private volatile InnerCache _currentCache;

        public PageActionInvokerProvider(
            IPageLoader loader,
            IPageFactoryProvider pageFactoryProvider,
            IPageModelFactoryProvider modelFactoryProvider,
            IRazorPageFactoryProvider razorPageFactoryProvider,
            IActionDescriptorCollectionProvider collectionProvider,
            IEnumerable<IFilterProvider> filterProviders,
            ParameterBinder parameterBinder,
            IModelMetadataProvider modelMetadataProvider,
            ITempDataDictionaryFactory tempDataFactory,
            IOptions<MvcOptions> mvcOptions,
            IOptions<HtmlHelperOptions> htmlHelperOptions,
            IOptions<RazorPagesOptions> razorPagesOptions,
            IPageHandlerMethodSelector selector,
            RazorProject razorProject,
            DiagnosticSource diagnosticSource,
            ILoggerFactory loggerFactory)
        {
            _loader = loader;
            _pageFactoryProvider = pageFactoryProvider;
            _modelFactoryProvider = modelFactoryProvider;
            _razorPageFactoryProvider = razorPageFactoryProvider;
            _collectionProvider = collectionProvider;
            _filterProviders = filterProviders.ToArray();
            _valueProviderFactories = mvcOptions.Value.ValueProviderFactories.ToArray();
            _parameterBinder = parameterBinder;
            _modelMetadataProvider = modelMetadataProvider;
            _tempDataFactory = tempDataFactory;
            _htmlHelperOptions = htmlHelperOptions.Value;
            _razorPagesOptions = razorPagesOptions.Value;
            _selector = selector;
            _razorProject = razorProject;
            _diagnosticSource = diagnosticSource;
            _logger = loggerFactory.CreateLogger<PageActionInvoker>();
        }

        public int Order { get; } = -1000;

        public void OnProvidersExecuting(ActionInvokerProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var actionContext = context.ActionContext;
            var actionDescriptor = actionContext.ActionDescriptor as PageActionDescriptor;
            if (actionDescriptor == null)
            {
                return;
            }

            var cache = CurrentCache;
            PageActionInvokerCacheEntry cacheEntry;

            IFilterMetadata[] filters;
            if (!cache.Entries.TryGetValue(actionDescriptor, out cacheEntry))
            {
                var filterFactoryResult = FilterFactory.GetAllFilters(_filterProviders, actionContext);
                filters = filterFactoryResult.Filters;
                cacheEntry = CreateCacheEntry(context, filterFactoryResult.CacheableFilters);
                cacheEntry = cache.Entries.GetOrAdd(actionDescriptor, cacheEntry);
            }
            else
            {
                filters = FilterFactory.CreateUncachedFilters(
                    _filterProviders,
                    actionContext,
                    cacheEntry.CacheableFilters);
            }

            context.Result = CreateActionInvoker(actionContext, cacheEntry, filters);
        }

        public void OnProvidersExecuted(ActionInvokerProviderContext context)
        {

        }

        private InnerCache CurrentCache
        {
            get
            {
                var current = _currentCache;
                var actionDescriptors = _collectionProvider.ActionDescriptors;

                if (current == null || current.Version != actionDescriptors.Version)
                {
                    current = new InnerCache(actionDescriptors.Version);
                    _currentCache = current;
                }

                return current;
            }
        }

        private PageActionInvoker CreateActionInvoker(
            ActionContext actionContext,
            PageActionInvokerCacheEntry cacheEntry,
            IFilterMetadata[] filters)
        {
            var tempData = _tempDataFactory.GetTempData(actionContext.HttpContext);
            var pageContext = new PageContext(
                actionContext,
                new ViewDataDictionary(_modelMetadataProvider, actionContext.ModelState),
                tempData,
                _htmlHelperOptions);

            pageContext.ActionDescriptor = cacheEntry.ActionDescriptor;

            return new PageActionInvoker(
                _selector,
                _diagnosticSource,
                _logger,
                pageContext,
                filters,
                new CopyOnWriteList<IValueProviderFactory>(_valueProviderFactories),
                cacheEntry);
        }

        private PageActionInvokerCacheEntry CreateCacheEntry(
            ActionInvokerProviderContext context,
            FilterItem[] cachedFilters)
        {
            var actionDescriptor = (PageActionDescriptor)context.ActionContext.ActionDescriptor;
            var compiledActionDescriptor = _loader.Load(actionDescriptor);

            var pageFactory = _pageFactoryProvider.CreatePageFactory(compiledActionDescriptor);
            var pageDisposer = _pageFactoryProvider.CreatePageDisposer(compiledActionDescriptor);
            var propertyBinder = PagePropertyBinderFactory.CreateBinder(
                _parameterBinder,
                _modelMetadataProvider,
                compiledActionDescriptor);

            Func<PageContext, object> modelFactory = null;
            Action<PageContext, object> modelReleaser = null;
            if (compiledActionDescriptor.ModelTypeInfo == null)
            {
                PopulateHandlerMethodDescriptors(compiledActionDescriptor.PageTypeInfo, compiledActionDescriptor);
            }
            else
            {
                PopulateHandlerMethodDescriptors(compiledActionDescriptor.ModelTypeInfo, compiledActionDescriptor);

                modelFactory = _modelFactoryProvider.CreateModelFactory(compiledActionDescriptor);
                modelReleaser = _modelFactoryProvider.CreateModelDisposer(compiledActionDescriptor);
            }

            var pageStartFactories = GetPageStartFactories(compiledActionDescriptor);

            return new PageActionInvokerCacheEntry(
                compiledActionDescriptor,
                pageFactory,
                pageDisposer,
                modelFactory,
                modelReleaser,
                propertyBinder,
                pageStartFactories,
                cachedFilters);
        }

        // Internal for testing.
        internal List<Func<IRazorPage>> GetPageStartFactories(CompiledPageActionDescriptor descriptor)
        {
            var pageStartFactories = new List<Func<IRazorPage>>();
            var pageStartItems = _razorProject.FindHierarchicalItems(
                _razorPagesOptions.RootDirectory,
                descriptor.RelativePath,
                PageStartFileName);
            foreach (var item in pageStartItems)
            {
                if (item.Exists)
                {
                    var factoryResult = _razorPageFactoryProvider.CreateFactory(item.Path);
                    if (factoryResult.Success)
                    {
                        pageStartFactories.Insert(0, factoryResult.RazorPageFactory);
                    }
                }
            }

            return pageStartFactories;
        }

        // Internal for testing.
        internal static void PopulateHandlerMethodDescriptors(TypeInfo type, CompiledPageActionDescriptor actionDescriptor)
        {
            var methods = type.GetMethods();
            for (var i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                if (!IsValidHandler(method))
                {
                    continue;
                }

                string httpMethod;
                int formActionStart;

                if (method.Name.StartsWith("OnGet", StringComparison.Ordinal))
                {
                    httpMethod = "GET";
                    formActionStart = "OnGet".Length;
                }
                else if (method.Name.StartsWith("OnPost", StringComparison.Ordinal))
                {
                    httpMethod = "POST";
                    formActionStart = "OnPost".Length;
                }
                else
                {
                    continue;
                }

                var formActionLength = method.Name.Length - formActionStart;
                if (method.Name.EndsWith("Async", StringComparison.OrdinalIgnoreCase))
                {
                    formActionLength -= "Async".Length;
                }

                var formAction = new StringSegment(method.Name, formActionStart, formActionLength);

                var handlerMethodDescriptor = new HandlerMethodDescriptor
                {
                    Method = method,
                    Executor = ExecutorFactory.CreateExecutor(actionDescriptor, method),
                    FormAction = formAction,
                    HttpMethod = httpMethod,
                };

                actionDescriptor.HandlerMethods.Add(handlerMethodDescriptor);
            }
        }

        private static bool IsValidHandler(MethodInfo methodInfo)
        {
            // The SpecialName bit is set to flag members that are treated in a special way by some compilers
            // (such as property accessors and operator overloading methods).
            if (methodInfo.IsSpecialName)
            {
                return false;
            }

            // Overriden methods from Object class, e.g. Equals(Object), GetHashCode(), etc., are not valid.
            if (methodInfo.GetBaseDefinition().DeclaringType == typeof(object))
            {
                return false;
            }

            if (methodInfo.IsStatic)
            {
                return false;
            }

            if (methodInfo.IsAbstract)
            {
                return false;
            }

            if (methodInfo.IsConstructor)
            {
                return false;
            }

            if (methodInfo.IsGenericMethod)
            {
                return false;
            }

            return methodInfo.IsPublic;
        }

        internal class InnerCache
        {
            public InnerCache(int version)
            {
                Version = version;
            }

            public ConcurrentDictionary<ActionDescriptor, PageActionInvokerCacheEntry> Entries { get; } =
                new ConcurrentDictionary<ActionDescriptor, PageActionInvokerCacheEntry>();

            public int Version { get; }
        }
    }
}
