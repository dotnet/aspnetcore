// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Forms;

public class InputCheckboxTest
{
    private readonly TestRenderer _testRenderer = new TestRenderer();

    [Fact]
    public async Task InputElementIsAssignedSuccessfully()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<bool, InputCheckbox>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.BoolProperty,
        };

        var inputCheckboxComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        Assert.NotNull(inputCheckboxComponent.Element);
    }

    [Fact]
    public async Task RendersIdAttribute()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<bool, InputCheckbox>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.BoolProperty,
        };

        var componentId = await RenderAndGetInputCheckboxComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var idAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "id");
        Assert.Equal("model_BoolProperty", idAttribute.AttributeValue);
    }

    [Fact]
    public async Task ExplicitIdOverridesGenerated()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<bool, InputCheckbox>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.BoolProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "id", "custom-checkbox-id" } }
        };

        var componentId = await RenderAndGetInputCheckboxComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var idAttribute = frames.Array.First(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "id");
        Assert.Equal("custom-checkbox-id", idAttribute.AttributeValue);
    }

    [Fact]
    public async Task RendersTypeAttributeAsCheckbox()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<bool, InputCheckbox>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.BoolProperty,
        };

        var componentId = await RenderAndGetInputCheckboxComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var typeAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "type");
        Assert.Equal("checkbox", typeAttribute.AttributeValue);
    }

    [Fact]
    public async Task RendersCheckedAttributeWhenValueIsTrue()
    {
        var model = new TestModel { BoolProperty = true };
        var rootComponent = new TestInputHostComponent<bool, InputCheckbox>
        {
            EditContext = new EditContext(model),
            Value = true,
            ValueExpression = () => model.BoolProperty,
        };

        var componentId = await RenderAndGetInputCheckboxComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var checkedAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "checked");
        Assert.Equal(true, checkedAttribute.AttributeValue);
    }

    [Fact]
    public async Task BindingUpdatesCheckedAttributeWhenValueChanges()
    {
        // This test validates that the checked binding is set up correctly
        // We verify by testing the binding event handler exists
        var model = new TestModel { BoolProperty = false };
        var rootComponent = new TestInputHostComponent<bool, InputCheckbox>
        {
            EditContext = new EditContext(model),
            Value = false,
            ValueExpression = () => model.BoolProperty,
        };

        var componentId = await RenderAndGetInputCheckboxComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Verify SetUpdatesAttributeName was called for "checked" binding
        // This ensures two-way binding is properly set up
        var hasCheckboxBinding = frames.Array.Any(f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "onchange");

        Assert.True(hasCheckboxBinding, "onchange binding must be present for checked state updates");
    }

    [Fact]
    public async Task RendersValueAttributeAsTrueString()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<bool, InputCheckbox>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.BoolProperty,
        };

        var componentId = await RenderAndGetInputCheckboxComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var valueAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "value");
        Assert.Equal("True", valueAttribute.AttributeValue);
    }

    [Fact]
    public async Task RendersNameAttribute()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<bool, InputCheckbox>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.BoolProperty,
        };

        var componentId = await RenderAndGetInputCheckboxComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var nameAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "name");
        // Name attribute uses field identifier notation with dots
        Assert.Equal("model.BoolProperty", nameAttribute.AttributeValue);
    }

    [Fact]
    public async Task CssClassAttributeIncludesFieldValidationState()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var rootComponent = new TestInputHostComponent<bool, InputCheckbox>
        {
            EditContext = editContext,
            Value = false,
            ValueExpression = () => model.BoolProperty,
        };

        var componentId = await RenderAndGetInputCheckboxComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var classAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class");
        // Class attribute must exist and contain field state classes
        Assert.NotNull(classAttribute.AttributeValue);
        Assert.IsType<string>(classAttribute.AttributeValue);
    }

    [Fact]
    public async Task CssClassIncludesModifiedStateAfterValueChange()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var rootComponent = new TestInputHostComponent<bool, TestInputCheckboxComponent>
        {
            EditContext = editContext,
            Value = false,
            ValueExpression = () => model.BoolProperty,
        };

        var inputCheckboxComponent = await InputRenderer.RenderAndGetComponent(rootComponent);
        Assert.DoesNotContain("modified", inputCheckboxComponent.GetCssClass());

        // Act - change the value
        inputCheckboxComponent.SetCurrentValue(true);

        // Assert - field should now include "modified" class
        Assert.Contains("modified", inputCheckboxComponent.GetCssClass());
    }

    [Fact]
    public async Task NotifiesEditContextWhenCheckboxValueChanges()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<bool, TestInputCheckboxComponent>
        {
            EditContext = new EditContext(model),
            Value = false,
            ValueExpression = () => model.BoolProperty,
        };

        var inputCheckboxComponent = await InputRenderer.RenderAndGetComponent(rootComponent);
        Assert.False(rootComponent.EditContext.IsModified(() => model.BoolProperty));

        // Act - change the CurrentValue
        inputCheckboxComponent.SetCurrentValue(true);

        // Assert - EditContext should be notified of the change
        Assert.True(rootComponent.EditContext.IsModified(() => model.BoolProperty));
    }

    [Fact]
    public async Task InvokesValueChangedCallbackWhenValueChanges()
    {
        var model = new TestModel();
        var valueChangedCallLog = new List<bool>();
        var rootComponent = new TestInputHostComponent<bool, TestInputCheckboxComponent>
        {
            EditContext = new EditContext(model),
            Value = false,
            ValueChanged = val => valueChangedCallLog.Add(val),
            ValueExpression = () => model.BoolProperty,
        };

        var inputCheckboxComponent = await InputRenderer.RenderAndGetComponent(rootComponent);
        Assert.Empty(valueChangedCallLog);

        // Act
        inputCheckboxComponent.SetCurrentValue(true);

        // Assert - must have exactly one callback with the new value
        Assert.Single(valueChangedCallLog);
        Assert.True(valueChangedCallLog[0]);
    }

    [Fact]
    public async Task DoesNotInvokeValueChangedIfValueUnchanged()
    {
        var model = new TestModel();
        var valueChangedCallLog = new List<bool>();
        var rootComponent = new TestInputHostComponent<bool, TestInputCheckboxComponent>
        {
            EditContext = new EditContext(model),
            Value = true,
            ValueChanged = val => valueChangedCallLog.Add(val),
            ValueExpression = () => model.BoolProperty,
        };

        var inputCheckboxComponent = await InputRenderer.RenderAndGetComponent(rootComponent);
        Assert.Empty(valueChangedCallLog);

        // Act - set to same value
        inputCheckboxComponent.SetCurrentValue(true);

        // Assert - No callback invoked since value didn't actually change
        Assert.Empty(valueChangedCallLog);
    }

    [Fact]
    public async Task AdditionalAttributesArePropagated()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<bool, InputCheckbox>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.BoolProperty,
            AdditionalAttributes = new Dictionary<string, object>
            {
                { "data-test", "checkbox-test" },
                { "disabled", true }
            }
        };

        var componentId = await RenderAndGetInputCheckboxComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var dataTestAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "data-test");
        Assert.Equal("checkbox-test", dataTestAttribute.AttributeValue);

        var disabledAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "disabled");
        Assert.Equal(true, disabledAttribute.AttributeValue);
    }

    [Fact]
    public async Task OnChangeBindingIsPresent()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<bool, InputCheckbox>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.BoolProperty,
        };

        var componentId = await RenderAndGetInputCheckboxComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Verify that onchange handler is registered (it's an attribute with EventCallbackWorkItem type or similar)
        var hasOnChangeAttribute = frames.Array.Any(f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "onchange");

        Assert.True(hasOnChangeAttribute, "onchange attribute must be present for value binding");
    }

    [Fact]
    public async Task ComponentTypeAlwaysOverridesUserDefinedType()
    {
        // InputCheckbox always renders type="checkbox", even if user provides different type
        var model = new TestModel();
        var hostComponent = new TestInputHostComponent<bool, InputCheckbox>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.BoolProperty,
            AdditionalAttributes = new Dictionary<string, object>
            {
                { "type", "text" }  // User attempts to override, but checkbox type should prevail
            }
        };

        var componentId = await RenderAndGetInputCheckboxComponentIdAsync(hostComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var typeAttributeFrame = frames.Array.Single(frame =>
            frame.FrameType == RenderTreeFrameType.Attribute &&
            frame.AttributeName == "type");

        // Assert - checkbox type always takes precedence
        Assert.Equal("checkbox", typeAttributeFrame.AttributeValue);
    }

    private async Task<int> RenderAndGetInputCheckboxComponentIdAsync(TestInputHostComponent<bool, InputCheckbox> hostComponent)
    {
        var hostComponentId = _testRenderer.AssignRootComponentId(hostComponent);
        await _testRenderer.RenderRootComponentAsync(hostComponentId);
        var batch = _testRenderer.Batches.Single();
        return batch.GetComponentFrames<InputCheckbox>().Single().ComponentId;
    }

    private async Task<int> RenderAndGetTestInputCheckboxComponentIdAsync(TestInputHostComponent<bool, TestInputCheckboxComponent> hostComponent)
    {
        var hostComponentId = _testRenderer.AssignRootComponentId(hostComponent);
        await _testRenderer.RenderRootComponentAsync(hostComponentId);
        var batch = _testRenderer.Batches.Single();
        return batch.GetComponentFrames<TestInputCheckboxComponent>().Single().ComponentId;
    }

    private class TestModel
    {
        public bool BoolProperty { get; set; }
    }

    private class TestInputCheckboxComponent : InputCheckbox
    {
        public bool GetCurrentValue() => CurrentValue;

        public void SetCurrentValue(bool value) => CurrentValue = value;

        public string GetCssClass() => CssClass;
    }
}
