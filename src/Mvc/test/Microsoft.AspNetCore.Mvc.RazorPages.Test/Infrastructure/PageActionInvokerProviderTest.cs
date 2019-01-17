// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class PageInvokerProviderTest
    {
        [Fact]
        public void OnProvidersExecuting_ReturnsCreatePageCacheEntryActionInvoker_OnCacheMiss()
        {
            // Arrange
            var descriptor = new PageActionDescriptor
            {
                RelativePath = "/Path1",
                FilterDescriptors = new FilterDescriptor[0],
            };

            var descriptorCollection = new ActionDescriptorCollection(new[] { descriptor }, version: 1);

            var actionDescriptorProvider = Mock.Of<IActionDescriptorCollectionProvider>(m => m.ActionDescriptors == descriptorCollection);
            var invokerProvider = CreateInvokerProvider(actionDescriptorProvider);

            var context = new ActionInvokerProviderContext(new ActionContext
            {
                ActionDescriptor = descriptor,
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData(),
            });

            // Act 
            invokerProvider.OnProvidersExecuting(context);

            // Assert
            Assert.NotNull(context.Result);
            Assert.IsType<CreatePageCacheEntryActionInvoker>(context.Result);
        }

        [Fact]
        public void OnProvidersExecuting_UsesActionInvokerFactory_IfCacheEntryExists()
        {
            // Arrange
            var expected = Mock.Of<IActionInvoker>();

            var descriptor = new PageActionDescriptor
            {
                RelativePath = "/Path1",
                FilterDescriptors = new FilterDescriptor[0],
            };

            var actionContext = new ActionContext
            {
                ActionDescriptor = descriptor,
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData(),
            };

            var cacheEntry = new PageActionInvokerCacheEntry(
                new CompiledPageActionDescriptor(),
                (_, __) => null,
                (_, __) => null,
                (_, __, ___) => { },
                _ => null,
                (_, __) => { },
                (_, __) => Task.CompletedTask,
                Array.Empty<PageHandlerExecutorDelegate>(),
                Array.Empty<PageHandlerBinderDelegate>(),
                Array.Empty<Func<IRazorPage>>(),
                Array.Empty<FilterItem>());

            var invokerFactory = Mock.Of<IPageActionInvokerFactory>(m => m.CreateInvoker(actionContext, cacheEntry, It.IsAny<IFilterMetadata[]>()) == expected);

            var descriptorCollection = new ActionDescriptorCollection(new[] { descriptor }, version: 1);
            var actionDescriptorProvider = Mock.Of<IActionDescriptorCollectionProvider>(m => m.ActionDescriptors == descriptorCollection);
            
            var context = new ActionInvokerProviderContext(actionContext);

            var invokerProvider = CreateInvokerProvider(actionDescriptorProvider, invokerFactory);
            invokerProvider.CurrentCache.Entries[descriptor] = cacheEntry;

            // Act 
            invokerProvider.OnProvidersExecuting(context);

            // Assert
            Assert.NotNull(context.Result);
            Assert.Same(expected, context.Result);
        }

        [Fact]
        public void OnProvidersExecuting_PreservesCache_WhenActionDescriptorCollectionDoesNotChange()
        {
            // Arrange
            var descriptor = new PageActionDescriptor
            {
                RelativePath = "/Path1",
                FilterDescriptors = new FilterDescriptor[0],
            };

            var descriptorCollection = new ActionDescriptorCollection(new[] { descriptor }, version: 1);

            var actionDescriptorProvider = Mock.Of<IActionDescriptorCollectionProvider>(p => p.ActionDescriptors == descriptorCollection);
            var invokerProvider = CreateInvokerProvider(actionDescriptorProvider);

            // Act
            var cache1 = invokerProvider.CurrentCache;
            var cache2 = invokerProvider.CurrentCache;

            // Assert
            Assert.Same(cache1, cache2);
        }

        [Fact]
        public void OnProvidersExecuting_InvalidatesCache_WhenActionDescriptorCollectionChanges()
        {
            // Arrange
            var descriptor = new PageActionDescriptor
            {
                RelativePath = "/Path1",
                FilterDescriptors = new FilterDescriptor[0],
            };

            var descriptorCollection1 = new ActionDescriptorCollection(new[] { descriptor }, version: 1);
            var descriptorCollection2 = new ActionDescriptorCollection(new[] { descriptor }, version: 2);

            var actionDescriptorProvider = new Mock<IActionDescriptorCollectionProvider>();
            actionDescriptorProvider
                .SetupSequence(p => p.ActionDescriptors)
                .Returns(descriptorCollection1)
                .Returns(descriptorCollection2);

            var invokerProvider = CreateInvokerProvider(actionDescriptorProvider.Object);

            // Act
            var cache1 = invokerProvider.CurrentCache;
            var cache2 = invokerProvider.CurrentCache;

            // Assert
            Assert.NotSame(cache1, cache2);
        }

        private static CompiledPageActionDescriptor CreateCompiledPageActionDescriptor(
            PageActionDescriptor descriptor,
            Type pageType = null,
            Type modelType = null)
        {
            pageType = pageType ?? typeof(object);
            var pageTypeInfo = pageType.GetTypeInfo();

            var modelTypeInfo = modelType?.GetTypeInfo();
            TypeInfo declaredModelTypeInfo = null;
            if (pageType != null)
            {
                declaredModelTypeInfo = pageTypeInfo.GetProperty("Model")?.PropertyType.GetTypeInfo();
                if (modelTypeInfo == null)
                {
                    modelTypeInfo = declaredModelTypeInfo;
                }
            }

            return new CompiledPageActionDescriptor(descriptor)
            {
                HandlerTypeInfo = modelTypeInfo ?? pageTypeInfo,
                DeclaredModelTypeInfo = declaredModelTypeInfo ?? pageTypeInfo,
                ModelTypeInfo = modelTypeInfo ?? pageTypeInfo,
                PageTypeInfo = pageTypeInfo,
                FilterDescriptors = Array.Empty<FilterDescriptor>(),
            };
        }

        private static PageActionInvokerProvider CreateInvokerProvider(
            IActionDescriptorCollectionProvider actionDescriptorProvider,
            IPageActionInvokerFactory pageActionInvokerFactory = null)
        {
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var modelBinderFactory = TestModelBinderFactory.CreateDefault();
            var mvcOptions = new MvcOptions();
            pageActionInvokerFactory = pageActionInvokerFactory ?? Mock.Of<IPageActionInvokerFactory>();

            var parameterBinder = new ParameterBinder(
                modelMetadataProvider,
                TestModelBinderFactory.CreateDefault(),
                Mock.Of<IObjectModelValidator>(),
                Options.Create(mvcOptions),
                NullLoggerFactory.Instance);

            return new PageActionInvokerProvider(
                Mock.Of<PageLoaderBase>(),
                Mock.Of<IPageFactoryProvider>(),
                Mock.Of<IPageModelFactoryProvider>(),
                Mock.Of<IRazorPageFactoryProvider>(),
                actionDescriptorProvider,
                new IFilterProvider[0],
                parameterBinder,
                modelMetadataProvider,
                modelBinderFactory,
                pageActionInvokerFactory);
        }

        private IActionDescriptorCollectionProvider CreateActionDescriptorCollection(PageActionDescriptor descriptor)
        {
            var descriptorCollection = new ActionDescriptorCollection(new[] { descriptor }, version: 1);
            var actionDescriptorProvider = new Mock<IActionDescriptorCollectionProvider>();
            actionDescriptorProvider
                .Setup(p => p.ActionDescriptors)
                .Returns(descriptorCollection);

            return actionDescriptorProvider.Object;
        }

        private class PageWithModel
        {
            public TestPageModel Model { get; set; }
        }

        private class TestPageModel
        {
            public void OnGet()
            {
            }
        }

        private class DerivedTestPageModel : TestPageModel
        {
        }
    }
}
