// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Internal
{
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
                controllerContext,
                new[] { new DefaultFilterProvider() });

            // Act
            var cacheEntry1 = controllerActionInvokerCache.GetState(controllerContext);
            var cacheEntry2 = controllerActionInvokerCache.GetState(controllerContext);

            // Assert
            Assert.Equal(cacheEntry1.Filters, cacheEntry2.Filters);
        }

        [Fact]
        public void GetControllerActionMethodExecutor_CachesActionMethodExecutor()
        {
            // Arrange
            var filter = new TestFilter();
            var controllerContext = CreateControllerContext(new[]
                {
                    new FilterDescriptor(filter, FilterScope.Action)
                });
            var controllerActionInvokerCache = CreateControllerActionInvokerCache(
                controllerContext,
                new[] { new DefaultFilterProvider() });

            // Act
            var cacheEntry1 = controllerActionInvokerCache.GetState(controllerContext);
            var cacheEntry2 = controllerActionInvokerCache.GetState(controllerContext);

            // Assert
            Assert.Same(cacheEntry1.ActionMethodExecutor, cacheEntry2.ActionMethodExecutor);
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
            ControllerContext controllerContext,
            IFilterProvider[] filterProviders)
        {
            var descriptorProvider = new CustomActionDescriptorCollectionProvider(
                new[] { controllerContext.ActionDescriptor });
            return new ControllerActionInvokerCache(descriptorProvider, filterProviders);
        }

        private static ControllerContext CreateControllerContext(FilterDescriptor[] filterDescriptors)
        {
            var actionDescriptor = new ControllerActionDescriptor()
            {
                FilterDescriptors = filterDescriptors,
                MethodInfo = typeof(TestController).GetMethod(nameof(TestController.Index)),
                ControllerTypeInfo = typeof(TestController).GetTypeInfo()
            };

            var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), actionDescriptor);
            return new ControllerContext(actionContext);
        }
    }
}
