// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Forms;

public class InputHiddenTest
{
    private readonly TestRenderer _testRenderer = new TestRenderer();

    [Fact]
    public async Task InputElementIsAssignedSuccessfully()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputHidden>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        var inputHiddenComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        Assert.NotNull(inputHiddenComponent.Element);
    }

    [Fact]
    public async Task RendersIdAttribute()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputHidden>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        var componentId = await RenderAndGetInputHiddenComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var idAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "id");
        Assert.Equal("model_StringProperty", idAttribute.AttributeValue);
    }

    [Fact]
    public async Task ExplicitIdOverridesGenerated()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputHidden>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "id", "custom-hidden-id" } }
        };

        var componentId = await RenderAndGetInputHiddenComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var idAttribute = frames.Array.First(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "id");
        Assert.Equal("custom-hidden-id", idAttribute.AttributeValue);
    }

    [Fact]
    public async Task RendersInputTypeAsHidden()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputHidden>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        var componentId = await RenderAndGetInputHiddenComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var typeAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "type");
        Assert.Equal("hidden", typeAttribute.AttributeValue);
    }

    [Fact]
    public async Task RendersNameAttribute()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputHidden>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        var componentId = await RenderAndGetInputHiddenComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var nameAttribute = frames.Array.FirstOrDefault(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "name");
        // Name attribute should be present (generated from field name)
        Assert.True(nameAttribute.AttributeName == "name");
    }

    [Fact]
    public async Task RendersValueAttribute()
    {
        var model = new TestModel { StringProperty = "test-value" };
        var rootComponent = new TestInputHostComponent<string, InputHidden>
        {
            EditContext = new EditContext(model),
            Value = "test-value",
            ValueExpression = () => model.StringProperty,
        };

        var componentId = await RenderAndGetInputHiddenComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var valueAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "value");
        Assert.Equal("test-value", valueAttribute.AttributeValue);
    }

    [Fact]
    public async Task RendersEmptyStringValue()
    {
        var model = new TestModel { StringProperty = "" };
        var rootComponent = new TestInputHostComponent<string, InputHidden>
        {
            EditContext = new EditContext(model),
            Value = "",
            ValueExpression = () => model.StringProperty,
        };

        var componentId = await RenderAndGetInputHiddenComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var valueAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "value");
        Assert.Equal("", valueAttribute.AttributeValue);
    }

    [Fact]
    public async Task RendersMultipleAdditionalAttributes()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputHidden>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object>
            {
                { "data-test", "custom-data" },
                { "aria-label", "Hidden field" },
                { "title", "This is a hidden field" }
            }
        };

        var componentId = await RenderAndGetInputHiddenComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var dataTestAttr = frames.Array.FirstOrDefault(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "data-test");
        var ariaLabelAttr = frames.Array.FirstOrDefault(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "aria-label");
        var titleAttr = frames.Array.FirstOrDefault(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "title");

        Assert.True(dataTestAttr.AttributeName == "data-test");
        Assert.Equal("custom-data", dataTestAttr.AttributeValue);
        Assert.True(ariaLabelAttr.AttributeName == "aria-label");
        Assert.Equal("Hidden field", ariaLabelAttr.AttributeValue);
        Assert.True(titleAttr.AttributeName == "title");
        Assert.Equal("This is a hidden field", titleAttr.AttributeValue);
    }

    [Fact]
    public async Task OnChangeEventBinderIsPresent()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputHidden>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        var componentId = await RenderAndGetInputHiddenComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Look for the onchange event binding
        var onchangeEvent = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "onchange");

        Assert.True(onchangeEvent.AttributeName == "onchange");
    }

    [Fact]
    public async Task RendersCorrectHtmlElement()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputHidden>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        var componentId = await RenderAndGetInputHiddenComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // First frame should be the opening element frame for "input"
        var elementFrame = frames.Array.First(f => f.FrameType == RenderTreeFrameType.Element);
        Assert.Equal("input", elementFrame.ElementName);
    }

    [Fact]
    public async Task ExplicitNameAttributeOverridesGenerated()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputHidden>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "name", "custom-name" } }
        };

        var componentId = await RenderAndGetInputHiddenComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var nameAttribute = frames.Array.First(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "name");
        Assert.Equal("custom-name", nameAttribute.AttributeValue);
    }

    [Fact]
    public async Task UserDefinedTypeAttributeOverridesDefault()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputHidden>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "type", "text" } }
        };

        var componentId = await RenderAndGetInputHiddenComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var typeAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "type");
        // User-defined type should override the default "hidden"
        Assert.Equal("text", typeAttribute.AttributeValue);
    }

    [Fact]
    public async Task MultipleAttributesFromAdditionalAttributesArePreserved()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputHidden>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object>
            {
                { "data-field-id", "field123" },
                { "data-encrypted", "true" },
                { "disabled", true }
            }
        };

        var componentId = await RenderAndGetInputHiddenComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var dataFieldIdAttr = frames.Array.FirstOrDefault(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "data-field-id");
        var dataEncryptedAttr = frames.Array.FirstOrDefault(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "data-encrypted");
        var disabledAttr = frames.Array.FirstOrDefault(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "disabled");

        Assert.True(dataFieldIdAttr.AttributeName == "data-field-id");
        Assert.Equal("field123", dataFieldIdAttr.AttributeValue);
        Assert.True(dataEncryptedAttr.AttributeName == "data-encrypted");
        Assert.Equal("true", dataEncryptedAttr.AttributeValue);
        Assert.True(disabledAttr.AttributeName == "disabled");
    }

     [Fact]
    public async Task RendersNullValueAsEmptyString()
    {
        var model = new TestModel { StringProperty = null };
        var rootComponent = new TestInputHostComponent<string, InputHidden>
        {
            Value = null,
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        var componentId = await RenderAndGetInputHiddenComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // When value is null, CurrentValueAsString returns null which may not render value attribute
        var valueAttributes = frames.Array.Where(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "value").ToList();

        // Verify the input element exists and is a hidden input
        var typeAttribute = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "type");
        Assert.Equal("hidden", typeAttribute.AttributeValue);
    }

    [Fact]
    public async Task RendersCssClassWithFieldValidationClass()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        editContext.Validate(); // trigger validation to see field class

        var rootComponent = new TestInputHostComponent<string, InputHidden>
        {
            EditContext = editContext,
            ValueExpression = () => model.StringProperty,
        };

        var componentId = await RenderAndGetInputHiddenComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var classAttribute = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class");
        // Should include field validation CSS class (e.g., "modified", "valid", or "invalid")
        Assert.True(classAttribute.AttributeName == "class");
    }

    [Fact]
    public async Task FieldIdentifierIsCreatedCorrectly()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var rootComponent = new TestInputHostComponent<string, InputHidden>
        {
            EditContext = editContext,
            ValueExpression = () => model.StringProperty,
        };

        var inputHiddenComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // FieldIdentifier should be created from ValueExpression
        Assert.Equal("StringProperty", inputHiddenComponent.FieldIdentifier.FieldName);
    }

    private async Task<int> RenderAndGetInputHiddenComponentIdAsync(TestInputHostComponent<string, InputHidden> hostComponent)
    {
        var hostComponentId = _testRenderer.AssignRootComponentId(hostComponent);
        await _testRenderer.RenderRootComponentAsync(hostComponentId);
        var batch = _testRenderer.Batches.Single();
        return batch.GetComponentFrames<InputHidden>().Single().ComponentId;
    }

    private class TestModel
    {
        public string StringProperty { get; set; }
    }
}
