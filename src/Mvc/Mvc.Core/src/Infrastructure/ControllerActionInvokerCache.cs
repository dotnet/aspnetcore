// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Linq;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

internal sealed class ControllerActionInvokerCache
{
    private readonly ParameterBinder _parameterBinder;
    private readonly IModelBinderFactory _modelBinderFactory;
    private readonly IModelMetadataProvider _modelMetadataProvider;
    private readonly IFilterProvider[] _filterProviders;
    private readonly IControllerFactoryProvider _controllerFactoryProvider;
    private readonly MvcOptions _mvcOptions;

    public ControllerActionInvokerCache(
        ParameterBinder parameterBinder,
        IModelBinderFactory modelBinderFactory,
        IModelMetadataProvider modelMetadataProvider,
        IEnumerable<IFilterProvider> filterProviders,
        IControllerFactoryProvider factoryProvider,
        IOptions<MvcOptions> mvcOptions)
    {
        _parameterBinder = parameterBinder;
        _modelBinderFactory = modelBinderFactory;
        _modelMetadataProvider = modelMetadataProvider;
        _filterProviders = filterProviders.OrderBy(item => item.Order).ToArray();
        _controllerFactoryProvider = factoryProvider;
        _mvcOptions = mvcOptions.Value;
    }

    public (ControllerActionInvokerCacheEntry cacheEntry, IFilterMetadata[] filters) GetCachedResult(ControllerContext controllerContext)
    {
        var actionDescriptor = controllerContext.ActionDescriptor;

        IFilterMetadata[] filters;

        var cacheEntry = actionDescriptor.CacheEntry;

        // We don't care about thread safety here
        if (cacheEntry is null)
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
            var controllerReleaser = _controllerFactoryProvider.CreateAsyncControllerReleaser(actionDescriptor);
            var propertyBinderFactory = ControllerBinderDelegateProvider.CreateBinderDelegate(
                _parameterBinder,
                _modelBinderFactory,
                _modelMetadataProvider,
                actionDescriptor,
                _mvcOptions);

            var actionMethodExecutor = ActionMethodExecutor.GetExecutor(objectMethodExecutor);
            var filterExecutor = actionDescriptor.FilterDelegate is not null
                ? ActionMethodExecutor.GetFilterExecutor(actionDescriptor)
                : null;

            cacheEntry = new ControllerActionInvokerCacheEntry(
                filterFactoryResult.CacheableFilters,
                controllerFactory,
                controllerReleaser,
                propertyBinderFactory,
                objectMethodExecutor,
                filterExecutor ?? actionMethodExecutor,
                actionMethodExecutor);

            actionDescriptor.CacheEntry = cacheEntry;
        }
        else
        {
            // Filter instances from statically defined filter descriptors + from filter providers
            filters = FilterFactory.CreateUncachedFilters(_filterProviders, controllerContext, cacheEntry.CachedFilters);
        }

        return (cacheEntry, filters);
    }
}
