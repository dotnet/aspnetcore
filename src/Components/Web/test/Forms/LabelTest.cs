// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Forms;

public class LabelTest
{
    [Fact]
    public async Task RendersLabelElement()
    {
        var model = new TestModel();
        var rootComponent = new TestHostComponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Label<string>>(0);
                builder.AddComponentParameter(1, "For", (Expression<Func<string>>)(() => model.PlainProperty));
                builder.CloseComponent();
            }
        };

        var frames = await RenderAndGetFrames(rootComponent);

        var labelElement = frames.First(f => f.FrameType == RenderTree.RenderTreeFrameType.Element && f.ElementName == "label");
        Assert.Equal("label", labelElement.ElementName);
    }

    [Fact]
    public async Task DisplaysDisplayAttributeNameAsContent()
    {
        var model = new TestModel();
        var rootComponent = new TestHostComponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Label<string>>(0);
                builder.AddComponentParameter(1, "For", (Expression<Func<string>>)(() => model.PropertyWithDisplayAttribute));
                builder.CloseComponent();
            }
        };

        var frames = await RenderAndGetFrames(rootComponent);

        var textFrame = frames.First(f => f.FrameType == RenderTree.RenderTreeFrameType.Text);
        Assert.Equal("Custom Display Name", textFrame.TextContent);
    }

    [Fact]
    public async Task DisplaysPropertyNameWhenNoAttributePresent()
    {
        var model = new TestModel();
        var rootComponent = new TestHostComponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Label<string>>(0);
                builder.AddComponentParameter(1, "For", (Expression<Func<string>>)(() => model.PlainProperty));
                builder.CloseComponent();
            }
        };

        var frames = await RenderAndGetFrames(rootComponent);

        var textFrame = frames.First(f => f.FrameType == RenderTree.RenderTreeFrameType.Text);
        Assert.Equal("PlainProperty", textFrame.TextContent);
    }

    [Fact]
    public async Task DisplaysDisplayNameAttributeName()
    {
        var model = new TestModel();
        var rootComponent = new TestHostComponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Label<string>>(0);
                builder.AddComponentParameter(1, "For", (Expression<Func<string>>)(() => model.PropertyWithDisplayNameAttribute));
                builder.CloseComponent();
            }
        };

        var frames = await RenderAndGetFrames(rootComponent);

        var textFrame = frames.First(f => f.FrameType == RenderTree.RenderTreeFrameType.Text);
        Assert.Equal("Custom DisplayName", textFrame.TextContent);
    }

    [Fact]
    public async Task DisplayAttributeTakesPrecedenceOverDisplayNameAttribute()
    {
        var model = new TestModel();
        var rootComponent = new TestHostComponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Label<string>>(0);
                builder.AddComponentParameter(1, "For", (Expression<Func<string>>)(() => model.PropertyWithBothAttributes));
                builder.CloseComponent();
            }
        };

        var frames = await RenderAndGetFrames(rootComponent);

        var textFrame = frames.First(f => f.FrameType == RenderTree.RenderTreeFrameType.Text);
        Assert.Equal("Display Takes Precedence", textFrame.TextContent);
    }

    [Fact]
    public async Task AppliesAdditionalAttributes()
    {
        var model = new TestModel();
        var additionalAttributes = new Dictionary<string, object>
        {
            { "class", "form-label" },
            { "data-testid", "my-label" }
        };

        var rootComponent = new TestHostComponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Label<string>>(0);
                builder.AddComponentParameter(1, "For", (Expression<Func<string>>)(() => model.PlainProperty));
                builder.AddComponentParameter(2, "AdditionalAttributes", additionalAttributes);
                builder.CloseComponent();
            }
        };

        var frames = await RenderAndGetFrames(rootComponent);

        var classAttribute = frames.FirstOrDefault(f => f.FrameType == RenderTree.RenderTreeFrameType.Attribute && f.AttributeName == "class");
        Assert.NotNull(classAttribute.AttributeName);
        Assert.Equal("form-label", classAttribute.AttributeValue);

        var dataAttribute = frames.FirstOrDefault(f => f.FrameType == RenderTree.RenderTreeFrameType.Attribute && f.AttributeName == "data-testid");
        Assert.NotNull(dataAttribute.AttributeName);
        Assert.Equal("my-label", dataAttribute.AttributeValue);
    }

    [Fact]
    public async Task RendersChildContent()
    {
        var model = new TestModel();
        var rootComponent = new TestHostComponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Label<string>>(0);
                builder.AddComponentParameter(1, "For", (Expression<Func<string>>)(() => model.PlainProperty));
                builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(childBuilder =>
                {
                    childBuilder.OpenElement(0, "input");
                    childBuilder.AddAttribute(1, "type", "text");
                    childBuilder.CloseElement();
                }));
                builder.CloseComponent();
            }
        };

        var frames = await RenderAndGetFrames(rootComponent);

        var labelElement = frames.First(f => f.FrameType == RenderTree.RenderTreeFrameType.Element && f.ElementName == "label");
        Assert.Equal("label", labelElement.ElementName);

        var inputElement = frames.First(f => f.FrameType == RenderTree.RenderTreeFrameType.Element && f.ElementName == "input");
        Assert.Equal("input", inputElement.ElementName);

        var textFrame = frames.First(f => f.FrameType == RenderTree.RenderTreeFrameType.Text);
        Assert.Equal("PlainProperty", textFrame.TextContent);
    }

    [Fact]
    public async Task WorksWithDifferentPropertyTypes()
    {
        var model = new TestModel();
        var intComponent = new TestHostComponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Label<int>>(0);
                builder.AddComponentParameter(1, "For", (Expression<Func<int>>)(() => model.IntProperty));
                builder.CloseComponent();
            }
        };
        var dateComponent = new TestHostComponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Label<DateTime>>(0);
                builder.AddComponentParameter(1, "For", (Expression<Func<DateTime>>)(() => model.DateProperty));
                builder.CloseComponent();
            }
        };

        var intFrames = await RenderAndGetFrames(intComponent);
        var dateFrames = await RenderAndGetFrames(dateComponent);

        var intText = intFrames.First(f => f.FrameType == RenderTree.RenderTreeFrameType.Text);
        var dateText = dateFrames.First(f => f.FrameType == RenderTree.RenderTreeFrameType.Text);
        Assert.Equal("Integer Value", intText.TextContent);
        Assert.Equal("Date Value", dateText.TextContent);
    }

    [Fact]
    public async Task ThrowsWhenForIsNull()
    {
        var rootComponent = new TestHostComponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Label<string>>(0);
                // Not setting For parameter
                builder.CloseComponent();
            }
        };

        var testRenderer = new TestRenderer();
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => testRenderer.RenderRootComponentAsync(componentId));

        Assert.Contains("For", exception.Message);
    }

    [Fact]
    public async Task AllowsForAttributeInAdditionalAttributes()
    {
        var model = new TestModel();
        var additionalAttributes = new Dictionary<string, object>
        {
            { "for", "some-id" }
        };

        var rootComponent = new TestHostComponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Label<string>>(0);
                builder.AddComponentParameter(1, "For", (Expression<Func<string>>)(() => model.PlainProperty));
                builder.AddComponentParameter(2, "AdditionalAttributes", additionalAttributes);
                builder.CloseComponent();
            }
        };

        var frames = await RenderAndGetFrames(rootComponent);

        var labelElement = frames.First(f => f.FrameType == RenderTree.RenderTreeFrameType.Element && f.ElementName == "label");
        Assert.Equal("label", labelElement.ElementName);

        var forAttribute = frames.First(f => f.FrameType == RenderTree.RenderTreeFrameType.Attribute && f.AttributeName == "for");
        Assert.Equal("some-id", forAttribute.AttributeValue);
    }

    [Fact]
    public async Task RendersWithoutChildContent()
    {
        var model = new TestModel();
        var rootComponent = new TestHostComponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Label<string>>(0);
                builder.AddComponentParameter(1, "For", (Expression<Func<string>>)(() => model.PlainProperty));
                builder.CloseComponent();
            }
        };

        var frames = await RenderAndGetFrames(rootComponent);

        var labelElement = frames.First(f => f.FrameType == RenderTree.RenderTreeFrameType.Element && f.ElementName == "label");
        Assert.Equal("label", labelElement.ElementName);

        var textFrame = frames.First(f => f.FrameType == RenderTree.RenderTreeFrameType.Text);
        Assert.Equal("PlainProperty", textFrame.TextContent);
    }

    [Fact]
    public async Task RendersForAttributeWhenNoChildContent()
    {
        var model = new TestModel();
        var rootComponent = new TestHostComponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Label<string>>(0);
                builder.AddComponentParameter(1, "For", (Expression<Func<string>>)(() => model.PlainProperty));
                builder.CloseComponent();
            }
        };

        var frames = await RenderAndGetFrames(rootComponent);

        var forAttribute = frames.First(f => f.FrameType == RenderTree.RenderTreeFrameType.Attribute && f.AttributeName == "for");
        Assert.Equal("model_PlainProperty", forAttribute.AttributeValue);
    }

    [Fact]
    public async Task DoesNotRenderForAttributeWhenChildContentProvided()
    {
        var model = new TestModel();
        var rootComponent = new TestHostComponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Label<string>>(0);
                builder.AddComponentParameter(1, "For", (Expression<Func<string>>)(() => model.PlainProperty));
                builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(childBuilder =>
                {
                    childBuilder.AddContent(0, "Input goes here");
                }));
                builder.CloseComponent();
            }
        };

        var frames = await RenderAndGetFrames(rootComponent);

        var forAttributes = frames.Where(f => f.FrameType == RenderTree.RenderTreeFrameType.Attribute && f.AttributeName == "for");
        Assert.Empty(forAttributes);
    }

    [Fact]
    public async Task NonNestedLabel_ExplicitForOverridesGenerated()
    {
        var model = new TestModel();
        var additionalAttributes = new Dictionary<string, object>
        {
            { "for", "custom-input-id" }
        };

        var rootComponent = new TestHostComponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Label<string>>(0);
                builder.AddComponentParameter(1, "For", (Expression<Func<string>>)(() => model.PlainProperty));
                builder.AddComponentParameter(2, "AdditionalAttributes", additionalAttributes);
                builder.CloseComponent();
            }
        };

        var frames = await RenderAndGetFrames(rootComponent);

        var forAttribute = frames.First(f => f.FrameType == RenderTree.RenderTreeFrameType.Attribute && f.AttributeName == "for");
        Assert.Equal("custom-input-id", forAttribute.AttributeValue);
    }

    [Fact]
    public async Task WorksWithNestedProperties()
    {
        var model = new TestModelWithNestedProperty();
        var rootComponent = new TestHostComponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Label<string>>(0);
                builder.AddComponentParameter(1, "For", (Expression<Func<string>>)(() => model.Address.Street));
                builder.CloseComponent();
            }
        };

        var frames = await RenderAndGetFrames(rootComponent);

        var textFrame = frames.First(f => f.FrameType == RenderTree.RenderTreeFrameType.Text);
        Assert.Equal("Street Address", textFrame.TextContent);
    }

    [Fact]
    public async Task SupportsLocalizationWithResourceType()
    {
        var model = new TestModel();
        var rootComponent = new TestHostComponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Label<string>>(0);
                builder.AddComponentParameter(1, "For", (Expression<Func<string>>)(() => model.PropertyWithResourceBasedDisplay));
                builder.CloseComponent();
            }
        };

        var frames = await RenderAndGetFrames(rootComponent);

        var textFrame = frames.First(f => f.FrameType == RenderTree.RenderTreeFrameType.Text);
        Assert.Equal("Localized Display Name", textFrame.TextContent);
    }

    [Fact]
    public async Task ReRendersWhenForChangesWithSameDisplayNameButAttributesChange()
    {
        var model = new TestModel();
        Expression<Func<string>> forExpression1 = () => model.PlainProperty;
        Expression<Func<string>> forExpression2 = () => model.PlainProperty;

        var attributes1 = new Dictionary<string, object> { { "class", "label-1" } };
        var attributes2 = new Dictionary<string, object> { { "class", "label-2" } };

        var currentFor = forExpression1;
        var currentAttributes = attributes1;

        var testRenderer = new TestRenderer();
        var rootComponent = new TestHostComponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Label<string>>(0);
                builder.AddComponentParameter(1, "For", currentFor);
                builder.AddComponentParameter(2, "AdditionalAttributes", currentAttributes);
                builder.CloseComponent();
            }
        };

        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        // Verify initial render has class="label-1"
        var initialFrames = testRenderer.Batches.Last().ReferenceFrames;
        var initialClassAttr = initialFrames.First(f =>
            f.FrameType == RenderTree.RenderTreeFrameType.Attribute && f.AttributeName == "class");
        Assert.Equal("label-1", initialClassAttr.AttributeValue);

        // Change both For (different object, same display name) and AdditionalAttributes
        currentFor = forExpression2;
        currentAttributes = attributes2;
        rootComponent.InnerContent = builder =>
        {
            builder.OpenComponent<Label<string>>(0);
            builder.AddComponentParameter(1, "For", currentFor);
            builder.AddComponentParameter(2, "AdditionalAttributes", currentAttributes);
            builder.CloseComponent();
        };

        await testRenderer.Dispatcher.InvokeAsync(() => rootComponent.TriggerRender());

        // Should have re-rendered with the new attributes
        var updatedFrames = testRenderer.Batches.Last().ReferenceFrames;
        var updatedClassAttr = updatedFrames.First(f =>
            f.FrameType == RenderTree.RenderTreeFrameType.Attribute && f.AttributeName == "class");
        Assert.Equal("label-2", updatedClassAttr.AttributeValue);
    }

    private static async Task<RenderTreeFrame[]> RenderAndGetFrames(TestHostComponent rootComponent)
    {
        var testRenderer = new TestRenderer();
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        var batch = testRenderer.Batches.Single();
        return batch.ReferenceFrames;
    }

    private class TestHostComponent : ComponentBase
    {
        public RenderFragment InnerContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            InnerContent(builder);
        }

        public void TriggerRender()
        {
            StateHasChanged();
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

    private class TestModelWithNestedProperty
    {
        public AddressModel Address { get; set; } = new();
    }

    private class AddressModel
    {
        [Display(Name = "Street Address")]
        public string Street { get; set; } = string.Empty;
    }

    public static class TestResources
    {
        public static string LocalizedDisplayName => "Localized Display Name";
    }
}
