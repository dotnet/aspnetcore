// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageInvokerProviderTest
    {
        [Fact]
        public void OnProvidersExecuting_WithEmptyModel_PopulatesCacheEntry()
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
                .Returns(CreateCompiledPageActionDescriptor(descriptor));
            var descriptorCollection = new ActionDescriptorCollection(new[] { descriptor }, version: 1);
            var actionDescriptorProvider = new Mock<IActionDescriptorCollectionProvider>();
            actionDescriptorProvider.Setup(p => p.ActionDescriptors).Returns(descriptorCollection);
            var pageFactoryProvider = new Mock<IPageFactoryProvider>();
            pageFactoryProvider.Setup(f => f.CreatePageFactory(It.IsAny<CompiledPageActionDescriptor>()))
                .Returns(factory);
            pageFactoryProvider.Setup(f => f.CreatePageDisposer(It.IsAny<CompiledPageActionDescriptor>()))
                .Returns(releaser);

            var invokerProvider = CreateInvokerProvider(
                loader.Object,
                actionDescriptorProvider.Object,
                pageFactoryProvider.Object);
            var context = new ActionInvokerProviderContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), descriptor));

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
            Assert.Null(entry.ModelFactory);
            Assert.Null(entry.ReleaseModel);
        }

        [Fact]
        public void OnProvidersExecuting_SetsHandlers()
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
                .Returns(CreateCompiledPageActionDescriptor(descriptor, typeof(TestSetPageWithModel)));
            var descriptorCollection = new ActionDescriptorCollection(new[] { descriptor }, version: 1);
            var actionDescriptorProvider = new Mock<IActionDescriptorCollectionProvider>();
            actionDescriptorProvider.Setup(p => p.ActionDescriptors).Returns(descriptorCollection);
            var pageFactoryProvider = new Mock<IPageFactoryProvider>();
            pageFactoryProvider.Setup(f => f.CreatePageFactory(It.IsAny<CompiledPageActionDescriptor>()))
                .Returns(factory);
            pageFactoryProvider.Setup(f => f.CreatePageDisposer(It.IsAny<CompiledPageActionDescriptor>()))
                .Returns(releaser);

            var modelFactoryProvider = new Mock<IPageModelFactoryProvider>();

            var invokerProvider = CreateInvokerProvider(
                loader.Object,
                actionDescriptorProvider.Object,
                pageFactoryProvider.Object,
                modelFactoryProvider.Object);
            var context = new ActionInvokerProviderContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), descriptor));

            // Act
            invokerProvider.OnProvidersExecuting(context);

            // Assert
            Assert.NotNull(context.Result);
            var actionInvoker = Assert.IsType<PageActionInvoker>(context.Result);
            var entry = actionInvoker.CacheEntry;

            Assert.Collection(entry.ActionDescriptor.HandlerMethods,
                handlerDescriptor =>
                {
                    Assert.Equal(nameof(TestSetPageModel.OnGet), handlerDescriptor.Method.Name);
                    Assert.NotNull(handlerDescriptor.Executor);
                },
                handlerDescriptor =>
                {
                    Assert.Equal(nameof(TestSetPageModel.OnPost), handlerDescriptor.Method.Name);
                    Assert.NotNull(handlerDescriptor.Executor);
                });
        }

        [Fact]
        public void OnProvidersExecuting_WithModel_PopulatesCacheEntry()
        {
            // Arrange
            var descriptor = new PageActionDescriptor
            {
                RelativePath = "Path1",
                FilterDescriptors = new FilterDescriptor[0],
            };
            Func<PageContext, object> factory = _ => null;
            Action<PageContext, object> releaser = (_, __) => { };
            Func<PageContext, object> modelFactory = _ => null;
            Action<PageContext, object> modelDisposer = (_, __) => { };

            var loader = new Mock<IPageLoader>();
            loader.Setup(l => l.Load(It.IsAny<PageActionDescriptor>()))
                .Returns(CreateCompiledPageActionDescriptor(descriptor, pageType: typeof(PageWithModel)));
            var descriptorCollection = new ActionDescriptorCollection(new[] { descriptor }, version: 1);
            var actionDescriptorProvider = new Mock<IActionDescriptorCollectionProvider>();
            actionDescriptorProvider.Setup(p => p.ActionDescriptors).Returns(descriptorCollection);
            var pageFactoryProvider = new Mock<IPageFactoryProvider>();
            pageFactoryProvider.Setup(f => f.CreatePageFactory(It.IsAny<CompiledPageActionDescriptor>()))
                .Returns(factory);
            pageFactoryProvider.Setup(f => f.CreatePageDisposer(It.IsAny<CompiledPageActionDescriptor>()))
                .Returns(releaser);

            var modelFactoryProvider = new Mock<IPageModelFactoryProvider>();
            modelFactoryProvider.Setup(f => f.CreateModelFactory(It.IsAny<CompiledPageActionDescriptor>()))
                .Returns(modelFactory);
            modelFactoryProvider.Setup(f => f.CreateModelDisposer(It.IsAny<CompiledPageActionDescriptor>()))
                .Returns(modelDisposer);

            var invokerProvider = CreateInvokerProvider(
                loader.Object,
                actionDescriptorProvider.Object,
                pageFactoryProvider.Object,
                modelFactoryProvider.Object);
            var context = new ActionInvokerProviderContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), descriptor));

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
            Assert.Same(modelFactory, entry.ModelFactory);
            Assert.Same(modelDisposer, entry.ReleaseModel);
        }

        [Fact]
        public void OnProvidersExecuting_CachesViewStartFactories()
        {
            // Arrange
            var descriptor = new PageActionDescriptor
            {
                RelativePath = "/Home/Path1/File.cshtml",
                ViewEnginePath = "/Home/Path1/File.cshtml",
                FilterDescriptors = new FilterDescriptor[0],
            };

            var loader = new Mock<IPageLoader>();
            loader.Setup(l => l.Load(It.IsAny<PageActionDescriptor>()))
                .Returns(CreateCompiledPageActionDescriptor(descriptor, pageType: typeof(PageWithModel)));
            var descriptorCollection = new ActionDescriptorCollection(new[] { descriptor }, version: 1);
            var actionDescriptorProvider = new Mock<IActionDescriptorCollectionProvider>();
            actionDescriptorProvider.Setup(p => p.ActionDescriptors).Returns(descriptorCollection);
            var razorPageFactoryProvider = new Mock<IRazorPageFactoryProvider>();
            Func<IRazorPage> factory1 = () => null;
            Func<IRazorPage> factory2 = () => null;
            razorPageFactoryProvider.Setup(f => f.CreateFactory("/Home/Path1/_PageStart.cshtml"))
                .Returns(new RazorPageFactoryResult(factory1, new IChangeToken[0]));
            razorPageFactoryProvider.Setup(f => f.CreateFactory("/_PageStart.cshtml"))
                .Returns(new RazorPageFactoryResult(factory2, new[] { Mock.Of<IChangeToken>() }));
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile("/Home/Path1/_PageStart.cshtml", "content1");
            fileProvider.AddFile("/_PageStart.cshtml", "content2");
            var defaultRazorProject = new DefaultRazorProject(fileProvider);

            var invokerProvider = CreateInvokerProvider(
                loader.Object,
                actionDescriptorProvider.Object,
                razorPageFactoryProvider: razorPageFactoryProvider.Object,
                razorProject: defaultRazorProject);
            var context = new ActionInvokerProviderContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), descriptor));

            // Act
            invokerProvider.OnProvidersExecuting(context);

            // Assert
            Assert.NotNull(context.Result);
            var actionInvoker = Assert.IsType<PageActionInvoker>(context.Result);
            var entry = actionInvoker.CacheEntry;
            Assert.Equal(new[] { factory2, factory1 }, entry.PageStartFactories);
        }

        [Fact]
        public void OnProvidersExecuting_CachesExecutor()
        {
            // Arrange
            var descriptor = new PageActionDescriptor
            {
                RelativePath = "/Home/Path1/File.cshtml",
                ViewEnginePath = "/Home/Path1/File.cshtml",
                FilterDescriptors = new FilterDescriptor[0],
            };

            var loader = new Mock<IPageLoader>();
            loader.Setup(l => l.Load(It.IsAny<PageActionDescriptor>()))
                .Returns(CreateCompiledPageActionDescriptor(descriptor, pageType: typeof(PageWithModel)));
            var descriptorCollection = new ActionDescriptorCollection(new[] { descriptor }, version: 1);
            var actionDescriptorProvider = new Mock<IActionDescriptorCollectionProvider>();
            actionDescriptorProvider.Setup(p => p.ActionDescriptors).Returns(descriptorCollection);
            var razorPageFactoryProvider = new Mock<IRazorPageFactoryProvider>();
            var fileProvider = new TestFileProvider();
            var defaultRazorProject = new DefaultRazorProject(fileProvider);

            var invokerProvider = CreateInvokerProvider(
                loader.Object,
                actionDescriptorProvider.Object,
                razorPageFactoryProvider: razorPageFactoryProvider.Object,
                razorProject: defaultRazorProject);
            var context = new ActionInvokerProviderContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), descriptor));

            // Act
            invokerProvider.OnProvidersExecuting(context);

            // Assert
            Assert.NotNull(context.Result);
            var actionInvoker = Assert.IsType<PageActionInvoker>(context.Result);
            var actionDescriptor = actionInvoker.CacheEntry.ActionDescriptor;
            Assert.Collection(actionDescriptor.HandlerMethods,
                handlerDescriptor =>
                {
                    Assert.Equal(nameof(TestPageModel.OnGet), handlerDescriptor.Method.Name);
                    Assert.NotNull(handlerDescriptor.Executor);
                });
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
                .Returns(CreateCompiledPageActionDescriptor(descriptor));
            var descriptorCollection = new ActionDescriptorCollection(new[] { descriptor }, version: 1);
            var actionDescriptorProvider = new Mock<IActionDescriptorCollectionProvider>();
            actionDescriptorProvider.Setup(p => p.ActionDescriptors).Returns(descriptorCollection);

            var invokerProvider = CreateInvokerProvider(
                loader.Object,
                actionDescriptorProvider.Object);
            var context = new ActionInvokerProviderContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), descriptor));

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
                .Returns(CreateCompiledPageActionDescriptor(descriptor));
            var invokerProvider = CreateInvokerProvider(
                 loader.Object,
                 actionDescriptorProvider.Object);
            var context = new ActionInvokerProviderContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), descriptor));

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

        [Fact]
        public void PopulateHandlerMethodDescriptors_DiscoversHandlersFromBaseType()
        {
            // Arrange
            var descriptor = new PageActionDescriptor()
            {
                RelativePath = "Path1",
                FilterDescriptors = new FilterDescriptor[0],
                ViewEnginePath = "/Views/Deeper/Index.cshtml"
            };

            var actionDescriptor = CreateCompiledPageActionDescriptor(descriptor, typeof(InheritsMethods));

            var type = actionDescriptor.ModelTypeInfo ?? actionDescriptor.PageTypeInfo;

            // Act
            PageActionInvokerProvider.PopulateHandlerMethodDescriptors(type, actionDescriptor);

            // Assert
            Assert.Collection(actionDescriptor.HandlerMethods,
                (handler) =>
                {
                    Assert.Equal("OnGet", handler.Method.Name);
                    Assert.Equal(typeof(InheritsMethods), handler.Method.DeclaringType);
                },
                (handler) =>
                {
                    Assert.Equal("OnPost", handler.Method.Name);
                    Assert.Equal(typeof(TestSetPageModel), handler.Method.DeclaringType);
                });
        }

        [Fact]
        public void PopulateHandlerMethodDescriptors_ProtectedMethodsNotFound()
        {
            // Arrange
            var descriptor = new PageActionDescriptor()
            {
                RelativePath = "Path1",
                FilterDescriptors = new FilterDescriptor[0],
                ViewEnginePath = "/Views/Deeper/Index.cshtml"
            };

            var actionDescriptor = CreateCompiledPageActionDescriptor(descriptor, typeof(ProtectedModel));

            var type = actionDescriptor.ModelTypeInfo ?? actionDescriptor.PageTypeInfo;

            // Act
            PageActionInvokerProvider.PopulateHandlerMethodDescriptors(type, actionDescriptor);

            // Assert
            Assert.Empty(actionDescriptor.HandlerMethods);
        }

        [Fact]
        public void PopulateHandlerMethodDescriptors_IgnoreGenericTypeParameters()
        {
            // Arrange
            var descriptor = new PageActionDescriptor()
            {
                RelativePath = "Path1",
                FilterDescriptors = new FilterDescriptor[0],
                ViewEnginePath = "/Views/Deeper/Index.cshtml"
            };

            var actionDescriptor = CreateCompiledPageActionDescriptor(descriptor, typeof(GenericClassModel));

            var type = actionDescriptor.ModelTypeInfo ?? actionDescriptor.PageTypeInfo;

            // Act
            PageActionInvokerProvider.PopulateHandlerMethodDescriptors(type, actionDescriptor);

            // Assert
            Assert.Empty(actionDescriptor.HandlerMethods);
        }

        [Fact]
        public void PopulateHandlerMethodDescriptors_AllowOnlyOneMethod()
        {
            // Arrange
            var descriptor = new PageActionDescriptor()
            {
                RelativePath = "Path1",
                FilterDescriptors = new FilterDescriptor[0],
                ViewEnginePath = "/Views/Deeper/Index.cshtml"
            };

            var actionDescriptor = CreateCompiledPageActionDescriptor(descriptor, typeof(TestPageModel));

            var type = actionDescriptor.ModelTypeInfo ?? actionDescriptor.PageTypeInfo;

            // Act
            PageActionInvokerProvider.PopulateHandlerMethodDescriptors(type, actionDescriptor);

            // Assert
            var handler = Assert.Single(actionDescriptor.HandlerMethods);
            Assert.Equal("OnGet", handler.Method.Name);
        }

        [Fact]
        public void GetPageStartFactories_FindsFullHeirarchy()
        {
            // Arrange
            var descriptor = new PageActionDescriptor()
            {
                RelativePath = "Path1",
                FilterDescriptors = new FilterDescriptor[0],
                ViewEnginePath = "/Views/Deeper/Index.cshtml"
            };
            var loader = new Mock<IPageLoader>();
            loader.Setup(l => l.Load(It.IsAny<PageActionDescriptor>()))
                .Returns(CreateCompiledPageActionDescriptor(descriptor, typeof(TestPageModel)));
            var descriptorCollection = new ActionDescriptorCollection(new[] { descriptor }, version: 1);
            var actionDescriptorProvider = new Mock<IActionDescriptorCollectionProvider>();
            actionDescriptorProvider.Setup(p => p.ActionDescriptors).Returns(descriptorCollection);

            var fileProvider = new TestFileProvider();
            fileProvider.AddFile("/View/Deeper/Not_PageStart.cshtml", "page content");
            fileProvider.AddFile("/View/Wrong/_PageStart.cshtml", "page content");
            fileProvider.AddFile("/_PageStart.cshtml", "page content ");
            fileProvider.AddFile("/Views/_PageStart.cshtml", "@page starts!");
            fileProvider.AddFile("/Views/Deeper/_PageStart.cshtml", "page content");

            var razorProject = CreateRazorProject(fileProvider);

            var mock = new Mock<IRazorPageFactoryProvider>();
            mock.Setup(p => p.CreateFactory("/Views/Deeper/_PageStart.cshtml"))
                .Returns(new RazorPageFactoryResult(() => null, new List<IChangeToken>()))
                .Verifiable();
            mock.Setup(p => p.CreateFactory("/Views/_PageStart.cshtml"))
                .Returns(new RazorPageFactoryResult(() => null, new List<IChangeToken>()))
                .Verifiable();
            mock.Setup(p => p.CreateFactory("/_PageStart.cshtml"))
                .Returns(new RazorPageFactoryResult(() => null, new List<IChangeToken>()))
                .Verifiable();
            var razorPageFactoryProvider = mock.Object;

            var invokerProvider = CreateInvokerProvider(
                loader.Object,
                actionDescriptorProvider.Object,
                pageProvider: null,
                modelProvider: null,
                razorPageFactoryProvider: razorPageFactoryProvider,
                razorProject: razorProject);

            var compiledDescriptor = CreateCompiledPageActionDescriptor(descriptor);

            // Act
            var factories = invokerProvider.GetPageStartFactories(compiledDescriptor);

            // Assert
            mock.Verify();
        }

        [Fact]
        public void GetPageStartFactories_NoFactoriesForMissingFiles()
        {
            // Arrange
            var descriptor = new PageActionDescriptor()
            {
                RelativePath = "Path1",
                FilterDescriptors = new FilterDescriptor[0],
                ViewEnginePath = "/Views/Deeper/Index.cshtml"
            };
            var loader = new Mock<IPageLoader>();
            loader.Setup(l => l.Load(It.IsAny<PageActionDescriptor>()))
                .Returns(CreateCompiledPageActionDescriptor(descriptor, typeof(TestPageModel)));
            var descriptorCollection = new ActionDescriptorCollection(new[] { descriptor }, version: 1);
            var actionDescriptorProvider = new Mock<IActionDescriptorCollectionProvider>();
            actionDescriptorProvider.Setup(p => p.ActionDescriptors).Returns(descriptorCollection);

            // No files
            var fileProvider = new TestFileProvider();

            var razorProject = CreateRazorProject(fileProvider);

            var invokerProvider = CreateInvokerProvider(
                loader.Object,
                actionDescriptorProvider.Object,
                pageProvider: null,
                modelProvider: null,
                razorPageFactoryProvider: CreateRazorPageFactoryProvider(),
                razorProject: razorProject);

            var compiledDescriptor = CreateCompiledPageActionDescriptor(descriptor);

            // Act
            var factories = invokerProvider.GetPageStartFactories(compiledDescriptor);

            // Assert
            Assert.Empty(factories);
        }

        private IRazorPageFactoryProvider CreateRazorPageFactoryProvider()
        {
            var mock = new Mock<IRazorPageFactoryProvider>();
            mock.Setup(p => p.CreateFactory(It.IsAny<string>()))
                .Returns(new RazorPageFactoryResult(() => null, new List<IChangeToken>()));
            return mock.Object;
        }

        private RazorProject CreateRazorProject(IFileProvider fileProvider)
        {
            return new DefaultRazorProject(fileProvider);
        }

        private static CompiledPageActionDescriptor CreateCompiledPageActionDescriptor(
            PageActionDescriptor descriptor,
            Type pageType = null)
        {
            TypeInfo modelTypeInfo = null;
            if (pageType != null)
            {
                modelTypeInfo = pageType.GetTypeInfo().GetProperty("Model")?.PropertyType.GetTypeInfo();
            }

            return new CompiledPageActionDescriptor(descriptor)
            {
                ModelTypeInfo = modelTypeInfo,
                PageTypeInfo = (pageType ?? typeof(object)).GetTypeInfo()
            };
        }

        private static PageActionInvokerProvider CreateInvokerProvider(
            IPageLoader loader,
            IActionDescriptorCollectionProvider actionDescriptorProvider,
            IPageFactoryProvider pageProvider = null,
            IPageModelFactoryProvider modelProvider = null,
            IRazorPageFactoryProvider razorPageFactoryProvider = null,
            RazorProject razorProject = null)
        {
            var tempDataFactory = new Mock<ITempDataDictionaryFactory>();
            tempDataFactory.Setup(t => t.GetTempData(It.IsAny<HttpContext>()))
                .Returns((HttpContext context) => new TempDataDictionary(context, Mock.Of<ITempDataProvider>()));

            if (razorProject == null)
            {
                razorProject = Mock.Of<RazorProject>();
            }

            return new PageActionInvokerProvider(
                loader,
                pageProvider ?? Mock.Of<IPageFactoryProvider>(),
                modelProvider ?? Mock.Of<IPageModelFactoryProvider>(),
                razorPageFactoryProvider ?? Mock.Of<IRazorPageFactoryProvider>(),
                actionDescriptorProvider,
                new IFilterProvider[0],
                new EmptyModelMetadataProvider(),
                tempDataFactory.Object,
                new TestOptionsManager<MvcOptions>(),
                new TestOptionsManager<HtmlHelperOptions>(),
                Mock.Of<IPageHandlerMethodSelector>(),
                new TempDataPropertyProvider(),
                razorProject,
                new DiagnosticListener("Microsoft.AspNetCore"),
                NullLoggerFactory.Instance);
        }

        private class GenericClassModel
        {
            public void OnGet<T>()
            {

            }
        }

        private class TestSetPageWithModel
        {
            public TestSetPageModel Model { get; set; }
        }

        private class InheritsMethods : TestSetPageModel
        {
            public new void OnGet()
            {

            }
        }

        private class ProtectedModel
        {
            protected void OnGet()
            {

            }

            private void OnPost()
            {

            }
        }

        private class TestSetPageModel
        {
            public void OnGet()
            {

            }

            public void OnPost()
            {

            }
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
    }
}
