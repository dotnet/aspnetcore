// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    public class RazorPageActivatorTest
    {
        [Fact]
        public void Activate_ActivatesAndContextualizesPropertiesOnViews()
        {
            // Arrange
            var activator = new RazorPageActivator(new EmptyModelMetadataProvider());
            var instance = new TestRazorPage();

            var myService = new MyService();
            var helper = Mock.Of<IHtmlHelper<object>>();
            var htmlEncoder = new HtmlTestEncoder();
            var diagnosticSource = new DiagnosticListener("Microsoft.AspNetCore");
            var serviceProvider = new ServiceCollection()
                .AddSingleton(myService)
                .AddSingleton(helper)
                .AddSingleton<HtmlEncoder>(htmlEncoder)
                .AddSingleton<DiagnosticSource>(diagnosticSource)
                .BuildServiceProvider();
            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider
            };

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var viewContext = new ViewContext(
                actionContext,
                Mock.Of<IView>(),
                new ViewDataDictionary(new EmptyModelMetadataProvider()),
                Mock.Of<ITempDataDictionary>(),
                TextWriter.Null,
                new HtmlHelperOptions());

            // Act
            activator.Activate(instance, viewContext);

            // Assert
            Assert.Same(helper, instance.Html);
            Assert.Same(myService, instance.MyService);
            Assert.Same(viewContext, myService.ViewContext);
            Assert.Same(diagnosticSource, instance.DiagnosticSource);
            Assert.Null(instance.MyService2);
        }

        [Fact]
        public void Activate_ThrowsIfTheViewDoesNotDeriveFromRazorViewOfT()
        {
            // Arrange
            var activator = new RazorPageActivator(new EmptyModelMetadataProvider());
            var instance = new DoesNotDeriveFromRazorPageOfT();

            var myService = new MyService();
            var helper = Mock.Of<IHtmlHelper<object>>();
            var serviceProvider = new Mock<IServiceProvider>();
            var httpContext = new DefaultHttpContext
            {
                RequestServices = new ServiceCollection().BuildServiceProvider()
            };

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var viewContext = new ViewContext(
                actionContext,
                Mock.Of<IView>(),
                new ViewDataDictionary(new EmptyModelMetadataProvider()),
                Mock.Of<ITempDataDictionary>(),
                TextWriter.Null,
                new HtmlHelperOptions());

            // Act and Assert
            var ex = Assert.Throws<InvalidOperationException>(() => activator.Activate(instance, viewContext));
            var message = $"View of type '{instance.GetType()}' cannot be activated by '{typeof(RazorPageActivator)}'.";
            Assert.Equal(message, ex.Message);
        }

        [Fact]
        public void Activate_InstantiatesNewViewDataDictionaryType_IfTheTypeDoesNotMatch()
        {
            // Arrange
            var activator = new RazorPageActivator(new EmptyModelMetadataProvider());
            var instance = new TestRazorPage();

            var myService = new MyService();
            var helper = Mock.Of<IHtmlHelper<object>>();
            var htmlEncoder = new HtmlTestEncoder();
            var serviceProvider = new ServiceCollection()
                .AddSingleton(myService)
                .AddSingleton(helper)
                .AddSingleton<HtmlEncoder>(htmlEncoder)
                .AddSingleton<DiagnosticSource>(new DiagnosticListener("Microsoft.Aspnet.Mvc"))
                .BuildServiceProvider();
            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider
            };

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var viewData = new ViewDataDictionary<object>(new EmptyModelMetadataProvider())
            {
                Model = new MyModel()
            };
            var viewContext = new ViewContext(
                actionContext,
                Mock.Of<IView>(),
                viewData,
                Mock.Of<ITempDataDictionary>(),
                TextWriter.Null,
                new HtmlHelperOptions());

            // Act
            activator.Activate(instance, viewContext);

            // Assert
            Assert.IsType<ViewDataDictionary<MyModel>>(viewContext.ViewData);
        }

        [Fact]
        public void Activate_UsesPassedInViewDataDictionaryInstance_IfPassedInTypeMatches()
        {
            // Arrange
            var activator = new RazorPageActivator(new EmptyModelMetadataProvider());
            var instance = new TestRazorPage();
            var myService = new MyService();
            var helper = Mock.Of<IHtmlHelper<object>>();
            var htmlEncoder = new HtmlTestEncoder();
            var serviceProvider = new ServiceCollection()
                .AddSingleton(myService)
                .AddSingleton(helper)
                .AddSingleton<HtmlEncoder>(htmlEncoder)
                .AddSingleton<DiagnosticSource>(new DiagnosticListener("Microsoft.Aspnet.Mvc"))
                .BuildServiceProvider();
            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider
            };

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var viewData = new ViewDataDictionary<MyModel>(new EmptyModelMetadataProvider())
            {
                Model = new MyModel()
            };
            var viewContext = new ViewContext(
                actionContext,
                Mock.Of<IView>(),
                viewData,
                Mock.Of<ITempDataDictionary>(),
                TextWriter.Null,
                new HtmlHelperOptions());

            // Act
            activator.Activate(instance, viewContext);

            // Assert
            Assert.Same(viewData, viewContext.ViewData);
        }

        [Fact]
        public void Activate_DeterminesModelTypeFromProperty()
        {
            // Arrange
            var activator = new RazorPageActivator(new EmptyModelMetadataProvider());
            var instance = new DoesNotDeriveFromRazorPageOfTButHasModelProperty();
            var myService = new MyService();
            var helper = Mock.Of<IHtmlHelper<object>>();
            var htmlEncoder = new HtmlTestEncoder();
            var serviceProvider = new ServiceCollection()
                .AddSingleton(myService)
                .AddSingleton(helper)
                .AddSingleton<HtmlEncoder>(htmlEncoder)
                .AddSingleton<DiagnosticSource>(new DiagnosticListener("Microsoft.AspNetCore.Mvc"))
                .BuildServiceProvider();
            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider
            };

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var viewData = new ViewDataDictionary<object>(new EmptyModelMetadataProvider());
            var viewContext = new ViewContext(
                actionContext,
                Mock.Of<IView>(),
                viewData,
                Mock.Of<ITempDataDictionary>(),
                TextWriter.Null,
                new HtmlHelperOptions());

            // Act
            activator.Activate(instance, viewContext);

            // Assert
            Assert.IsType<ViewDataDictionary<string>>(viewContext.ViewData);
        }

        private abstract class TestPageBase<TModel> : RazorPage<TModel>
        {
            [RazorInject]
            public MyService MyService { get; set; }

            public MyService MyService2 { get; set; }
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

        private abstract class DoesNotDeriveFromRazorPageOfTBase<TModel> : RazorPage
        {
        }

        private class DoesNotDeriveFromRazorPageOfT : DoesNotDeriveFromRazorPageOfTBase<MyModel>
        {
            public override Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }
        }

        private class DoesNotDeriveFromRazorPageOfTButHasModelProperty : DoesNotDeriveFromRazorPageOfTBase<MyModel>
        {
            public string Model { get; set; }

            public override Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }
        }

        private class MyService : ICanHasViewContext
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