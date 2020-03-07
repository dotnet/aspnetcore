// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    public class RazorPageActivatorTest
    {
        public RazorPageActivatorTest()
        {
            DiagnosticListener = new DiagnosticListener("Microsoft.AspNetCore");
            HtmlEncoder = new HtmlTestEncoder();
            JsonHelper = Mock.Of<IJsonHelper>();
            MetadataProvider = new EmptyModelMetadataProvider();
            ModelExpressionProvider = new ModelExpressionProvider(MetadataProvider);
            UrlHelperFactory = new UrlHelperFactory();
        }

        private DiagnosticListener DiagnosticListener { get; }

        private HtmlEncoder HtmlEncoder { get; }

        private IJsonHelper JsonHelper { get; }

        private IModelMetadataProvider MetadataProvider { get; }

        private IModelExpressionProvider ModelExpressionProvider { get; }

        private IUrlHelperFactory UrlHelperFactory { get; }

        [Fact]
        public void Activate_ContextualizesServices_AndSetsProperties_OnPage()
        {
            // Arrange
            var activator = CreateActivator();

            var instance = new TestRazorPage();
            var viewData = new ViewDataDictionary<MyModel>(MetadataProvider, new ModelStateDictionary());
            var viewContext = CreateViewContext();

            var urlHelper = UrlHelperFactory.GetUrlHelper(viewContext);

            // Act
            activator.Activate(instance, viewContext);

            // Assert
            Assert.Same(DiagnosticListener, instance.DiagnosticSource);
            Assert.Same(HtmlEncoder, instance.HtmlEncoder);
            Assert.Same(JsonHelper, instance.Json);
            Assert.Same(urlHelper, instance.Url);
            Assert.Same(viewContext.ViewData, instance.ViewData);

            // Has no [RazorInject] so it shouldn't get injected
            Assert.Null(instance.MyService2);

            // We're not testing the IViewContextualizable implementation here because it's a mock.
            Assert.NotNull(instance.Html);
            Assert.IsAssignableFrom<IHtmlHelper<object>>(instance.Html);

            var service = instance.MyService;
            Assert.NotNull(service);
            Assert.Same(viewContext, service.ViewContext);
        }

        [Fact]
        public void Activate_ContextualizesServices_AndSetsProperties_OnPageWithoutModel()
        {
            // Arrange
            var activator = CreateActivator();

            var viewData = new ViewDataDictionary<object>(MetadataProvider, new ModelStateDictionary());
            var viewContext = CreateViewContext(viewData);

            var urlHelper = UrlHelperFactory.GetUrlHelper(viewContext);

            var instance = new NoModelPropertyPage();

            // Act
            activator.Activate(instance, viewContext);

            // Assert
            Assert.Same(DiagnosticListener, instance.DiagnosticSource);
            Assert.Same(HtmlEncoder, instance.HtmlEncoder);

            // When we don't have a model property, the activator will just leave ViewData alone.
            Assert.NotNull(viewContext.ViewData);
        }

        [Fact]
        public void Activate_InstantiatesNewViewDataDictionaryType_IfTheTypeDoesNotMatch()
        {
            // Arrange
            var activator = CreateActivator();

            var viewData = new ViewDataDictionary<object>(MetadataProvider, new ModelStateDictionary())
            {
                { "key", "value" },
            };
            var viewContext = CreateViewContext(viewData);

            var urlHelper = UrlHelperFactory.GetUrlHelper(viewContext);

            var instance = new TestRazorPage();

            // Act
            activator.Activate(instance, viewContext);

            // Assert
            Assert.Same(DiagnosticListener, instance.DiagnosticSource);
            Assert.Same(HtmlEncoder, instance.HtmlEncoder);
            Assert.Same(JsonHelper, instance.Json);
            Assert.Same(urlHelper, instance.Url);
            Assert.Same(viewContext.ViewData, instance.ViewData);

            // The original ViewDataDictionary was replaced.
            Assert.NotSame(viewData, viewContext.ViewData);
            Assert.NotSame(viewData, instance.ViewData);

            // But this value is copied
            Assert.Equal("value", viewData["key"]);

            // Has no [RazorInject] so it shouldn't get injected
            Assert.Null(instance.MyService2);

            // We're not testing the IViewContextualizable implementation here because it's a mock.
            Assert.NotNull(instance.Html);
            Assert.IsAssignableFrom<IHtmlHelper<object>>(instance.Html);

            var service = instance.MyService;
            Assert.NotNull(service);
            Assert.Same(viewContext, service.ViewContext);
        }

        [Fact]
        public void Activate_Throws_WhenViewDataPropertyHasIncorrectType()
        {
            // Arrange
            var activator = CreateActivator();

            var viewData = new ViewDataDictionary<MyModel>(MetadataProvider, new ModelStateDictionary());
            var viewContext = CreateViewContext(viewData);

            var instance = new HasIncorrectViewDataPropertyType();

            // Act & Assert
            Assert.Throws<InvalidCastException>(() => activator.Activate(instance, viewContext));
        }

        [Fact]
        public void Activate_UsesModelFromModelTypeProvider()
        {
            // Arrange
            var activator = CreateActivator();

            var viewData = new ViewDataDictionary<object>(MetadataProvider, new ModelStateDictionary())
            {
                { "key", "value" },
            };
            var viewContext = CreateViewContext(viewData);
            var page = new ModelTypeProviderRazorPage();

            // Act
            activator.Activate(page, viewContext);

            // Assert
            Assert.Same(viewContext.ViewData, page.ViewData);
            Assert.NotSame(viewData, viewContext.ViewData);

            Assert.IsType<ViewDataDictionary<Guid>>(viewContext.ViewData);
            Assert.Equal("value", viewContext.ViewData["key"]);
        }

        [Fact]
        public void GetOrAddCacheEntry_CachesPages()
        {
            // Arrange
            var activator = CreateActivator();
            var page = new TestRazorPage();

            // Act
            var result1 = activator.GetOrAddCacheEntry(page);
            var result2 = activator.GetOrAddCacheEntry(page);

            // Assert
            Assert.Same(result1, result2);
        }

        [Fact]
        public void GetOrAddCacheEntry_VariesByModelType_IfPageIsModelTypeProvider()
        {
            // Arrange
            var activator = CreateActivator();
            var page = new ModelTypeProviderRazorPage();

            // Act - 1
            var result1 = activator.GetOrAddCacheEntry(page);
            var result2 = activator.GetOrAddCacheEntry(page);

            // Assert - 1
            Assert.Same(result1, result2);

            // Act - 2
            page.ModelType = typeof(string);
            var result3 = activator.GetOrAddCacheEntry(page);
            var result4 = activator.GetOrAddCacheEntry(page);

            // Assert - 2
            Assert.Same(result3, result4);
            Assert.NotSame(result1, result3);
        }

        private RazorPageActivator CreateActivator()
        {
            return new RazorPageActivator(MetadataProvider, UrlHelperFactory, JsonHelper, DiagnosticListener, HtmlEncoder, ModelExpressionProvider);
        }

        private ViewContext CreateViewContext(ViewDataDictionary viewData = null)
        {
            if (viewData == null)
            {
                viewData = new ViewDataDictionary(MetadataProvider, new ModelStateDictionary());
            }

            var myService = new MyService();
            var htmlHelper = Mock.Of<IHtmlHelper<object>>();

            var serviceProvider = new ServiceCollection()
                .AddSingleton(myService)
                .AddSingleton(htmlHelper)
                .BuildServiceProvider();

            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider
            };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            return new ViewContext(
                actionContext,
                Mock.Of<IView>(),
                viewData,
                Mock.Of<ITempDataDictionary>(),
                TextWriter.Null,
                new HtmlHelperOptions());
        }

        private abstract class TestPageBase<TModel> : RazorPage<TModel>
        {
            [RazorInject]
            public MyService MyService { get; set; }

            public MyService MyService2 { get; set; }

            [RazorInject]
            public IJsonHelper Json { get; set; }

            [RazorInject]
            public IUrlHelper Url { get; set; }
        }

        private class TestRazorPage : TestPageBase<MyModel>
        {
            [RazorInject]
            internal IHtmlHelper<object> Html { get; private set; }

            public override Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }
        }

        private class ModelTypeProviderRazorPage : RazorPage, IModelTypeProvider
        {
            [RazorInject]
            public ViewDataDictionary ViewData { get; set; }

            public Type ModelType { get; set; } = typeof(Guid);

            public override Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }

            public Type GetModelType() => ModelType;
        }

        private abstract class NoModelPropertyBase<TModel> : RazorPage
        {
            [RazorInject]
            public ViewDataDictionary ViewData { get; set; }
        }

        private class NoModelPropertyPage : NoModelPropertyBase<MyModel>
        {
            public override Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }
        }

        private class HasIncorrectViewDataPropertyType : RazorPage<MyModel>
        {
            [RazorInject]
            public ViewDataDictionary<object> MoreViewData { get; set; }

            public override Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }
        }

        private class MyService : IViewContextAware
        {
            public ViewContext ViewContext { get; private set; }

            public void Contextualize(ViewContext viewContext)
            {
                ViewContext = viewContext;
            }
        }

        private class MyModel
        {
        }
    }
}