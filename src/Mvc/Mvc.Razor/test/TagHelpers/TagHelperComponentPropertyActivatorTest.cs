// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Razor.TagHelpers;

public class TagHelperComponentPropertyActivatorTest
{
    [Fact]
    public void Activate_InitializesViewContext()
    {
        // Arrange
        var tagHelperComponent = new TestTagHelperComponent();
        var viewContext = CreateViewContext();

        var propertyActivator = new TagHelperComponentPropertyActivator();

        // Act
        propertyActivator.Activate(viewContext, tagHelperComponent);

        // Assert
        Assert.Same(viewContext, tagHelperComponent.ViewContext);
    }

    private class TestTagHelperComponent : ITagHelperComponent
    {
        public int Order => 1;

        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public void Init(TagHelperContext context)
        {
        }

        public Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            return Task.CompletedTask;
        }
    }

    private static ViewContext CreateViewContext()
    {
        var httpContext = new DefaultHttpContext()
        {
            RequestServices = new ServiceCollection()
            .AddSingleton<ITagHelperComponentPropertyActivator>(new TagHelperComponentPropertyActivator())
            .BuildServiceProvider()
        };

        var viewContext = Mock.Of<ViewContext>(vc => vc.HttpContext == httpContext);
        return viewContext;
    }
}
