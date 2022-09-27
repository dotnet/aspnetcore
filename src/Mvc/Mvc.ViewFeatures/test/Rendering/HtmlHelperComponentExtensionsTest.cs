// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Rendering;

public class HtmlHelperComponentExtensionsTest
{
    [Fact]
    public async Task RenderComponentAsync_Works()
    {
        // Arrange
        var viewContext = GetViewContext();
        var htmlHelper = Mock.Of<IHtmlHelper>(h => h.ViewContext == viewContext);

        // Act
        var result = await HtmlHelperComponentExtensions.RenderComponentAsync<TestComponent>(htmlHelper, RenderMode.Static);

        // Assert
        Assert.Equal("Hello world", HtmlContentUtilities.HtmlContentToString(result));
    }

    private static ViewContext GetViewContext()
    {
        var htmlContent = new HtmlContentBuilder().AppendHtml("Hello world");
        var renderer = Mock.Of<IComponentRenderer>(c =>
            c.RenderComponentAsync(It.IsAny<ViewContext>(), It.IsAny<Type>(), It.IsAny<RenderMode>(), It.IsAny<object>()) == new ValueTask<IHtmlContent>(htmlContent));

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton<IComponentRenderer>(renderer).BuildServiceProvider(),
        };

        var viewContext = new ViewContext { HttpContext = httpContext };
        return viewContext;
    }

    private class TestComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle)
        {
        }

        public Task SetParametersAsync(ParameterView parameters) => null;
    }
}
