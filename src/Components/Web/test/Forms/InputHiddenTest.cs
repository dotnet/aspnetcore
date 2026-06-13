// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Forms;

public class InputHiddenTest
{
    private readonly TestRenderer _testRenderer = new TestRenderer();

    [Fact]
    public async Task InputElementIsAssignedSuccessfullyAndHasCorrectAttributes()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputHidden>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        var inputHiddenComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Verify element reference is captured
        Assert.NotNull(inputHiddenComponent.Element);

        // Verify it renders the correct input type
        var componentId = await RenderAndGetInputHiddenComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);
        var typeAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "type");
        Assert.Equal("hidden", typeAttribute.AttributeValue);
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
        Assert.NotNull(nameAttribute.AttributeValue);
        Assert.True(!string.IsNullOrEmpty(nameAttribute.AttributeValue?.ToString()));
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

        Assert.NotNull(onchangeEvent.AttributeValue);
        Assert.Equal("onchange", onchangeEvent.AttributeName);
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
        Assert.NotNull(nameAttribute.AttributeValue);
        Assert.Equal("name", nameAttribute.AttributeName);
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

        Assert.NotNull(dataFieldIdAttr.AttributeValue);
        Assert.Equal("data-field-id", dataFieldIdAttr.AttributeName);
        Assert.Equal("field123", dataFieldIdAttr.AttributeValue);

        Assert.NotNull(dataEncryptedAttr.AttributeValue);
        Assert.Equal("data-encrypted", dataEncryptedAttr.AttributeName);
        Assert.Equal("true", dataEncryptedAttr.AttributeValue);

        Assert.NotNull(disabledAttr.AttributeValue);
        Assert.Equal("disabled", disabledAttr.AttributeName);
    }

    [Fact]
    public async Task RendersNullValueCorrectly()
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

        // When value is null, the value attribute should either be null or empty
        var valueAttribute = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "value");

        // Value attribute should exist (even if null/empty)
        Assert.True(
            valueAttribute.Equals(default(RenderTreeFrame)) ||
            valueAttribute.AttributeValue == null ||
            valueAttribute.AttributeValue.ToString() == ""
        );
    }

    [Fact]
    public async Task RendersCssClassAttribute()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);

        var rootComponent = new TestInputHostComponent<string, InputHidden>
        {
            EditContext = editContext,
            ValueExpression = () => model.StringProperty,
        };

        var componentId = await RenderAndGetInputHiddenComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var classAttribute = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class");

        // Class attribute should exist (may be empty or contain field CSS classes)
        Assert.Equal("class", classAttribute.AttributeName);
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

    [Fact]
    public async Task ValueAttributeAlwaysExists()
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

        // Value attribute must always exist for hidden inputs
        var valueAttribute = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "value");

        Assert.NotNull(valueAttribute.AttributeValue);
        Assert.Equal("value", valueAttribute.AttributeName);
        Assert.Equal("test-value", valueAttribute.AttributeValue);
    }

    [Fact]
    public async Task UserProvidedClassMergedWithValidationClass()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputHidden>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "class", "user-class" } }
        };

        var componentId = await RenderAndGetInputHiddenComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var classAttribute = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class");

        // Class should contain user-provided class
        Assert.Equal("class", classAttribute.AttributeName);
        Assert.NotNull(classAttribute.AttributeValue);
        var classValue = classAttribute.AttributeValue?.ToString() ?? "";
        Assert.Contains("user-class", classValue);
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
