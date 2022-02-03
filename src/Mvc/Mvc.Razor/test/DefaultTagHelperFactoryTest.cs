// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Razor;

public class DefaultTagHelperFactoryTest
{
    [Theory]
    [InlineData("test", 100)]
    [InlineData(null, -1)]
    public void CreateTagHelper_InitializesTagHelpers(string name, int number)
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new MvcCoreBuilder(services, new ApplicationPartManager());
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
        var factory = CreateFactory();

        // Act
        var helper = factory.CreateTagHelper<TestTagHelper>(viewContext);

        // Assert
        Assert.Equal(name, helper.Name);
        Assert.Equal(number, helper.Number);
        Assert.Same(viewDataValue, helper.ViewDataValue);
    }

    [Fact]
    public void CreateTagHelper_InitializesTagHelpersAfterActivatingProperties()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new MvcCoreBuilder(services, new ApplicationPartManager());
        builder.InitializeTagHelper<TestTagHelper>((h, _) => h.ViewContext = MakeViewContext(MakeHttpContext()));
        var httpContext = MakeHttpContext(services.BuildServiceProvider());
        var viewContext = MakeViewContext(httpContext);
        var factory = CreateFactory();

        // Act
        var helper = factory.CreateTagHelper<TestTagHelper>(viewContext);

        // Assert
        Assert.NotSame(viewContext, helper.ViewContext);
    }

    [Fact]
    public void CreateTagHelper_InitializesTagHelpersWithMultipleInitializers()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new MvcCoreBuilder(services, new ApplicationPartManager());
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
        var factory = CreateFactory();

        // Act
        var helper = factory.CreateTagHelper<TestTagHelper>(viewContext);

        // Assert
        Assert.Equal("Test 1, Test 2", helper.Name);
        Assert.Equal(200, helper.Number);
    }

    [Fact]
    public void CreateTagHelper_InitializesTagHelpersWithCorrectInitializers()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new MvcCoreBuilder(services, new ApplicationPartManager());
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

        var activator = new Mock<ITagHelperActivator>();
        activator
            .Setup(a => a.Create<TestTagHelper>(It.IsAny<ViewContext>()))
            .Returns(new TestTagHelper());

        activator
            .Setup(a => a.Create<AnotherTestTagHelper>(It.IsAny<ViewContext>()))
            .Returns(new AnotherTestTagHelper());

        var factory = new DefaultTagHelperFactory(activator.Object);

        // Act
        var testTagHelper = factory.CreateTagHelper<TestTagHelper>(viewContext);
        var anotherTestTagHelper = factory.CreateTagHelper<AnotherTestTagHelper>(viewContext);

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

    private static DefaultTagHelperFactory CreateFactory()
    {
        var activator = new Mock<ITagHelperActivator>();
        activator.Setup(a => a.Create<TestTagHelper>(It.IsAny<ViewContext>())).Returns(new TestTagHelper());
        return new DefaultTagHelperFactory(activator.Object);
    }

    private static ViewContext MakeViewContext(HttpContext httpContext)
    {
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var metadataProvider = new EmptyModelMetadataProvider();
        var viewData = new ViewDataDictionary(metadataProvider, new ModelStateDictionary());
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
