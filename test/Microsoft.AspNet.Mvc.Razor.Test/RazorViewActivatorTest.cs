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
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorViewActivatorTest
    {
        [Fact]
        public void Activate_ActivatesAndContextualizesPropertiesOnViews()
        {
            // Arrange
            var activator = new RazorViewActivator(Mock.Of<ITypeActivator>());
            var instance = new TestView();

            var myService = new MyService();
            var helper = Mock.Of<IHtmlHelper<object>>();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(p => p.GetService(typeof(MyService)))
                           .Returns(myService);
            serviceProvider.Setup(p => p.GetService(typeof(IHtmlHelper<object>)))
                           .Returns(helper);
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.RequestServices)
                       .Returns(serviceProvider.Object);
            var routeContext = new RouteContext(httpContext.Object);
            var actionContext = new ActionContext(routeContext, new ActionDescriptor());
            var viewContext = new ViewContext(actionContext,
                                              instance,
                                              new ViewDataDictionary(Mock.Of<IModelMetadataProvider>()),
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
            var activator = new RazorViewActivator(Mock.Of<ITypeActivator>());
            var instance = new DoesNotDeriveFromRazorViewOfT();

            var myService = new MyService();
            var helper = Mock.Of<IHtmlHelper<object>>();
            var serviceProvider = new Mock<IServiceProvider>();
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.RequestServices)
                       .Returns(serviceProvider.Object);
            var routeContext = new RouteContext(httpContext.Object);
            var actionContext = new ActionContext(routeContext, new ActionDescriptor());
            var viewContext = new ViewContext(actionContext,
                                              instance,
                                              new ViewDataDictionary(Mock.Of<IModelMetadataProvider>()),
                                              TextWriter.Null);

            // Act and Assert
            var ex = Assert.Throws<InvalidOperationException>(() => activator.Activate(instance, viewContext));
            var message = string.Format(CultureInfo.InvariantCulture,
                                        "View of type '{0}' cannot be activated by '{1}'.",
                                        instance.GetType().FullName,
                                        typeof(RazorViewActivator).FullName);

            Assert.Equal(message, ex.Message);
        }

        [Fact]
        public void Activate_InstantiatesNewViewDataDictionaryType_IfTheTypeDoesNotMatch()
        {
            // Arrange
            var typeActivator = new TypeActivator();
            var activator = new RazorViewActivator(typeActivator);
            var instance = new TestView();

            var myService = new MyService();
            var helper = Mock.Of<IHtmlHelper<object>>();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(p => p.GetService(typeof(MyService)))
                           .Returns(myService);
            serviceProvider.Setup(p => p.GetService(typeof(IHtmlHelper<object>)))
                           .Returns(helper);
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.RequestServices)
                       .Returns(serviceProvider.Object);
            var routeContext = new RouteContext(httpContext.Object);
            var actionContext = new ActionContext(routeContext, new ActionDescriptor());
            var viewData = new ViewDataDictionary(Mock.Of<IModelMetadataProvider>())
            {
                Model = new MyModel()
            };
            var viewContext = new ViewContext(actionContext,
                                              instance,
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
            var typeActivator = new TypeActivator();
            var activator = new RazorViewActivator(typeActivator);
            var instance = new TestView();
            var myService = new MyService();
            var helper = Mock.Of<IHtmlHelper<object>>();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(p => p.GetService(typeof(MyService)))
                           .Returns(myService);
            serviceProvider.Setup(p => p.GetService(typeof(IHtmlHelper<object>)))
                           .Returns(helper);
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.RequestServices)
                       .Returns(serviceProvider.Object);
            var routeContext = new RouteContext(httpContext.Object);
            var actionContext = new ActionContext(routeContext, new ActionDescriptor());
            var viewData = new ViewDataDictionary<MyModel>(Mock.Of<IModelMetadataProvider>())
            {
                Model = new MyModel()
            };
            var viewContext = new ViewContext(actionContext,
                                              instance,
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
            var typeActivator = new TypeActivator();
            var activator = new RazorViewActivator(typeActivator);
            var instance = new DoesNotDeriveFromRazorViewOfTButHasModelProperty();
            var myService = new MyService();
            var helper = Mock.Of<IHtmlHelper<object>>();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(p => p.GetService(typeof(MyService)))
                           .Returns(myService);
            serviceProvider.Setup(p => p.GetService(typeof(IHtmlHelper<object>)))
                           .Returns(helper);
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.RequestServices)
                       .Returns(serviceProvider.Object);
            var routeContext = new RouteContext(httpContext.Object);
            var actionContext = new ActionContext(routeContext, new ActionDescriptor());
            var viewData = new ViewDataDictionary(Mock.Of<IModelMetadataProvider>());
            var viewContext = new ViewContext(actionContext,
                                              instance,
                                              viewData,
                                              TextWriter.Null);

            // Act
            activator.Activate(instance, viewContext);

            // Assert
            Assert.IsType<ViewDataDictionary<string>>(viewContext.ViewData);
        }

        private abstract class TestViewBase<TModel> : RazorView<TModel>
        {
            [Activate]
            public MyService MyService { get; set; }

            public MyService MyService2 { get; set; }
        }

        private class TestView : TestViewBase<MyModel>
        {
            [Activate]
            internal IHtmlHelper<object> Html { get; private set; }

            public override Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }
        }

        private abstract class DoesNotDeriveFromRazorViewOfTBase<TModel> : RazorView
        {
        }

        private class DoesNotDeriveFromRazorViewOfT : DoesNotDeriveFromRazorViewOfTBase<MyModel>
        {
            public override Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }
        }

        private class DoesNotDeriveFromRazorViewOfTButHasModelProperty : DoesNotDeriveFromRazorViewOfTBase<MyModel>
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