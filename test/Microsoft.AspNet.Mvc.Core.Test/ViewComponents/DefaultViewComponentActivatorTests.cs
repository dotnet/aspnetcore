// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ViewComponents
{
    public class DefaultViewComponentActivatorTests
    {
        [Fact]
        public void DefaultViewComponentActivatorSetsAllPropertiesMarkedAsActivate()
        {
            // Arrange
            var activator = new DefaultViewComponentActivator();
            var instance = new TestViewComponent();
            var helper = Mock.Of<IHtmlHelper<object>>();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(p => p.GetService(typeof(IHtmlHelper<object>))).Returns(helper);
            serviceProvider.Setup(p => p.GetService(typeof(ICompositeViewEngine))).Returns(Mock.Of<ICompositeViewEngine>());
            serviceProvider.Setup(p => p.GetService(typeof(IUrlHelper))).Returns(Mock.Of<IUrlHelper>());
            var viewContext = GetViewContext(serviceProvider.Object);

            // Act
            activator.Activate(instance, viewContext);

            // Assert
            Assert.Same(helper, instance.Html);
            Assert.Same(viewContext, instance.ViewContext);
            Assert.IsType<ViewDataDictionary>(instance.ViewData);
        }

        [Fact]
        public void DefaultViewComponentActivatorSetsModelAsNull()
        {
            // Arrange
            var activator = new DefaultViewComponentActivator();
            var helper = Mock.Of<IHtmlHelper<object>>();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(p => p.GetService(typeof(IHtmlHelper<object>))).Returns(helper);
            serviceProvider.Setup(p => p.GetService(typeof(ICompositeViewEngine))).Returns(Mock.Of<ICompositeViewEngine>());
            serviceProvider.Setup(p => p.GetService(typeof(IUrlHelper))).Returns(Mock.Of<IUrlHelper>());
            var viewContext = GetViewContext(serviceProvider.Object);

            // Act
            activator.Activate(new TestViewComponent(), viewContext);

            // Assert
            Assert.Null(viewContext.ViewData.Model);
        }

        [Fact]
        public void DefaultViewComponentActivatorActivatesNonBuiltInTypes()
        {
            // Arrange
            var activator = new DefaultViewComponentActivator();
            var helper = Mock.Of<IHtmlHelper<object>>();
            var myTestService = new MyService();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(p => p.GetService(typeof(IHtmlHelper<object>))).Returns(helper);
            serviceProvider.Setup(p => p.GetService(typeof(MyService))).Returns(myTestService);
            serviceProvider.Setup(p => p.GetService(typeof(ICompositeViewEngine))).Returns(Mock.Of<ICompositeViewEngine>());
            serviceProvider.Setup(p => p.GetService(typeof(IUrlHelper))).Returns(Mock.Of<IUrlHelper>());
            var viewContext = GetViewContext(serviceProvider.Object);
            var instance = new TestViewComponentWithCustomDataType();

            // Act
            activator.Activate(instance, viewContext);

            // Assert
            Assert.Equal(myTestService, instance.TestMyServiceObject);

        }

        [Fact]
        public void DefaulViewComponentActivatorContextualizesService()
        {
            // Arrange
            var activator = new DefaultViewComponentActivator();
            var instance = new TestClassUsingMyService();
            var myTestService = new MyService();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(p => p.GetService(typeof(MyService))).Returns(myTestService);
            var viewContext = GetViewContext(serviceProvider.Object);

            // Act
            activator.Activate(instance, viewContext);

            // Assert
            Assert.Same(myTestService, instance.MyTestService);
            Assert.Same(viewContext, instance.MyTestService.ViewContext);
        }

        private ViewContext GetViewContext(IServiceProvider serviceProvider)
        {
            var httpContext = new Mock<DefaultHttpContext>();
            httpContext.SetupGet(c => c.RequestServices)
                       .Returns(serviceProvider);

            var actionContext = new ActionContext(httpContext.Object, new RouteData(), new ActionDescriptor());
            return new ViewContext(actionContext,
                                              Mock.Of<IView>(),
                                              new ViewDataDictionary(new EmptyModelMetadataProvider()),
                                              null,
                                              TextWriter.Null);
        }

        private class TestViewComponent : ViewComponent
        {
            [Activate]
            public IHtmlHelper<object> Html { get; private set; }

            public Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }
        }

        private class TestViewComponentWithCustomDataType : TestViewComponent
        {
            [Activate]
            public MyService TestMyServiceObject { get; set; }
        }

        private class MyService : ICanHasViewContext
        {
            public ViewContext ViewContext { get; private set; }

            public void Contextualize(ViewContext viewContext)
            {
                ViewContext = viewContext;
            }
        }

        private class TestClassUsingMyService
        {
            [Activate]
            public MyService MyTestService { get; set; }
        }
    }
}
#endif
