// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
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
            loader
                .Setup(l => l.Load(It.IsAny<PageActionDescriptor>()))
                .Returns(CreateCompiledPageActionDescriptor(descriptor));

            var pageFactoryProvider = new Mock<IPageFactoryProvider>();
            pageFactoryProvider
                .Setup(f => f.CreatePageFactory(It.IsAny<CompiledPageActionDescriptor>()))
                .Returns(factory);
            pageFactoryProvider
                .Setup(f => f.CreatePageDisposer(It.IsAny<CompiledPageActionDescriptor>()))
                .Returns(releaser);

            var invokerProvider = CreateInvokerProvider(
                loader.Object,
                CreateActionDescriptorCollection(descriptor),
                pageFactoryProvider.Object);

            var context = new ActionInvokerProviderContext(new ActionContext()
            {
                ActionDescriptor = descriptor,
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData(),
            });

            // Act
            invokerProvider.OnProvidersExecuting(context);

            // Assert
            Assert.NotNull(context.Result);
            var actionInvoker = Assert.IsType<PageActionInvoker>(context.Result);
            var entry = actionInvoker.CacheEntry;
            Assert.Equal(descriptor.RelativePath, entry.ActionDescriptor.RelativePath);
            Assert.Same(factory, entry.PageFactory);
            Assert.Same(releaser, entry.ReleasePage);
            Assert.Null(entry.ModelFactory);
            Assert.Null(entry.ReleaseModel);
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
            loader
                .Setup(l => l.Load(It.IsAny<PageActionDescriptor>()))
                .Returns(CreateCompiledPageActionDescriptor(descriptor, pageType: typeof(PageWithModel)));

            var pageFactoryProvider = new Mock<IPageFactoryProvider>();
            pageFactoryProvider
                .Setup(f => f.CreatePageFactory(It.IsAny<CompiledPageActionDescriptor>()))
                .Returns(factory);
            pageFactoryProvider
                .Setup(f => f.CreatePageDisposer(It.IsAny<CompiledPageActionDescriptor>()))
                .Returns(releaser);

            var modelFactoryProvider = new Mock<IPageModelFactoryProvider>();
            modelFactoryProvider
                .Setup(f => f.CreateModelFactory(It.IsAny<CompiledPageActionDescriptor>()))
                .Returns(modelFactory);
            modelFactoryProvider
                .Setup(f => f.CreateModelDisposer(It.IsAny<CompiledPageActionDescriptor>()))
                .Returns(modelDisposer);

            var invokerProvider = CreateInvokerProvider(
                loader.Object,
                CreateActionDescriptorCollection(descriptor),
                pageFactoryProvider.Object,
                modelFactoryProvider.Object);

            var context = new ActionInvokerProviderContext(new ActionContext()
            {
                ActionDescriptor = descriptor,
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData(),
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
            loader
                .Setup(l => l.Load(It.IsAny<PageActionDescriptor>()))
                .Returns(CreateCompiledPageActionDescriptor(descriptor, pageType: typeof(PageWithModel)));

            var razorPageFactoryProvider = new Mock<IRazorPageFactoryProvider>();

            Func<IRazorPage> factory1 = () => null;
            Func<IRazorPage> factory2 = () => null;

            razorPageFactoryProvider
                .Setup(f => f.CreateFactory("/Home/Path1/_ViewStart.cshtml"))
                .Returns(new RazorPageFactoryResult(factory1, new IChangeToken[0]));
            razorPageFactoryProvider
                .Setup(f => f.CreateFactory("/_ViewStart.cshtml"))
                .Returns(new RazorPageFactoryResult(factory2, new[] { Mock.Of<IChangeToken>() }));

            var fileProvider = new TestFileProvider();
            fileProvider.AddFile("/Home/Path1/_ViewStart.cshtml", "content1");
            fileProvider.AddFile("/_ViewStart.cshtml", "content2");

            var defaultRazorProject = new TestRazorProject(fileProvider);

            var invokerProvider = CreateInvokerProvider(
                loader.Object,
                CreateActionDescriptorCollection(descriptor),
                razorPageFactoryProvider: razorPageFactoryProvider.Object,
                razorProject: defaultRazorProject);

            var context = new ActionInvokerProviderContext(new ActionContext()
            {
                ActionDescriptor = descriptor,
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData(),
            });

            // Act
            invokerProvider.OnProvidersExecuting(context);

            // Assert
            Assert.NotNull(context.Result);
            var actionInvoker = Assert.IsType<PageActionInvoker>(context.Result);
            var entry = actionInvoker.CacheEntry;
            Assert.Equal(new[] { factory2, factory1 }, entry.ViewStartFactories);
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
            loader
                .Setup(l => l.Load(It.IsAny<PageActionDescriptor>()))
                .Returns(CreateCompiledPageActionDescriptor(descriptor));

            var invokerProvider = CreateInvokerProvider(
                loader.Object,
                CreateActionDescriptorCollection(descriptor));

            var context = new ActionInvokerProviderContext(new ActionContext()
            {
                ActionDescriptor = descriptor,
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData(),
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
            actionDescriptorProvider
                .SetupSequence(p => p.ActionDescriptors)
                .Returns(descriptorCollection1)
                .Returns(descriptorCollection2);

            var loader = new Mock<IPageLoader>();
            loader
                .Setup(l => l.Load(It.IsAny<PageActionDescriptor>()))
                .Returns(CreateCompiledPageActionDescriptor(descriptor));

            var invokerProvider = CreateInvokerProvider(
                 loader.Object,
                 actionDescriptorProvider.Object);

            var context = new ActionInvokerProviderContext(new ActionContext()
            {
                ActionDescriptor = descriptor,
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData(),
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

        [Fact]
        public void GetViewStartFactories_FindsFullHeirarchy()
        {
            // Arrange
            var descriptor = new PageActionDescriptor()
            {
                RelativePath = "/Views/Deeper/Index.cshtml",
                FilterDescriptors = new FilterDescriptor[0],
                ViewEnginePath = "/Views/Deeper/Index.cshtml"
            };

            var loader = new Mock<IPageLoader>();
            loader
                .Setup(l => l.Load(It.IsAny<PageActionDescriptor>()))
                .Returns(CreateCompiledPageActionDescriptor(descriptor, typeof(TestPageModel)));

            var fileProvider = new TestFileProvider();
            fileProvider.AddFile("/View/Deeper/Not_ViewStart.cshtml", "page content");
            fileProvider.AddFile("/View/Wrong/_ViewStart.cshtml", "page content");
            fileProvider.AddFile("/_ViewStart.cshtml", "page content ");
            fileProvider.AddFile("/Views/_ViewStart.cshtml", "@page starts!");
            fileProvider.AddFile("/Views/Deeper/_ViewStart.cshtml", "page content");

            var razorProject = new TestRazorProject(fileProvider);

            var mock = new Mock<IRazorPageFactoryProvider>();
            mock
                .Setup(p => p.CreateFactory("/Views/Deeper/_ViewStart.cshtml"))
                .Returns(new RazorPageFactoryResult(() => null, new List<IChangeToken>()))
                .Verifiable();
            mock
                .Setup(p => p.CreateFactory("/Views/_ViewStart.cshtml"))
                .Returns(new RazorPageFactoryResult(() => null, new List<IChangeToken>()))
                .Verifiable();
            mock
                .Setup(p => p.CreateFactory("/_ViewStart.cshtml"))
                .Returns(new RazorPageFactoryResult(() => null, new List<IChangeToken>()))
                .Verifiable();

            var razorPageFactoryProvider = mock.Object;

            var invokerProvider = CreateInvokerProvider(
                loader.Object,
                CreateActionDescriptorCollection(descriptor),
                pageProvider: null,
                modelProvider: null,
                razorPageFactoryProvider: razorPageFactoryProvider,
                razorProject: razorProject);

            var compiledDescriptor = CreateCompiledPageActionDescriptor(descriptor);

            // Act
            var factories = invokerProvider.GetViewStartFactories(compiledDescriptor);

            // Assert
            mock.Verify();
        }

        [Theory]
        [InlineData("/Pages/Level1/")]
        [InlineData("/Pages/Level1")]
        public void GetPageFactories_DoesNotFindViewStartsOutsideBaseDirectory(string rootDirectory)
        {
            // Arrange
            var descriptor = new PageActionDescriptor()
            {
                RelativePath = "/Pages/Level1/Level2/Index.cshtml",
                FilterDescriptors = new FilterDescriptor[0],
                ViewEnginePath = "/Pages/Level1/Level2/Index.cshtml"
            };

            var compiledPageDescriptor = new CompiledPageActionDescriptor(descriptor)
            {
                PageTypeInfo = typeof(object).GetTypeInfo(),
            };

            var loader = new Mock<IPageLoader>();
            loader
                .Setup(l => l.Load(It.IsAny<PageActionDescriptor>()))
                .Returns(compiledPageDescriptor);

            var fileProvider = new TestFileProvider();
            fileProvider.AddFile("/_ViewStart.cshtml", "page content");
            fileProvider.AddFile("/Pages/_ViewStart.cshtml", "page content");
            fileProvider.AddFile("/Pages/Level1/_ViewStart.cshtml", "page content");
            fileProvider.AddFile("/Pages/Level1/Level2/_ViewStart.cshtml", "page content");
            fileProvider.AddFile("/Pages/Level1/Level3/_ViewStart.cshtml", "page content");

            var razorProject = new TestRazorProject(fileProvider);

            var mock = new Mock<IRazorPageFactoryProvider>(MockBehavior.Strict);
            mock
                .Setup(p => p.CreateFactory("/Pages/Level1/Level2/_ViewStart.cshtml"))
                .Returns(new RazorPageFactoryResult(() => null, new List<IChangeToken>()))
                .Verifiable();
            mock
                .Setup(p => p.CreateFactory("/Pages/Level1/_ViewStart.cshtml"))
                .Returns(new RazorPageFactoryResult(() => null, new List<IChangeToken>()))
                .Verifiable();
            var razorPageFactoryProvider = mock.Object;

            var options = new RazorPagesOptions
            {
                RootDirectory = rootDirectory,
            };

            var invokerProvider = CreateInvokerProvider(
                loader.Object,
                CreateActionDescriptorCollection(descriptor),
                razorPageFactoryProvider: razorPageFactoryProvider,
                razorProject: razorProject,
                razorPagesOptions: options);

            // Act
            var factories = invokerProvider.GetViewStartFactories(compiledPageDescriptor);

            // Assert
            mock.Verify();
        }

        [Fact]
        public void GetViewStartFactories_ReturnsFactoriesForFilesThatDoNotExistInProject()
        {
            // The factory provider might have access to _ViewStarts for files that do not exist on disk \ RazorProject.
            // This test verifies that we query the factory provider correctly.
            // Arrange
            var descriptor = new PageActionDescriptor()
            {
                RelativePath = "/Views/Deeper/Index.cshtml",
                FilterDescriptors = new FilterDescriptor[0],
                ViewEnginePath = "/Views/Deeper/Index.cshtml"
            };

            var loader = new Mock<IPageLoader>();
            loader
                .Setup(l => l.Load(It.IsAny<PageActionDescriptor>()))
                .Returns(CreateCompiledPageActionDescriptor(descriptor, typeof(TestPageModel)));

            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            pageFactory
                .Setup(f => f.CreateFactory("/Views/Deeper/_ViewStart.cshtml"))
                .Returns(new RazorPageFactoryResult(() => null, new IChangeToken[0]));
            pageFactory
                .Setup(f => f.CreateFactory("/Views/_ViewStart.cshtml"))
                .Returns(new RazorPageFactoryResult(new IChangeToken[0]));
            pageFactory
                .Setup(f => f.CreateFactory("/_ViewStart.cshtml"))
                .Returns(new RazorPageFactoryResult(() => null, new IChangeToken[0]));

            // No files
            var fileProvider = new TestFileProvider();
            var razorProject = new TestRazorProject(fileProvider);

            var invokerProvider = CreateInvokerProvider(
                loader.Object,
                CreateActionDescriptorCollection(descriptor),
                pageProvider: null,
                modelProvider: null,
                razorPageFactoryProvider: pageFactory.Object,
                razorProject: razorProject);

            var compiledDescriptor = CreateCompiledPageActionDescriptor(descriptor);

            // Act
            var factories = invokerProvider.GetViewStartFactories(compiledDescriptor).ToList();

            // Assert
            Assert.Equal(2, factories.Count);
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
            RazorProject razorProject = null,
            RazorPagesOptions razorPagesOptions = null)
        {
            var tempDataFactory = new Mock<ITempDataDictionaryFactory>();
            tempDataFactory
                .Setup(t => t.GetTempData(It.IsAny<HttpContext>()))
                .Returns((HttpContext context) => new TempDataDictionary(context, Mock.Of<ITempDataProvider>()));

            if (razorProject == null)
            {
                razorProject = Mock.Of<RazorProject>();
            }

            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var parameterBinder = new ParameterBinder(
                modelMetadataProvider,
                TestModelBinderFactory.CreateDefault(),
                Mock.Of<IObjectModelValidator>());

            return new PageActionInvokerProvider(
                loader,
                pageProvider ?? Mock.Of<IPageFactoryProvider>(),
                modelProvider ?? Mock.Of<IPageModelFactoryProvider>(),
                razorPageFactoryProvider ?? Mock.Of<IRazorPageFactoryProvider>(),
                actionDescriptorProvider,
                new IFilterProvider[0],
                parameterBinder,
                modelMetadataProvider,
                tempDataFactory.Object,
                new TestOptionsManager<MvcOptions>(),
                new TestOptionsManager<HtmlHelperOptions>(),
                new TestOptionsManager<RazorPagesOptions>(razorPagesOptions ?? new RazorPagesOptions()),
                Mock.Of<IPageHandlerMethodSelector>(),
                razorProject,
                new DiagnosticListener("Microsoft.AspNetCore"),
                NullLoggerFactory.Instance);
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
    }
}
