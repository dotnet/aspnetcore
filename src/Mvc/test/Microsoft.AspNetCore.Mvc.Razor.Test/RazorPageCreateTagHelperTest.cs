// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor
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

        private static TestRazorPage CreateTestRazorPage()
        {
            var modelMetadataProvider = new EmptyModelMetadataProvider();
            var modelExpressionProvider = new ModelExpressionProvider(modelMetadataProvider);
            var activator = new RazorPageActivator(
                modelMetadataProvider,
                new UrlHelperFactory(),
                Mock.Of<IJsonHelper>(),
                new DiagnosticListener("Microsoft.AspNetCore"),
                new HtmlTestEncoder(),
                modelExpressionProvider);

            var serviceProvider = new Mock<IServiceProvider>();
            var typeActivator = new TypeActivatorCache();
            var tagHelperActivator = new DefaultTagHelperActivator(typeActivator);
            var myService = new MyService();
            serviceProvider.Setup(mock => mock.GetService(typeof(MyService)))
                           .Returns(myService);
            serviceProvider.Setup(mock => mock.GetService(typeof(ITagHelperFactory)))
                .Returns(new DefaultTagHelperFactory(tagHelperActivator));
            serviceProvider.Setup(mock => mock.GetService(typeof(ITagHelperActivator)))
                           .Returns(tagHelperActivator);
            serviceProvider.Setup(mock => mock.GetService(typeof(ITypeActivatorCache)))
                           .Returns(typeActivator);
            serviceProvider.Setup(mock => mock.GetService(It.Is<Type>(serviceType =>
                serviceType.GetTypeInfo().IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))))
                .Returns<Type>(serviceType =>
                {
                    var enumerableType = serviceType.GetGenericArguments().First();
                    return typeof(Enumerable).GetMethod("Empty").MakeGenericMethod(enumerableType).Invoke(null, null);
                });
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.RequestServices)
                       .Returns(serviceProvider.Object);

            var actionContext = new ActionContext(httpContext.Object, new RouteData(), new ActionDescriptor());
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
            var viewContext = new ViewContext(
                actionContext,
                Mock.Of<IView>(),
                viewData,
                Mock.Of<ITempDataDictionary>(),
                TextWriter.Null,
                new HtmlHelperOptions());

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
            public ServiceTagHelper(MyService service)
            {
                ActivatedService = service;
            }

            [HtmlAttributeNotBound]
            public MyService ActivatedService { get; }
        }

        private class ViewContextTagHelper : TagHelper
        {
            [ViewContext]
            public ViewContext ViewContext { get; set; }
        }

        private class MyService
        {
        }
    }
}
