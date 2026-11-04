// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

public class DefaultPageFactoryProviderTest
{
    [Fact]
    public void CreatePage_ThrowsIfActivatedInstanceIsNotAnInstanceOfRazorPage()
    {
        // Arrange
        var descriptor = new CompiledPageActionDescriptor
        {
            PageTypeInfo = typeof(object).GetTypeInfo(),
        };

        var pageActivator = CreateActivator();
        var factoryProvider = CreatePageFactory();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => factoryProvider.CreatePageFactory(descriptor));
        Assert.Equal(
            $"Page created by '{pageActivator.GetType()}' must be an instance of '{typeof(PageBase)}'.",
            ex.Message);
    }

    [Fact]
    public void PageFactorySetsPageContext()
    {
        // Arrange
        var descriptor = new CompiledPageActionDescriptor
        {
            PageTypeInfo = typeof(TestPage).GetTypeInfo(),
        };
        var pageContext = new PageContext
        {
            ActionDescriptor = descriptor
        };
        var viewContext = new ViewContext();
        var factoryProvider = CreatePageFactory();

        // Act
        var factory = factoryProvider.CreatePageFactory(descriptor);
        var instance = factory(pageContext, viewContext);

        // Assert
        var testPage = Assert.IsType<TestPage>(instance);
        Assert.Same(pageContext, testPage.PageContext);
    }

    [Fact]
    public void PageFactorySetsPropertiesWithRazorInject()
    {
        // Arrange
        var pageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(TestPage).GetTypeInfo(),
            }
        };

        var viewContext = new ViewContext();

        var urlHelperFactory = new Mock<IUrlHelperFactory>();
        var urlHelper = Mock.Of<IUrlHelper>();
        urlHelperFactory
            .Setup(f => f.GetUrlHelper(viewContext))
            .Returns(urlHelper)
            .Verifiable();

        var htmlEncoder = HtmlEncoder.Create();

        var factoryProvider = CreatePageFactory(
            urlHelperFactory: urlHelperFactory.Object,
            htmlEncoder: htmlEncoder);

        // Act
        var factory = factoryProvider.CreatePageFactory(pageContext.ActionDescriptor);
        var instance = factory(pageContext, viewContext);

        // Assert
        var testPage = Assert.IsType<TestPage>(instance);
        Assert.Same(urlHelper, testPage.UrlHelper);
        Assert.Same(htmlEncoder, testPage.HtmlEncoder);
        Assert.NotNull(testPage.ViewData);
    }

    [Fact]
    public void PageFactorySetsPath()
    {
        // Arrange
        var descriptor = new CompiledPageActionDescriptor
        {
            PageTypeInfo = typeof(ViewDataTestPage).GetTypeInfo(),
            DeclaredModelTypeInfo = typeof(ViewDataTestPageModel).GetTypeInfo(),
            ModelTypeInfo = typeof(ViewDataTestPageModel).GetTypeInfo()
        };
        descriptor.RelativePath = "/this/is/a/path.cshtml";

        var pageContext = new PageContext
        {
            ActionDescriptor = descriptor
        };

        var viewContext = new ViewContext();

        // Act
        var factory = CreatePageFactory().CreatePageFactory(descriptor);
        var instance = factory(pageContext, viewContext);

        // Assert
        var testPage = Assert.IsType<ViewDataTestPage>(instance);
        Assert.Equal("/this/is/a/path.cshtml", testPage.Path);
    }

    [Fact]
    public void PageFactorySetViewDataWithModelTypeWhenNotNull()
    {
        // Arrange
        var pageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(ViewDataTestPage).GetTypeInfo(),
                DeclaredModelTypeInfo = typeof(ViewDataTestPageModel).GetTypeInfo(),
                ModelTypeInfo = typeof(ViewDataTestPageModel).GetTypeInfo(),
            },
        };

        var viewContext = new ViewContext();

        var factoryProvider = CreatePageFactory();

        // Act
        var factory = factoryProvider.CreatePageFactory(pageContext.ActionDescriptor);
        var instance = factory(pageContext, viewContext);

        // Assert
        var testPage = Assert.IsType<ViewDataTestPage>(instance);
        Assert.NotNull(testPage.ViewData);
    }

    [Fact]
    public void PageFactorySetViewDataWithDeclaredModelTypeWhenNotNull()
    {
        // Arrange
        var pageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(ViewDataTestPage).GetTypeInfo(),
                DeclaredModelTypeInfo = typeof(ViewDataTestPageModel).GetTypeInfo(),
                ModelTypeInfo = typeof(DerivedViewDataTestPageModel).GetTypeInfo(),
            },
        };

        var viewContext = new ViewContext();

        var factoryProvider = CreatePageFactory();

        // Act
        var factory = factoryProvider.CreatePageFactory(pageContext.ActionDescriptor);
        var instance = factory(pageContext, viewContext);

        // Assert
        var testPage = Assert.IsType<ViewDataTestPage>(instance);
        Assert.NotNull(testPage.ViewData);
    }

    [Fact]
    public void PageFactorySetsNonGenericViewDataDictionary()
    {
        // Arrange
        var pageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(NonGenericViewDataTestPage).GetTypeInfo()
            },
        };

        var viewContext = new ViewContext();

        var factoryProvider = CreatePageFactory();

        // Act
        var factory = factoryProvider.CreatePageFactory(pageContext.ActionDescriptor);
        var instance = factory(pageContext, viewContext);

        // Assert
        var testPage = Assert.IsType<NonGenericViewDataTestPage>(instance);
        Assert.NotNull(testPage.ViewData);
    }

    [Fact]
    public void PageFactory_SetsViewDataOnPage_FromPageContext()
    {
        // Arrange
        var modelMetadataProvider = new EmptyModelMetadataProvider();
        var pageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(TestPage).GetTypeInfo()
            },
            ViewData = new ViewDataDictionary<TestPage>(modelMetadataProvider, new ModelStateDictionary())
                {
                    { "test-key", "test-value" },
                }
        };

        var viewContext = new ViewContext()
        {
            HttpContext = pageContext.HttpContext,
            ViewData = pageContext.ViewData,
        };

        var factoryProvider = CreatePageFactory();

        // Act
        var factory = factoryProvider.CreatePageFactory(pageContext.ActionDescriptor);
        var instance = factory(pageContext, viewContext);

        // Assert
        var testPage = Assert.IsType<TestPage>(instance);
        Assert.NotNull(testPage.ViewData);
        Assert.Same(pageContext.ViewData, testPage.ViewData);
        Assert.Equal("test-value", testPage.ViewData["test-key"]);
    }

    [Fact]
    public void PageFactoryDoesNotBindPropertiesWithNoRazorInjectAttribute()
    {
        // Arrange
        var serviceProvider = new ServiceCollection()
            .AddSingleton<ILogger>(NullLogger.Instance)
            .BuildServiceProvider();

        var pageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(PropertiesWithoutRazorInject).GetTypeInfo()
            },
            HttpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider,
            },
        };

        var viewContext = new ViewContext()
        {
            HttpContext = pageContext.HttpContext,
        };

        var factoryProvider = CreatePageFactory();

        // Act
        var factory = factoryProvider.CreatePageFactory(pageContext.ActionDescriptor);
        var instance = factory(pageContext, viewContext);

        // Assert
        var testPage = Assert.IsType<PropertiesWithoutRazorInject>(instance);
        Assert.Null(testPage.DiagnosticSourceWithoutInject);
        Assert.NotNull(testPage.DiagnosticSourceWithInject);

        Assert.Null(testPage.LoggerWithoutInject);
        Assert.NotNull(testPage.LoggerWithInject);

        Assert.Null(testPage.ModelExpressionProviderWithoutInject);
        Assert.NotNull(testPage.ModelExpressionProviderWithInject);
    }

    [Fact]
    public void PageFactoryCreatePageDisposerCreatesDisposerForPage()
    {
        // Arrange
        var serviceProvider = new ServiceCollection()
            .AddSingleton<ILogger>(NullLogger.Instance)
            .BuildServiceProvider();

        var pageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(DisposablePage).GetTypeInfo()
            },
            HttpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider,
            },
        };

        var viewContext = new ViewContext()
        {
            HttpContext = pageContext.HttpContext,
        };

        var factoryProvider = CreatePageFactory();

        // Act
        var factory = factoryProvider.CreatePageFactory(pageContext.ActionDescriptor);
        var instance = factory(pageContext, viewContext);

        var disposer = factoryProvider.CreatePageDisposer(pageContext.ActionDescriptor);
        disposer(pageContext, viewContext, instance);

        // Assert
        Assert.True(((DisposablePage)instance).Disposed);
    }

    [Fact]
    public async Task PageFactoryCreateAsyncPageDisposerCreatesDisposerForPage()
    {
        // Arrange
        var serviceProvider = new ServiceCollection()
            .AddSingleton<ILogger>(NullLogger.Instance)
            .BuildServiceProvider();

        var pageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(DisposablePage).GetTypeInfo()
            },
            HttpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider,
            },
        };

        var viewContext = new ViewContext()
        {
            HttpContext = pageContext.HttpContext,
        };

        var factoryProvider = CreatePageFactory();

        // Act
        var factory = factoryProvider.CreatePageFactory(pageContext.ActionDescriptor);
        var instance = factory(pageContext, viewContext);

        var disposer = factoryProvider.CreateAsyncPageDisposer(pageContext.ActionDescriptor);
        await disposer(pageContext, viewContext, instance);

        // Assert
        Assert.True(((DisposablePage)instance).Disposed);
    }

    private static DefaultPageFactoryProvider CreatePageFactory(
        IPageActivatorProvider pageActivator = null,
        IModelMetadataProvider provider = null,
        IUrlHelperFactory urlHelperFactory = null,
        IJsonHelper jsonHelper = null,
        DiagnosticListener diagnosticListener = null,
        HtmlEncoder htmlEncoder = null,
        IModelExpressionProvider modelExpressionProvider = null)
    {
        return new DefaultPageFactoryProvider(
            pageActivator ?? CreateActivator(),
            provider ?? Mock.Of<IModelMetadataProvider>(),
            urlHelperFactory ?? Mock.Of<IUrlHelperFactory>(),
            jsonHelper ?? Mock.Of<IJsonHelper>(),
            diagnosticListener ?? new DiagnosticListener("Microsoft.AspNetCore.Mvc.RazorPages"),
            htmlEncoder ?? HtmlEncoder.Default,
            modelExpressionProvider ?? Mock.Of<IModelExpressionProvider>());
    }

    private static IPageActivatorProvider CreateActivator()
    {
        var activator = new Mock<IPageActivatorProvider>();
        activator
            .Setup(a => a.CreateActivator(It.IsAny<CompiledPageActionDescriptor>()))
            .Returns((CompiledPageActionDescriptor descriptor) =>
            {
                return (context, viewContext) => Activator.CreateInstance(descriptor.PageTypeInfo.AsType());
            });
        activator
            .Setup(a => a.CreateReleaser(It.IsAny<CompiledPageActionDescriptor>()))
            .Returns((CompiledPageActionDescriptor descriptor) =>
            {
                return (context, viewContext, instance) => (instance as IDisposable)?.Dispose();
            });

        activator
            .Setup(a => a.CreateAsyncReleaser(It.IsAny<CompiledPageActionDescriptor>()))
            .Returns((CompiledPageActionDescriptor descriptor) =>
            {
                return (context, viewContext, instance) => instance switch
                {
                    IAsyncDisposable asyncDisposable => asyncDisposable.DisposeAsync(),
                    IDisposable disposable => SyncDispose(disposable),
                    _ => default
                };
            });

        ValueTask SyncDispose(IDisposable disposable)
        {
            disposable.Dispose();
            return default;
        }

        return activator.Object;
    }

    private class TestPage : Page
    {
        [RazorInject]
        public IUrlHelper UrlHelper { get; set; }

        [RazorInject]
        public new HtmlEncoder HtmlEncoder { get; set; }

        [RazorInject]
        public ViewDataDictionary<TestPage> ViewData { get; set; }

        public override Task ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }

    private class NonGenericViewDataTestPage : Page
    {
        [RazorInject]
        public ViewDataDictionary ViewData { get; set; }

        public override Task ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }

    private class ViewDataTestPage : Page
    {
        [RazorInject]
        public ViewDataDictionary<ViewDataTestPageModel> ViewData { get; set; }

        public override Task ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }

    private class ViewDataTestPageModel
    {
    }

    private class DerivedViewDataTestPageModel : ViewDataTestPageModel
    {
    }

    private class DisposablePage : Page, IDisposable
    {
        public bool Disposed { get; set; }

        public void Dispose()
        {
            Disposed = true;
        }

        public override Task ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }

    private class PropertiesWithoutRazorInject : Page
    {
        public IModelExpressionProvider ModelExpressionProviderWithoutInject { get; set; }

        [RazorInject]
        public IModelExpressionProvider ModelExpressionProviderWithInject { get; set; }

        public DiagnosticSource DiagnosticSourceWithoutInject { get; set; }

        [RazorInject]
        public DiagnosticSource DiagnosticSourceWithInject { get; set; }

        public ILogger LoggerWithoutInject { get; set; }

        [RazorInject]
        public ILogger LoggerWithInject { get; set; }

        public override Task ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }
}
