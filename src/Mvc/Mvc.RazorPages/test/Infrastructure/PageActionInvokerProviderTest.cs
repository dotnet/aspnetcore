// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

public class PageInvokerProviderTest
{
    [Fact]
    public void OnProvidersExecuting_WithEmptyModel_PopulatesCacheEntry()
    {
        // Arrange
        var descriptor = CreateCompiledPageActionDescriptor(new PageActionDescriptor
        {
            RelativePath = "/Path1",
            FilterDescriptors = new FilterDescriptor[0],
        });

        Func<PageContext, ViewContext, object> factory = (a, b) => null;
        Func<PageContext, ViewContext, object, ValueTask> releaser = (a, b, c) => default;

        var loader = Mock.Of<PageLoader>();

        var pageFactoryProvider = new Mock<IPageFactoryProvider>();
        pageFactoryProvider
            .Setup(f => f.CreatePageFactory(It.IsAny<CompiledPageActionDescriptor>()))
            .Returns(factory);
        pageFactoryProvider
            .Setup(f => f.CreateAsyncPageDisposer(It.IsAny<CompiledPageActionDescriptor>()))
            .Returns(releaser);

        var invokerProvider = CreateInvokerProvider(
            loader,
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
        Assert.NotNull(entry.ViewDataFactory);
    }

    [Fact]
    public void OnProvidersExecuting_WithModel_PopulatesCacheEntry()
    {
        // Arrange
        var descriptor = CreateCompiledPageActionDescriptor(
            new PageActionDescriptor
            {
                RelativePath = "/Path1",
                FilterDescriptors = new FilterDescriptor[0]
            },
            pageType: typeof(PageWithModel),
            modelType: typeof(DerivedTestPageModel));

        Func<PageContext, ViewContext, object> factory = (a, b) => null;
        Func<PageContext, ViewContext, object, ValueTask> releaser = (a, b, c) => default;
        Func<PageContext, object> modelFactory = _ => null;
        Func<PageContext, object, ValueTask> modelDisposer = (_, __) => default;

        var loader = Mock.Of<PageLoader>();
        var pageFactoryProvider = new Mock<IPageFactoryProvider>();
        pageFactoryProvider
            .Setup(f => f.CreatePageFactory(It.IsAny<CompiledPageActionDescriptor>()))
            .Returns(factory);
        pageFactoryProvider
            .Setup(f => f.CreateAsyncPageDisposer(It.IsAny<CompiledPageActionDescriptor>()))
            .Returns(releaser);

        var modelFactoryProvider = new Mock<IPageModelFactoryProvider>();
        modelFactoryProvider
            .Setup(f => f.CreateModelFactory(It.IsAny<CompiledPageActionDescriptor>()))
            .Returns(modelFactory);
        modelFactoryProvider
            .Setup(f => f.CreateAsyncModelDisposer(It.IsAny<CompiledPageActionDescriptor>()))
            .Returns(modelDisposer);

        var invokerProvider = CreateInvokerProvider(
            loader,
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
        Assert.NotNull(entry.ViewDataFactory);

        var pageContext = actionInvoker.PageContext;
        Assert.Same(compiledPageActionDescriptor, pageContext.ActionDescriptor);
        Assert.Same(context.ActionContext.HttpContext, pageContext.HttpContext);
        Assert.Same(context.ActionContext.ModelState, pageContext.ModelState);
        Assert.Same(context.ActionContext.RouteData, pageContext.RouteData);
        Assert.Empty(pageContext.ValueProviderFactories);
        Assert.NotNull(Assert.IsType<ViewDataDictionary<TestPageModel>>(pageContext.ViewData));
        Assert.Empty(pageContext.ViewStartFactories);
    }

    [Fact]
    public void OnProvidersExecuting_CachesViewStartFactories()
    {
        // Arrange
        var descriptor = CreateCompiledPageActionDescriptor(new PageActionDescriptor
        {
            RelativePath = "/Home/Path1/File.cshtml",
            ViewEnginePath = "/Home/Path1/File.cshtml",
            FilterDescriptors = new FilterDescriptor[0],
        }, pageType: typeof(PageWithModel));

        var loader = Mock.Of<PageLoader>();
        var razorPageFactoryProvider = new Mock<IRazorPageFactoryProvider>();

        Func<IRazorPage> factory1 = () => null;
        Func<IRazorPage> factory2 = () => null;

        razorPageFactoryProvider
            .Setup(f => f.CreateFactory("/Home/Path1/_ViewStart.cshtml"))
            .Returns(new RazorPageFactoryResult(new CompiledViewDescriptor(), factory1));
        razorPageFactoryProvider
            .Setup(f => f.CreateFactory("/_ViewStart.cshtml"))
            .Returns(new RazorPageFactoryResult(new CompiledViewDescriptor(), factory2));

        var fileProvider = new TestFileProvider();
        fileProvider.AddFile("/Home/Path1/_ViewStart.cshtml", "content1");
        fileProvider.AddFile("/_ViewStart.cshtml", "content2");

        var invokerProvider = CreateInvokerProvider(
            loader,
            razorPageFactoryProvider: razorPageFactoryProvider.Object);

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
        var descriptor = CreateCompiledPageActionDescriptor(new PageActionDescriptor
        {
            RelativePath = "/Path1",
            FilterDescriptors = new FilterDescriptor[0],
        });

        var loader = Mock.Of<PageLoader>();

        var invokerProvider = CreateInvokerProvider(
            loader);

        var context = new ActionInvokerProviderContext(new ActionContext
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
        context = new ActionInvokerProviderContext(new ActionContext
        {
            ActionDescriptor = descriptor,
            HttpContext = new DefaultHttpContext(),
            RouteData = new RouteData(),
        });
        invokerProvider.OnProvidersExecuting(context);

        // Assert - 2
        Assert.NotNull(context.Result);
        actionInvoker = Assert.IsType<PageActionInvoker>(context.Result);
        var entry2 = actionInvoker.CacheEntry;
        Assert.Same(entry1, entry2);
    }

    [Fact]
    public void OnProvidersExecuting_DoesNotInvokePageLoader_WhenEndpointRoutingIsUsed()
    {
        // Arrange
        var descriptor = CreateCompiledPageActionDescriptor(new PageActionDescriptor
        {
            RelativePath = "/Path1",
            FilterDescriptors = new FilterDescriptor[0],
        });

        var loader = new Mock<PageLoader>();
        var invokerProvider = CreateInvokerProvider(
            loader.Object,
            mvcOptions: new MvcOptions { EnableEndpointRouting = true });

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
        Assert.IsType<PageActionInvoker>(context.Result);
        loader.Verify(l => l.LoadAsync(It.IsAny<PageActionDescriptor>(), It.IsAny<EndpointMetadataCollection>()), Times.Never());
    }

    [Fact]
    public void OnProvidersExecuting_InvokesPageLoader_WithoutEndpointRouting()
    {
        // Arrange
        var descriptor = new PageActionDescriptor
        {
            RelativePath = "/Path1",
            FilterDescriptors = new FilterDescriptor[0],
        };

        var loader = new Mock<PageLoader>();
        loader.Setup(l => l.LoadAsync(descriptor, EndpointMetadataCollection.Empty))
            .ReturnsAsync(CreateCompiledPageActionDescriptor(descriptor));

        var invokerProvider = CreateInvokerProvider(
            loader.Object,
            mvcOptions: new MvcOptions { EnableEndpointRouting = false });

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
        Assert.IsType<PageActionInvoker>(context.Result);
        loader.Verify(l => l.LoadAsync(It.IsAny<PageActionDescriptor>(), It.IsAny<EndpointMetadataCollection>()), Times.Once());
    }

    [Fact]
    public void CacheUpdatesWhenDescriptorChanges()
    {
        // Arrange
        var descriptor = CreateCompiledPageActionDescriptor(new PageActionDescriptor
        {
            RelativePath = "/Path1",
            FilterDescriptors = new FilterDescriptor[0],
        });

        var descriptor2 = CreateCompiledPageActionDescriptor(new PageActionDescriptor
        {
            RelativePath = "/Path1",
            FilterDescriptors = new FilterDescriptor[0],
        });

        var loader = Mock.Of<PageLoader>();

        var invokerProvider = CreateInvokerProvider(
             loader);

        var context1 = new ActionInvokerProviderContext(new ActionContext()
        {
            ActionDescriptor = descriptor,
            HttpContext = new DefaultHttpContext(),
            RouteData = new RouteData(),
        });

        // Act - 1
        invokerProvider.OnProvidersExecuting(context1);

        // Assert - 1
        Assert.NotNull(context1.Result);
        var actionInvoker = Assert.IsType<PageActionInvoker>(context1.Result);
        var entry1 = actionInvoker.CacheEntry;

        // Act - 2

        var context2 = new ActionInvokerProviderContext(new ActionContext()
        {
            ActionDescriptor = descriptor2,
            HttpContext = new DefaultHttpContext(),
            RouteData = new RouteData(),
        });

        invokerProvider.OnProvidersExecuting(context2);

        // Assert
        Assert.NotNull(context2.Result);
        actionInvoker = Assert.IsType<PageActionInvoker>(context2.Result);
        var entry2 = actionInvoker.CacheEntry;
        Assert.NotSame(entry1, entry2);
    }

    [Fact]
    public void GetViewStartFactories_FindsFullHierarchy()
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

        var loader = new Mock<PageLoader>();
        loader
            .Setup(l => l.LoadAsync(It.IsAny<PageActionDescriptor>(), It.IsAny<EndpointMetadataCollection>()))
            .ReturnsAsync(compiledPageDescriptor);

        var mock = new Mock<IRazorPageFactoryProvider>(MockBehavior.Strict);
        mock
            .Setup(p => p.CreateFactory("/Pages/Level1/Level2/_ViewStart.cshtml"))
            .Returns(new RazorPageFactoryResult(new CompiledViewDescriptor(), () => null))
            .Verifiable();
        mock
            .Setup(p => p.CreateFactory("/Pages/Level1/_ViewStart.cshtml"))
            .Returns(new RazorPageFactoryResult(new CompiledViewDescriptor(), () => null))
            .Verifiable();
        mock
            .Setup(p => p.CreateFactory("/Pages/_ViewStart.cshtml"))
            .Returns(new RazorPageFactoryResult(new CompiledViewDescriptor(), () => null))
            .Verifiable();
        mock
            .Setup(p => p.CreateFactory("/_ViewStart.cshtml"))
            .Returns(new RazorPageFactoryResult(new CompiledViewDescriptor(), () => null))
            .Verifiable();

        var razorPageFactoryProvider = mock.Object;

        var invokerProvider = CreateInvokerProvider(
            loader.Object,
            razorPageFactoryProvider: razorPageFactoryProvider);

        // Act
        var factories = invokerProvider.Cache.GetViewStartFactories(compiledPageDescriptor);

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

        var loader = new Mock<PageLoader>();
        loader
            .Setup(l => l.LoadAsync(It.IsAny<PageActionDescriptor>(), It.IsAny<EndpointMetadataCollection>()))
            .ReturnsAsync(CreateCompiledPageActionDescriptor(descriptor, typeof(TestPageModel)));

        var pageFactory = new Mock<IRazorPageFactoryProvider>();
        pageFactory
            .Setup(f => f.CreateFactory("/Views/Deeper/_ViewStart.cshtml"))
            .Returns(new RazorPageFactoryResult(new CompiledViewDescriptor(), () => null));
        pageFactory
            .Setup(f => f.CreateFactory("/Views/_ViewStart.cshtml"))
            .Returns(new RazorPageFactoryResult(new CompiledViewDescriptor(), razorPageFactory: null));
        pageFactory
            .Setup(f => f.CreateFactory("/_ViewStart.cshtml"))
            .Returns(new RazorPageFactoryResult(new CompiledViewDescriptor(), () => null));

        // No files
        var fileProvider = new TestFileProvider();

        var invokerProvider = CreateInvokerProvider(
            loader.Object,
            pageProvider: null,
            modelProvider: null,
            razorPageFactoryProvider: pageFactory.Object);

        var compiledDescriptor = CreateCompiledPageActionDescriptor(descriptor);

        // Act
        var factories = invokerProvider.Cache.GetViewStartFactories(compiledDescriptor).ToList();

        // Assert
        Assert.Equal(2, factories.Count);
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
        PageLoader loader,
        IPageFactoryProvider pageProvider = null,
        IPageModelFactoryProvider modelProvider = null,
        IRazorPageFactoryProvider razorPageFactoryProvider = null,
        MvcOptions mvcOptions = null)
    {
        var tempDataFactory = new Mock<ITempDataDictionaryFactory>();
        tempDataFactory
            .Setup(t => t.GetTempData(It.IsAny<HttpContext>()))
            .Returns((HttpContext context) => new TempDataDictionary(context, Mock.Of<ITempDataProvider>()));

        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var modelBinderFactory = TestModelBinderFactory.CreateDefault();
        mvcOptions = mvcOptions ?? new MvcOptions();

        var parameterBinder = new ParameterBinder(
            modelMetadataProvider,
            TestModelBinderFactory.CreateDefault(),
            Mock.Of<IObjectModelValidator>(),
            Options.Create(mvcOptions),
            NullLoggerFactory.Instance);

        var cache = new PageActionInvokerCache(
            pageProvider ?? Mock.Of<IPageFactoryProvider>(),
            modelProvider ?? Mock.Of<IPageModelFactoryProvider>(),
            razorPageFactoryProvider ?? Mock.Of<IRazorPageFactoryProvider>(),
            new IFilterProvider[0],
            parameterBinder,
            modelMetadataProvider,
            modelBinderFactory);

        return new PageActionInvokerProvider(
            loader,
            cache,
            modelMetadataProvider,
            tempDataFactory.Object,
            Options.Create(mvcOptions),
            Options.Create(new MvcViewOptions()),
            Mock.Of<IPageHandlerMethodSelector>(),
            new DiagnosticListener("Microsoft.AspNetCore"),
            NullLoggerFactory.Instance,
            new ActionResultTypeMapper());
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
