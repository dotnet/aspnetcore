// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorPageCreateTagHelperTest
    {
        [Fact]
        public void CreateTagHelper_CreatesProvidedTagHelperType()
        {
            // Arrange
            var instance = CreateTestRazorPage();

            // Act
            var tagHelper = instance.CreateTagHelper<NoServiceTagHelper>();

            // Assert
            Assert.NotNull(tagHelper);
        }

        [Fact]
        public void CreateTagHelper_ActivatesProvidedTagHelperType()
        {
            // Arrange
            var instance = CreateTestRazorPage();

            // Act
            var tagHelper = instance.CreateTagHelper<ServiceTagHelper>();

            // Assert
            Assert.NotNull(tagHelper.ActivatedService);
        }

        [Fact]
        public void CreateTagHelper_ProvidesTagHelperTypeWithViewContext()
        {
            // Arrange
            var instance = CreateTestRazorPage();

            // Act
            var tagHelper = instance.CreateTagHelper<ViewContextTagHelper>();

            // Assert
            Assert.NotNull(tagHelper.ViewContext);
        }

        [Fact]
        public void CreateTagHelper_ProvidesTagHelperTypeWithViewContextAndActivates()
        {
            // Arrange
            var instance = CreateTestRazorPage();

            // Act
            var tagHelper = instance.CreateTagHelper<ViewContextServiceTagHelper>();

            // Assert
            Assert.NotNull(tagHelper.ViewContext);
            Assert.NotNull(tagHelper.ActivatedService);
        }

        private static TestRazorPage CreateTestRazorPage()
        {
            var typeActivator = new TypeActivator();
            var activator = new RazorPageActivator(typeActivator);
            var serviceProvider = new Mock<IServiceProvider>();
            var myService = new MyService();
            serviceProvider.Setup(mock => mock.GetService(typeof(MyService)))
                           .Returns(myService);
            serviceProvider.Setup(mock => mock.GetService(typeof(ITypeActivator)))
                           .Returns(typeActivator);
            serviceProvider.Setup(mock => mock.GetService(typeof(ITagHelperActivator)))
                           .Returns(new DefaultTagHelperActivator());
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.RequestServices)
                       .Returns(serviceProvider.Object);
            var routeContext = new RouteContext(httpContext.Object);
            var actionContext = new ActionContext(routeContext, new ActionDescriptor());
            var viewData = new ViewDataDictionary(Mock.Of<IModelMetadataProvider>());
            var viewContext = new ViewContext(actionContext,
                                              Mock.Of<IView>(),
                                              viewData,
                                              TextWriter.Null);

            return new TestRazorPage
            {
                ViewContext = viewContext
            };
        }

        private class TestRazorPage : RazorPage<dynamic>
        {
            public override Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }
        }

        private class NoServiceTagHelper : TagHelper
        {
        }

        private class ServiceTagHelper : TagHelper
        {
            [Activate]
            public MyService ActivatedService { get; set; }
        }

        private class ViewContextTagHelper : TagHelper
        {
            [Activate]
            public ViewContext ViewContext { get; set; }
        }

        private class ViewContextServiceTagHelper : ViewContextTagHelper
        {
            [Activate]
            public MyService ActivatedService { get; set; }
        }

        private class MyService
        {
        }
    }
}