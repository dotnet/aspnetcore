// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

public class ControllerActionInvokerCacheTest
{
    [Fact]
    public void GetControllerActionMethodExecutor_CachesFilters()
    {
        // Arrange
        var filter = new TestFilter();
        var controllerContext = CreateControllerContext(new[]
            {
                    new FilterDescriptor(filter, FilterScope.Action)
                });
        var controllerActionInvokerCache = CreateControllerActionInvokerCache(
            new[] { new DefaultFilterProvider() });

        // Act
        var (cacheEntry, filters) = controllerActionInvokerCache.GetCachedResult(controllerContext);
        var (cacheEntry2, filters2) = controllerActionInvokerCache.GetCachedResult(controllerContext);

        // Assert
        Assert.Equal(filters, filters2);
    }

    [Fact]
    public void GetControllerActionMethodExecutor_CachesEntry()
    {
        // Arrange
        var filter = new TestFilter();
        var controllerContext = CreateControllerContext(new[]
            {
                    new FilterDescriptor(filter, FilterScope.Action)
                });
        var controllerActionInvokerCache = CreateControllerActionInvokerCache(
            new[] { new DefaultFilterProvider() });

        // Act
        var (cacheEntry, filters) = controllerActionInvokerCache.GetCachedResult(controllerContext);
        var (cacheEntry2, filters2) = controllerActionInvokerCache.GetCachedResult(controllerContext);

        // Assert
        Assert.Same(cacheEntry, cacheEntry2);
    }

    private class TestFilter : IFilterMetadata
    {
        public TestFilter()
        {
        }

        public TestFilter(string data)
        {
            Data = data;
        }

        public string Data { get; }
    }

    private class TestController
    {
        public void Index()
        {
        }
    }

    private class CustomActionDescriptorCollectionProvider : IActionDescriptorCollectionProvider
    {
        public CustomActionDescriptorCollectionProvider(ControllerActionDescriptor[] actionDescriptors)
        {
            ActionDescriptors = new ActionDescriptorCollection(actionDescriptors, version: 1);
        }

        public ActionDescriptorCollection ActionDescriptors { get; }
    }

    private static ControllerActionInvokerCache CreateControllerActionInvokerCache(
        IFilterProvider[] filterProviders)
    {
        var modelMetadataProvider = new EmptyModelMetadataProvider();
        var modelBinderFactory = TestModelBinderFactory.CreateDefault();
        var mvcOptions = Options.Create(new MvcOptions());

        return new ControllerActionInvokerCache(
            new ParameterBinder(
                modelMetadataProvider,
                modelBinderFactory,
                Mock.Of<IObjectModelValidator>(),
                mvcOptions,
                NullLoggerFactory.Instance),
            modelBinderFactory,
            modelMetadataProvider,
            filterProviders,
            Mock.Of<IControllerFactoryProvider>(),
            mvcOptions);
    }

    private static ControllerContext CreateControllerContext(FilterDescriptor[] filterDescriptors)
    {
        var actionDescriptor = new ControllerActionDescriptor()
        {
            FilterDescriptors = filterDescriptors,
            MethodInfo = typeof(TestController).GetMethod(nameof(TestController.Index)),
            ControllerTypeInfo = typeof(TestController).GetTypeInfo(),
            Parameters = new List<ParameterDescriptor>(),
            BoundProperties = new List<ParameterDescriptor>(),
        };

        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), actionDescriptor);
        return new ControllerContext(actionContext);
    }
}
