// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

public class ComponentTagHelperTest
{
    [Fact]
    public async Task ProcessAsync_RendersComponent()
    {
        // Arrange
        var tagHelper = new ComponentTagHelper
        {
            ViewContext = GetViewContext(),
            RenderMode = RenderMode.Static,
        };
        var context = GetTagHelperContext();
        var output = GetTagHelperOutput();

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        var content = HtmlContentUtilities.HtmlContentToString(output.Content);
        Assert.Equal("Hello world", content);
        Assert.Null(output.TagName);
    }

    [Fact]
    public async Task ProcessAsync_WithoutSpecifyingRenderMode_ThrowsError()
    {
        // Arrange
        var tagHelper = new ComponentTagHelper
        {
            ViewContext = GetViewContext(),
        };
        var context = GetTagHelperContext();
        var output = GetTagHelperOutput();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => tagHelper.ProcessAsync(context, output));
        Assert.Equal("A value for the 'render-mode' attribute must be supplied to the 'component' tag helper.", ex.Message);
    }

    private static TagHelperContext GetTagHelperContext()
    {
        return new TagHelperContext(
            "component",
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString("N"));
    }

    private static TagHelperOutput GetTagHelperOutput()
    {
        return new TagHelperOutput(
            "component",
            new TagHelperAttributeList(),
            (_, __) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
    }

    private ViewContext GetViewContext()
    {
        var htmlContent = new HtmlContentBuilder().AppendHtml("Hello world");
        var renderer = Mock.Of<IComponentRenderer>(c =>
            c.RenderComponentAsync(It.IsAny<ViewContext>(), It.IsAny<Type>(), It.IsAny<RenderMode>(), It.IsAny<object>()) == new ValueTask<IHtmlContent>(htmlContent));

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddSingleton<IComponentRenderer>(renderer)
                .AddSingleton<HtmlRenderer>()
                .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
                .AddSingleton<HtmlEncoder>(HtmlEncoder.Default)
                .BuildServiceProvider(),
        };

        return new ViewContext
        {
            HttpContext = httpContext,
        };
    }
}
