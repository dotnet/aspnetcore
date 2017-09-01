// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ControllerActionInvokerCache
    {
        private readonly IActionDescriptorCollectionProvider _collectionProvider;
        private readonly ParameterBinder _parameterBinder;
        private readonly IModelBinderFactory _modelBinderFactory;
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly IFilterProvider[] _filterProviders;
        private readonly IControllerFactoryProvider _controllerFactoryProvider;

        private volatile InnerCache _currentCache;

        public ControllerActionInvokerCache(
            IActionDescriptorCollectionProvider collectionProvider,
            ParameterBinder parameterBinder,
            IModelBinderFactory modelBinderFactory,
            IModelMetadataProvider modelMetadataProvider,
            IEnumerable<IFilterProvider> filterProviders,
            IControllerFactoryProvider factoryProvider)
        {
            _collectionProvider = collectionProvider;
            _parameterBinder = parameterBinder;
            _modelBinderFactory = modelBinderFactory;
            _modelMetadataProvider = modelMetadataProvider;
            _filterProviders = filterProviders.OrderBy(item => item.Order).ToArray();
            _controllerFactoryProvider = factoryProvider;
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

        public (ControllerActionInvokerCacheEntry cacheEntry, IFilterMetadata[] filters) GetCachedResult(ControllerContext controllerContext)
        {
            var cache = CurrentCache;
            var actionDescriptor = controllerContext.ActionDescriptor;

            IFilterMetadata[] filters;
            if (!cache.Entries.TryGetValue(actionDescriptor, out var cacheEntry))
            {
                var filterFactoryResult = FilterFactory.GetAllFilters(_filterProviders, controllerContext);
                filters = filterFactoryResult.Filters;

                var parameterDefaultValues = ParameterDefaultValues
                    .GetParameterDefaultValues(actionDescriptor.MethodInfo);

                var objectMethodExecutor = ObjectMethodExecutor.Create(
                    actionDescriptor.MethodInfo,
                    actionDescriptor.ControllerTypeInfo,
                    parameterDefaultValues);

                var controllerFactory = _controllerFactoryProvider.CreateControllerFactory(actionDescriptor);
                var controllerReleaser = _controllerFactoryProvider.CreateControllerReleaser(actionDescriptor);
                var propertyBinderFactory = ControllerBinderDelegateProvider.CreateBinderDelegate(
                    _parameterBinder,
                    _modelBinderFactory,
                    _modelMetadataProvider,
                    actionDescriptor);

                var actionMethodExecutor = ActionMethodExecutor.GetExecutor(objectMethodExecutor);

                cacheEntry = new ControllerActionInvokerCacheEntry(
                    filterFactoryResult.CacheableFilters, 
                    controllerFactory, 
                    controllerReleaser,
                    propertyBinderFactory,
                    objectMethodExecutor,
                    actionMethodExecutor);
                cacheEntry = cache.Entries.GetOrAdd(actionDescriptor, cacheEntry);
            }
            else
            {
                // Filter instances from statically defined filter descriptors + from filter providers
                filters = FilterFactory.CreateUncachedFilters(_filterProviders, controllerContext, cacheEntry.CachedFilters);
            }

            return (cacheEntry, filters);
        }

        private class InnerCache
        {
            public InnerCache(int version)
            {
                Version = version;
            }

            public ConcurrentDictionary<ActionDescriptor, ControllerActionInvokerCacheEntry> Entries { get; } =
                new ConcurrentDictionary<ActionDescriptor, ControllerActionInvokerCacheEntry>();

            public int Version { get; }
        }
    }
}
