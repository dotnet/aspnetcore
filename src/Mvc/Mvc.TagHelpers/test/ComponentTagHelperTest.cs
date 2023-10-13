// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

public class ComponentTagHelperTest
{
    [Fact]
    public async Task ProcessAsync_RendersComponent()
    {
        // Arrange
        var viewContext = GetViewContext();
        var tagHelper = new ComponentTagHelper
        {
            ViewContext = viewContext,
            RenderMode = Rendering.RenderMode.Static,
            ComponentType = typeof(TestComponent),
        };
        var context = GetTagHelperContext();
        var output = GetTagHelperOutput();

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        var prerenderer = viewContext.HttpContext.RequestServices.GetRequiredService<IComponentPrerenderer>();
        var content = await prerenderer.Dispatcher.InvokeAsync(() => HtmlContentUtilities.HtmlContentToString(output.Content));
        Assert.Equal("Hello from the component", content);
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
        var navManager = new Mock<NavigationManager>();
        navManager.As<IHostEnvironmentNavigationManager>();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddScoped<IComponentPrerenderer, EndpointHtmlRenderer>()
                .AddScoped<ServerComponentSerializer>()
                .AddScoped(_ => Mock.Of<IDataProtectionProvider>(
                    x => x.CreateProtector(It.IsAny<string>()) == Mock.Of<IDataProtector>()))
                .AddLogging()
                .AddScoped<ComponentStatePersistenceManager>()
                .AddScoped(_ => navManager.Object)
                .AddScoped<HttpContextFormDataProvider>()
                .BuildServiceProvider(),
        };

        return new ViewContext { HttpContext = httpContext };
    }

    private class TestComponent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, "Hello from the component");
        }
    }
}
