// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
        public void CreateTagHelper_ProvidesTagHelperWithViewData()
        {
            // Arrange
            var instance = CreateTestRazorPage();

            // Act
            var tagHelper = instance.CreateTagHelper<ViewDataTagHelper>();

            // Assert
            Assert.NotNull(tagHelper.ViewData);
        }

        [Fact]
        public void CreateTagHelper_ProvidesTagHelperWithInternalProperties()
        {
            // Arrange
            var instance = CreateTestRazorPage();

            // Act
            var tagHelper = instance.CreateTagHelper<TagHelperWithInternalProperty>();

            // Assert
            Assert.NotNull(tagHelper.ViewData);
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
            var activator = new RazorPageActivator(new EmptyModelMetadataProvider());
            var serviceProvider = new Mock<IServiceProvider>();
            var myService = new MyService();
            serviceProvider.Setup(mock => mock.GetService(typeof(MyService)))
                           .Returns(myService);
            serviceProvider.Setup(mock => mock.GetService(typeof(ITagHelperActivator)))
                           .Returns(new DefaultTagHelperActivator());
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.RequestServices)
                       .Returns(serviceProvider.Object);

            var actionContext = new ActionContext(httpContext.Object, new RouteData(), new ActionDescriptor());
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
            var viewContext = new ViewContext(actionContext,
                                              Mock.Of<IView>(),
                                              viewData,
                                              Mock.Of<ITempDataDictionary>(),
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

        private class ViewDataTagHelper : TagHelper
        {
            [Activate]
            public ViewDataDictionary ViewData { get; set; }
        }

        private class ViewContextServiceTagHelper : ViewContextTagHelper
        {
            [Activate]
            public MyService ActivatedService { get; set; }
        }

        private class TagHelperWithInternalProperty : TagHelper
        {
            [Activate]
            protected internal ViewDataDictionary ViewData { get; set; }

            [Activate]
            protected internal ViewContext ViewContext { get; set; }
        }

        private class MyService
        {
        }
    }
}