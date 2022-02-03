// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Razor.Infrastructure;

public class DefaultTagHelperActivatorTest
{
    [Fact]
    public void CreateTagHelper_InitializesTagHelpers()
    {
        // Arrange
        var httpContext = new DefaultHttpContext()
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        var viewContext = MakeViewContext(httpContext);
        var viewDataValue = new object();
        viewContext.ViewData.Add("TestData", viewDataValue);
        var activator = new DefaultTagHelperActivator();

        // Act
        var helper = activator.Create<TestTagHelper>(viewContext);

        // Assert
        Assert.NotNull(helper);
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
