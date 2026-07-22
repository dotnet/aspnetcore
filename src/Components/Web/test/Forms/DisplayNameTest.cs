// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Forms;

public class DisplayNameTest
{
    [Fact]
    public async Task ThrowsIfNoForParameterProvided()
    {
        // Arrange
        var rootComponent = new TestHostComponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<DisplayName<string>>(0);
                builder.CloseComponent();
            }
        };

        var testRenderer = new TestRenderer();
        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await testRenderer.RenderRootComponentAsync(componentId));
        Assert.Contains("For", ex.Message);
        Assert.Contains("parameter", ex.Message);
    }

    [Fact]
    public async Task DisplaysPropertyNameWhenNoAttributePresent()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestHostComponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<DisplayName<string>>(0);
                builder.AddComponentParameter(1, "For", (System.Linq.Expressions.Expression<Func<string>>)(() => model.PlainProperty));
                builder.CloseComponent();
            }
        };

        // Act
        var output = await RenderAndGetOutput(rootComponent);

        // Assert
        Assert.Equal("PlainProperty", output);
    }

    [Fact]
    public async Task DisplaysDisplayAttributeName()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestHostComponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<DisplayName<string>>(0);
                builder.AddComponentParameter(1, "For", (System.Linq.Expressions.Expression<Func<string>>)(() => model.PropertyWithDisplayAttribute));
                builder.CloseComponent();
            }
        };

        // Act
        var output = await RenderAndGetOutput(rootComponent);

        // Assert
        Assert.Equal("Custom Display Name", output);
    }

    [Fact]
    public async Task DisplaysDisplayNameAttributeName()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestHostComponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<DisplayName<string>>(0);
                builder.AddComponentParameter(1, "For", (System.Linq.Expressions.Expression<Func<string>>)(() => model.PropertyWithDisplayNameAttribute));
                builder.CloseComponent();
            }
        };

        // Act
        var output = await RenderAndGetOutput(rootComponent);

        // Assert
        Assert.Equal("Custom DisplayName", output);
    }

    [Fact]
    public async Task DisplayAttributeTakesPrecedenceOverDisplayNameAttribute()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestHostComponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<DisplayName<string>>(0);
                builder.AddComponentParameter(1, "For", (System.Linq.Expressions.Expression<Func<string>>)(() => model.PropertyWithBothAttributes));
                builder.CloseComponent();
            }
        };

        // Act
        var output = await RenderAndGetOutput(rootComponent);

        // Assert
        // DisplayAttribute should take precedence per MVC conventions
        Assert.Equal("Display Takes Precedence", output);
    }

    [Fact]
    public async Task WorksWithDifferentPropertyTypes()
    {
        // Arrange
        var model = new TestModel();
        var intComponent = new TestHostComponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<DisplayName<int>>(0);
                builder.AddComponentParameter(1, "For", (System.Linq.Expressions.Expression<Func<int>>)(() => model.IntProperty));
                builder.CloseComponent();
            }
        };
        var dateComponent = new TestHostComponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<DisplayName<DateTime>>(0);
                builder.AddComponentParameter(1, "For", (System.Linq.Expressions.Expression<Func<DateTime>>)(() => model.DateProperty));
                builder.CloseComponent();
            }
        };

        // Act
        var intOutput = await RenderAndGetOutput(intComponent);
        var dateOutput = await RenderAndGetOutput(dateComponent);

        // Assert
        Assert.Equal("Integer Value", intOutput);
        Assert.Equal("Date Value", dateOutput);
    }

    [Fact]
    public async Task SupportsLocalizationWithResourceType()
    {
        var model = new TestModel();
        var rootComponent = new TestHostComponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<DisplayName<string>>(0);
                builder.AddComponentParameter(1, "For", (System.Linq.Expressions.Expression<Func<string>>)(() => model.PropertyWithResourceBasedDisplay));
                builder.CloseComponent();
            }
        };

        var output = await RenderAndGetOutput(rootComponent);
        Assert.Equal("Localized Display Name", output);
    }

    private static async Task<string> RenderAndGetOutput(TestHostComponent rootComponent)
    {
        var testRenderer = new TestRenderer();
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        var batch = testRenderer.Batches.Single();
        var displayLabelComponentFrame = batch.ReferenceFrames
            .First(f => f.FrameType == RenderTree.RenderTreeFrameType.Component &&
                       f.Component is DisplayName<string> or DisplayName<int> or DisplayName<DateTime>);

        // Find the text content frame within the component
        var textFrame = batch.ReferenceFrames
            .First(f => f.FrameType == RenderTree.RenderTreeFrameType.Text);

        return textFrame.TextContent;
    }

    private class TestHostComponent : ComponentBase
    {
        public RenderFragment InnerContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            InnerContent(builder);
        }
    }

    private class TestModel
    {
        public string PlainProperty { get; set; } = string.Empty;

        [Display(Name = "Custom Display Name")]
        public string PropertyWithDisplayAttribute { get; set; } = string.Empty;

        [DisplayName("Custom DisplayName")]
        public string PropertyWithDisplayNameAttribute { get; set; } = string.Empty;

        [Display(Name = "Display Takes Precedence")]
        [DisplayName("This Should Not Be Used")]
        public string PropertyWithBothAttributes { get; set; } = string.Empty;

        [Display(Name = "Integer Value")]
        public int IntProperty { get; set; }

        [Display(Name = "Date Value")]
        public DateTime DateProperty { get; set; }

        [Display(Name = nameof(TestResources.LocalizedDisplayName), ResourceType = typeof(TestResources))]
        public string PropertyWithResourceBasedDisplay { get; set; } = string.Empty;
    }

    public static class TestResources
    {
        public static string LocalizedDisplayName => "Localized Display Name";
    }
}
