// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    public class DefaultTagHelperActivatorTest
    {
        [Theory]
        [InlineData("test", 100)]
        [InlineData(null, -1)]
        public void Activate_InitializesTagHelpers(string name, int number)
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new MvcCoreBuilder(services);
            builder.InitializeTagHelper<TestTagHelper>((h, vc) =>
            {
                h.Name = name;
                h.Number = number;
                h.ViewDataValue = vc.ViewData["TestData"];
            });
            var httpContext = MakeHttpContext(services.BuildServiceProvider());
            var viewContext = MakeViewContext(httpContext);
            var viewDataValue = new object();
            viewContext.ViewData.Add("TestData", viewDataValue);
            var activator = new DefaultTagHelperActivator();
            var helper = new TestTagHelper();

            // Act
            activator.Activate(helper, viewContext);

            // Assert
            Assert.Equal(name, helper.Name);
            Assert.Equal(number, helper.Number);
            Assert.Same(viewDataValue, helper.ViewDataValue);
        }

        [Fact]
        public void Activate_InitializesTagHelpersAfterActivatingProperties()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new MvcCoreBuilder(services);
            builder.InitializeTagHelper<TestTagHelper>((h, _) => h.ViewContext = MakeViewContext(MakeHttpContext()));
            var httpContext = MakeHttpContext(services.BuildServiceProvider());
            var viewContext = MakeViewContext(httpContext);
            var activator = new DefaultTagHelperActivator();
            var helper = new TestTagHelper();

            // Act
            activator.Activate(helper, viewContext);

            // Assert
            Assert.NotSame(viewContext, helper.ViewContext);
        }

        [Fact]
        public void Activate_InitializesTagHelpersWithMultipleInitializers()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new MvcCoreBuilder(services);
            builder.InitializeTagHelper<TestTagHelper>((h, vc) =>
            {
                h.Name = "Test 1";
                h.Number = 100;
            });
            builder.InitializeTagHelper<TestTagHelper>((h, vc) =>
            {
                h.Name += ", Test 2";
                h.Number += 100;
            });
            var httpContext = MakeHttpContext(services.BuildServiceProvider());
            var viewContext = MakeViewContext(httpContext);
            var activator = new DefaultTagHelperActivator();
            var helper = new TestTagHelper();

            // Act
            activator.Activate(helper, viewContext);

            // Assert
            Assert.Equal("Test 1, Test 2", helper.Name);
            Assert.Equal(200, helper.Number);
        }

        [Fact]
        public void Activate_InitializesTagHelpersWithCorrectInitializers()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new MvcCoreBuilder(services);
            builder.InitializeTagHelper<TestTagHelper>((h, vc) =>
            {
                h.Name = "Test 1";
                h.Number = 100;
            });
            builder.InitializeTagHelper<AnotherTestTagHelper>((h, vc) =>
            {
                h.Name = "Test 2";
                h.Number = 102;
            });
            var httpContext = MakeHttpContext(services.BuildServiceProvider());
            var viewContext = MakeViewContext(httpContext);
            var activator = new DefaultTagHelperActivator();
            var testTagHelper = new TestTagHelper();
            var anotherTestTagHelper = new AnotherTestTagHelper();

            // Act
            activator.Activate(testTagHelper, viewContext);
            activator.Activate(anotherTestTagHelper, viewContext);

            // Assert
            Assert.Equal("Test 1", testTagHelper.Name);
            Assert.Equal(100, testTagHelper.Number);
            Assert.Equal("Test 2", anotherTestTagHelper.Name);
            Assert.Equal(102, anotherTestTagHelper.Number);
        }

        private static HttpContext MakeHttpContext(IServiceProvider services = null)
        {
            var httpContext = new DefaultHttpContext();
            if (services != null)
            {
                httpContext.RequestServices = services;
            }
            return httpContext;
        }

        private static ViewContext MakeViewContext(HttpContext httpContext)
        {
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var metadataProvider = new EmptyModelMetadataProvider();
            var viewData = new ViewDataDictionary(metadataProvider);
            var viewContext = new ViewContext(
                actionContext,
                Mock.Of<IView>(),
                viewData,
                Mock.Of<ITempDataDictionary>(),
                TextWriter.Null,
                new HtmlHelperOptions());

            return viewContext;
        }

        private class TestTagHelper : TagHelper
        {
            public string Name { get; set; } = "Initial Name";

            public int Number { get; set; } = 1000;

            public object ViewDataValue { get; set; } = new object();

            [ViewContext]
            public ViewContext ViewContext { get; set; }
        }

        private class AnotherTestTagHelper : TagHelper
        {
            public string Name { get; set; } = "Initial Name";

            public int Number { get; set; } = 1000;

            public object ViewDataValue { get; set; } = new object();

            [ViewContext]
            public ViewContext ViewContext { get; set; }
        }
    }
}
