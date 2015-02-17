// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.WebEncoders;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
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
            var htmlEncoder = new HtmlEncoder();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(p => p.GetService(typeof(MyService)))
                           .Returns(myService);
            serviceProvider.Setup(p => p.GetService(typeof(IHtmlHelper<object>)))
                           .Returns(helper);
            serviceProvider.Setup(p => p.GetService(typeof(IHtmlEncoder)))
                           .Returns(htmlEncoder);
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.RequestServices)
                       .Returns(serviceProvider.Object);

            var actionContext = new ActionContext(httpContext.Object, new RouteData(), new ActionDescriptor());
            var viewContext = new ViewContext(actionContext,
                                              Mock.Of<IView>(),
                                              new ViewDataDictionary(new EmptyModelMetadataProvider()),
                                              TextWriter.Null);

            // Act
            activator.Activate(instance, viewContext);

            // Assert
            Assert.Same(helper, instance.Html);
            Assert.Same(myService, instance.MyService);
            Assert.Same(viewContext, myService.ViewContext);
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
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.RequestServices)
                       .Returns(serviceProvider.Object);

            var actionContext = new ActionContext(httpContext.Object, new RouteData(), new ActionDescriptor());
            var viewContext = new ViewContext(actionContext,
                                              Mock.Of<IView>(),
                                              new ViewDataDictionary(new EmptyModelMetadataProvider()),
                                              TextWriter.Null);

            // Act and Assert
            var ex = Assert.Throws<InvalidOperationException>(() => activator.Activate(instance, viewContext));
            var message = string.Format(CultureInfo.InvariantCulture,
                                        "View of type '{0}' cannot be activated by '{1}'.",
                                        instance.GetType().FullName,
                                        typeof(RazorPageActivator).FullName);

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
            var htmlEncoder = new HtmlEncoder();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(p => p.GetService(typeof(MyService)))
                           .Returns(myService);
            serviceProvider.Setup(p => p.GetService(typeof(IHtmlHelper<object>)))
                           .Returns(helper);
            serviceProvider.Setup(p => p.GetService(typeof(IHtmlEncoder)))
                           .Returns(htmlEncoder);
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.RequestServices)
                       .Returns(serviceProvider.Object);

            var actionContext = new ActionContext(httpContext.Object, new RouteData(), new ActionDescriptor());
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider())
            {
                Model = new MyModel()
            };
            var viewContext = new ViewContext(actionContext,
                                              Mock.Of<IView>(),
                                              viewData,
                                              TextWriter.Null);

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
            var htmlEncoder = new HtmlEncoder();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(p => p.GetService(typeof(MyService)))
                           .Returns(myService);
            serviceProvider.Setup(p => p.GetService(typeof(IHtmlHelper<object>)))
                           .Returns(helper);
            serviceProvider.Setup(p => p.GetService(typeof(IHtmlEncoder)))
                           .Returns(htmlEncoder);
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.RequestServices)
                       .Returns(serviceProvider.Object);

            var actionContext = new ActionContext(httpContext.Object, new RouteData(), new ActionDescriptor());
            var viewData = new ViewDataDictionary<MyModel>(new EmptyModelMetadataProvider())
            {
                Model = new MyModel()
            };
            var viewContext = new ViewContext(actionContext,
                                              Mock.Of<IView>(),
                                              viewData,
                                              TextWriter.Null);

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
            var htmlEncoder = new HtmlEncoder();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(p => p.GetService(typeof(MyService)))
                           .Returns(myService);
            serviceProvider.Setup(p => p.GetService(typeof(IHtmlHelper<object>)))
                           .Returns(helper);
            serviceProvider.Setup(p => p.GetService(typeof(IHtmlEncoder)))
                           .Returns(htmlEncoder);
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.RequestServices)
                       .Returns(serviceProvider.Object);

            var actionContext = new ActionContext(httpContext.Object, new RouteData(), new ActionDescriptor());
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
            var viewContext = new ViewContext(actionContext,
                                              Mock.Of<IView>(),
                                              viewData,
                                              TextWriter.Null);

            // Act
            activator.Activate(instance, viewContext);

            // Assert
            Assert.IsType<ViewDataDictionary<string>>(viewContext.ViewData);
        }

        private abstract class TestPageBase<TModel> : RazorPage<TModel>
        {
            [Activate]
            public MyService MyService { get; set; }

            public MyService MyService2 { get; set; }
        }

        private class TestRazorPage : TestPageBase<MyModel>
        {
            [Activate]
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