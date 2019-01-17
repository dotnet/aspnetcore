// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class CreatePageCacheEntryActionInvokerTest
    {
        private readonly ConcurrentDictionary<ActionDescriptor, PageActionInvokerCacheEntry> Cache =
            new ConcurrentDictionary<ActionDescriptor, PageActionInvokerCacheEntry>();

        private readonly Func<PageContext, ViewContext, object> PageFactory = (a, b) => null;
        private readonly Action<PageContext, ViewContext, object> PageReleaser = (a, b, c) => { };
        private readonly Func<PageContext, object> ModelFactory = _ => null;
        private readonly Action<PageContext, object> ModelDisposer = (_, __) => { };

        private readonly IActionInvoker TestInvoker = Mock.Of<IActionInvoker>(i => i.InvokeAsync() == Task.CompletedTask);

        [Fact]
        public async Task InvokeAsync_InvokesActionInvokerReturnedByInvokerFactory()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(object).GetTypeInfo(),
                ModelTypeInfo = typeof(object).GetTypeInfo(),
                DeclaredModelTypeInfo = typeof(object).GetTypeInfo(),
                RelativePath = "/Path1",
                FilterDescriptors = Array.Empty<FilterDescriptor>(),
            };

            var invoker = GetInvoker(actionDescriptor);

            // Act
            await invoker.InvokeAsync();

            // Assert
            Mock.Get(TestInvoker).Verify(v => v.InvokeAsync(), Times.Once());
        }

        [Fact]
        public async Task InvokeAsync_PopulatesCacheEntry()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(object).GetTypeInfo(),
                ModelTypeInfo = typeof(object).GetTypeInfo(),
                DeclaredModelTypeInfo = typeof(object).GetTypeInfo(),
                RelativePath = "/Path1",
                FilterDescriptors = Array.Empty<FilterDescriptor>(),
            };

            var invoker = GetInvoker(actionDescriptor);

            // Act
            await invoker.InvokeAsync();

            // Assert
            var kvp = Assert.Single(Cache);
            Assert.Same(actionDescriptor, kvp.Key);

            var entry = kvp.Value;
            Assert.Same(PageFactory, entry.PageFactory);
            Assert.Same(PageReleaser, entry.ReleasePage);
            Assert.Null(entry.ModelFactory);
            Assert.Null(entry.ReleaseModel);
            Assert.NotNull(entry.ViewDataFactory);
        }

        [Fact]
        public async Task InvokeAsync_WithModel_PopulatesCacheEntry()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(object).GetTypeInfo(),
                ModelTypeInfo = typeof(string).GetTypeInfo(),
                DeclaredModelTypeInfo = typeof(string).GetTypeInfo(),
                RelativePath = "/Path1",
                FilterDescriptors = Array.Empty<FilterDescriptor>(),
            };

            var invoker = GetInvoker(actionDescriptor);

            // Act
            await invoker.InvokeAsync();

            // Assert
            var kvp = Assert.Single(Cache);
            Assert.Same(actionDescriptor, kvp.Key);

            // Assert
            var entry = kvp.Value;
            Assert.Same(PageFactory, entry.PageFactory);
            Assert.Same(PageReleaser, entry.ReleasePage);
            Assert.Same(ModelFactory, entry.ModelFactory);
            Assert.Same(ModelDisposer, entry.ReleaseModel);
            Assert.NotNull(entry.ViewDataFactory);

            var viewData = entry.ViewDataFactory(new EmptyModelMetadataProvider(), new ModelStateDictionary());
            Assert.IsType<ViewDataDictionary<string>>(viewData);
        }

        [Fact]
        public async Task InvokeAsync_CachesViewStartFactories()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(object).GetTypeInfo(),
                ModelTypeInfo = typeof(object).GetTypeInfo(),
                DeclaredModelTypeInfo = typeof(object).GetTypeInfo(),
                RelativePath = "/Home/Path1/File.cshtml",
                ViewEnginePath = "Home/Path1/File.cshtml",
                FilterDescriptors = Array.Empty<FilterDescriptor>(),
            };

            var razorPageFactoryProvider = new Mock<IRazorPageFactoryProvider>();

            Func<IRazorPage> factory1 = () => null;
            Func<IRazorPage> factory2 = () => null;

            razorPageFactoryProvider
                .Setup(f => f.CreateFactory("/Home/Path1/_ViewStart.cshtml"))
                .Returns(new RazorPageFactoryResult(new CompiledViewDescriptor(), factory1));
            razorPageFactoryProvider
                .Setup(f => f.CreateFactory("/_ViewStart.cshtml"))
                .Returns(new RazorPageFactoryResult(new CompiledViewDescriptor(), factory2));

            var invoker = GetInvoker(actionDescriptor, razorPageFactoryProvider.Object);

            // Act
            await invoker.InvokeAsync();

            // Assert
            var kvp = Assert.Single(Cache);
            Assert.Same(actionDescriptor, kvp.Key);

            // Assert
            var entry = kvp.Value;
            Assert.Equal(new[] { factory2, factory1 }, entry.ViewStartFactories);
        }

        private CreatePageCacheEntryActionInvoker GetInvoker(
            CompiledPageActionDescriptor actionDescriptor,
            IRazorPageFactoryProvider razorPageFactoryProvider = null)
        {
            var tempDataFactory = new Mock<ITempDataDictionaryFactory>();
            tempDataFactory
                .Setup(t => t.GetTempData(It.IsAny<HttpContext>()))
                .Returns((HttpContext context) => new TempDataDictionary(context, Mock.Of<ITempDataProvider>()));

            var modelMetadataProvider = new EmptyModelMetadataProvider();

            var parameterBinder = new ParameterBinder(
               modelMetadataProvider,
               TestModelBinderFactory.CreateDefault(),
               Mock.Of<IObjectModelValidator>(),
               Options.Create(new MvcOptions()),
               NullLoggerFactory.Instance);

            var pageFactoryProvider = new Mock<IPageFactoryProvider>();
            pageFactoryProvider
                .Setup(f => f.CreatePageFactory(It.IsAny<CompiledPageActionDescriptor>()))
                .Returns(PageFactory);
            pageFactoryProvider
                .Setup(f => f.CreatePageDisposer(It.IsAny<CompiledPageActionDescriptor>()))
                .Returns(PageReleaser);

            var modelFactoryProvider = new Mock<IPageModelFactoryProvider>();
            modelFactoryProvider
                .Setup(f => f.CreateModelFactory(It.IsAny<CompiledPageActionDescriptor>()))
                .Returns(ModelFactory);
            modelFactoryProvider
                .Setup(f => f.CreateModelDisposer(It.IsAny<CompiledPageActionDescriptor>()))
                .Returns(ModelDisposer);

            razorPageFactoryProvider = razorPageFactoryProvider ?? Mock.Of<IRazorPageFactoryProvider>();

            var loader = Mock.Of<PageLoaderBase>(p => 
                p.LoadAsync(actionDescriptor) == new ValueTask<CompiledPageActionDescriptor>(actionDescriptor));

            var actionContext = new ActionContext();

            var actionInvokerFactory = Mock.Of<IPageActionInvokerFactory>(f =>
                f.CreateInvoker(actionContext, It.IsAny<PageActionInvokerCacheEntry>(), It.IsAny<IFilterMetadata[]>()) == TestInvoker);

            var invoker = new CreatePageCacheEntryActionInvoker(
                loader,
                pageFactoryProvider.Object,
                modelFactoryProvider.Object,
                razorPageFactoryProvider,
                Array.Empty<IFilterProvider>(),
                parameterBinder,
                Mock.Of<IModelBinderFactory>(),
                Mock.Of<IModelMetadataProvider>(),
                actionInvokerFactory,
                Cache,
                actionContext,
                actionDescriptor);

            return invoker;
        }
    }
}
