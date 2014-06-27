// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Routing;
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
            var activator = new RazorViewActivator();
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

        private abstract class TestViewBase : RazorView
        {
            [Activate]
            public MyService MyService { get; set; }

            public MyService MyService2 { get; set; }
        }

        private class TestView : TestViewBase
        {
            [Activate]
            internal IHtmlHelper<object> Html { get; private set; }

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
    }
}