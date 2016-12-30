// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageInvokerProviderTest
    {
        [Fact]
        public void OnProvidersExecuting_PopulatesCacheEntry()
        {
            // Arrange
            var descriptor = new PageActionDescriptor
            {
                RelativePath = "Path1",
                FilterDescriptors = new FilterDescriptor[0],
            };
            Func<PageContext, object> factory = _ => null;
            Action<PageContext, object> releaser = (_, __) => { };
            var loader = new Mock<IPageLoader>();
            loader.Setup(l => l.Load(It.IsAny<PageActionDescriptor>()))
                .Returns(typeof(object));
            var descriptorCollection = new ActionDescriptorCollection(new[] { descriptor }, version: 1);
            var actionDescriptorProvider = new Mock<IActionDescriptorCollectionProvider>();
            actionDescriptorProvider.Setup(p => p.ActionDescriptors).Returns(descriptorCollection);
            var factoryProvider = new Mock<IPageFactoryProvider>();
            factoryProvider.Setup(f => f.CreatePageFactory(It.IsAny<CompiledPageActionDescriptor>()))
                .Returns(factory);
            factoryProvider.Setup(f => f.CreatePageDisposer(It.IsAny<CompiledPageActionDescriptor>()))
                .Returns(releaser);

            var invokerProvider = new PageActionInvokerProvider(
                loader.Object,
                factoryProvider.Object,
                actionDescriptorProvider.Object,
                new IFilterProvider[0]);
            var context = new ActionInvokerProviderContext(new ActionContext
            {
                ActionDescriptor = descriptor,
            });

            // Act
            invokerProvider.OnProvidersExecuting(context);

            // Assert
            Assert.NotNull(context.Result);
            var actionInvoker = Assert.IsType<PageActionInvoker>(context.Result);
            var entry = actionInvoker.CacheEntry;
            var compiledPageActionDescriptor = Assert.IsType<CompiledPageActionDescriptor>(entry.ActionDescriptor);
            Assert.Equal(descriptor.RelativePath, compiledPageActionDescriptor.RelativePath);
            Assert.Same(factory, entry.PageFactory);
            Assert.Same(releaser, entry.ReleasePage);
        }

        [Fact]
        public void OnProvidersExecuting_CachesEntries()
        {
            // Arrange
            var descriptor = new PageActionDescriptor
            {
                RelativePath = "Path1",
                FilterDescriptors = new FilterDescriptor[0],
            };
            var loader = new Mock<IPageLoader>();
            loader.Setup(l => l.Load(It.IsAny<PageActionDescriptor>()))
                .Returns(typeof(object));
            var descriptorCollection = new ActionDescriptorCollection(new[] { descriptor }, version: 1);
            var actionDescriptorProvider = new Mock<IActionDescriptorCollectionProvider>();
            actionDescriptorProvider.Setup(p => p.ActionDescriptors).Returns(descriptorCollection);

            var invokerProvider = new PageActionInvokerProvider(
                loader.Object,
                Mock.Of<IPageFactoryProvider>(),
                actionDescriptorProvider.Object,
                new IFilterProvider[0]);
            var context = new ActionInvokerProviderContext(new ActionContext
            {
                ActionDescriptor = descriptor,
            });

            // Act - 1
            invokerProvider.OnProvidersExecuting(context);

            // Assert - 1
            Assert.NotNull(context.Result);
            var actionInvoker = Assert.IsType<PageActionInvoker>(context.Result);
            var entry1 = actionInvoker.CacheEntry;

            // Act - 2
            invokerProvider.OnProvidersExecuting(context);

            // Assert - 2
            Assert.NotNull(context.Result);
            actionInvoker = Assert.IsType<PageActionInvoker>(context.Result);
            var entry2 = actionInvoker.CacheEntry;
            Assert.Same(entry1, entry2);
        }

        [Fact]
        public void OnProvidersExecuting_UpdatesEntriesWhenActionDescriptorProviderCollectionIsUpdated()
        {
            // Arrange
            var descriptor = new PageActionDescriptor
            {
                RelativePath = "Path1",
                FilterDescriptors = new FilterDescriptor[0],
            };
            var descriptorCollection1 = new ActionDescriptorCollection(new[] { descriptor }, version: 1);
            var descriptorCollection2 = new ActionDescriptorCollection(new[] { descriptor }, version: 2);
            var actionDescriptorProvider = new Mock<IActionDescriptorCollectionProvider>();
            actionDescriptorProvider.SetupSequence(p => p.ActionDescriptors)
                .Returns(descriptorCollection1)
                .Returns(descriptorCollection2);

            var loader = new Mock<IPageLoader>();
            loader.Setup(l => l.Load(It.IsAny<PageActionDescriptor>()))
                .Returns(typeof(object));
            var invokerProvider = new PageActionInvokerProvider(
                loader.Object,
                Mock.Of<IPageFactoryProvider>(),
                actionDescriptorProvider.Object,
                new IFilterProvider[0]);
            var context = new ActionInvokerProviderContext(new ActionContext
            {
                ActionDescriptor = descriptor,
            });

            // Act - 1
            invokerProvider.OnProvidersExecuting(context);

            // Assert - 1
            Assert.NotNull(context.Result);
            var actionInvoker = Assert.IsType<PageActionInvoker>(context.Result);
            var entry1 = actionInvoker.CacheEntry;

            // Act - 2
            invokerProvider.OnProvidersExecuting(context);

            // Assert
            Assert.NotNull(context.Result);
            actionInvoker = Assert.IsType<PageActionInvoker>(context.Result);
            var entry2 = actionInvoker.CacheEntry;
            Assert.NotSame(entry1, entry2);
        }
    }
}
