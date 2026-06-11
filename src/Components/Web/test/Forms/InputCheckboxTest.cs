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
        var model = new TestModel { BoolProperty = false };
        var rootComponent = new TestInputHostComponent<bool, InputCheckbox>
        {
            EditContext = new EditContext(model),
            Value = false,
            ValueExpression = () => model.BoolProperty,
        };

        var componentId = await RenderAndGetInputCheckboxComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);
        var onchangeAttribute = frames.Array.SingleOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "onchange");
        Assert.NotNull(onchangeAttribute.AttributeValue);
        var handlerId = frames.Array
            .Where(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "onchange")
            .Select(f => f.AttributeEventHandlerId)
            .FirstOrDefault();
        Assert.NotEqual(0ul, handlerId);
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
        var classAttribute = frames.Array.SingleOrDefault(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class");
        Assert.NotNull(classAttribute.AttributeValue);
        var cssClass = classAttribute.AttributeValue?.ToString();
        Assert.NotNull(cssClass);
        Assert.Contains("valid", cssClass);
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
        var initialCssClass = inputCheckboxComponent.GetCssClass();
        Assert.DoesNotContain("modified", initialCssClass ?? "");
        Assert.Contains("valid", initialCssClass ?? "");
        inputCheckboxComponent.SetCurrentValue(true);
        var updatedCssClass = inputCheckboxComponent.GetCssClass();
        Assert.Contains("modified", updatedCssClass);
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
        inputCheckboxComponent.SetCurrentValue(true);
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
        inputCheckboxComponent.SetCurrentValue(true);
        Assert.Single(valueChangedCallLog);
        Assert.True(valueChangedCallLog[0]);
    }

    [Fact]
    public async Task EventDrivenBindingUpdatesModelViaDispatchEvent()
    {
        var model = new TestModel { BoolProperty = false };
        var rootComponent = new TestInputHostComponent<bool, TestInputCheckboxComponent>
        {
            EditContext = new EditContext(model),
            Value = false,
            ValueChanged = val => { },
            ValueExpression = () => model.BoolProperty,
        };
        var componentId = await RenderAndGetTestInputCheckboxComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);
        var onchangeHandlerId = frames.Array
            .Where(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "onchange")
            .Select(f => f.AttributeEventHandlerId)
            .FirstOrDefault();
        Assert.NotEqual(0ul, onchangeHandlerId);
        Assert.False(model.BoolProperty);
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
        inputCheckboxComponent.SetCurrentValue(true);
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
        var dataTestAttribute = frames.Array.SingleOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "data-test");
        Assert.NotNull(dataTestAttribute.AttributeValue);
        Assert.Equal("checkbox-test", dataTestAttribute.AttributeValue);

        var disabledAttribute = frames.Array.SingleOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "disabled");
        Assert.NotNull(disabledAttribute.AttributeValue);
        Assert.Equal(true, disabledAttribute.AttributeValue);
    }

    [Fact]
    public async Task RendersUncheckedStateWhenValueIsFalse()
    {
        var model = new TestModel { BoolProperty = false };
        var rootComponent = new TestInputHostComponent<bool, InputCheckbox>
        {
            EditContext = new EditContext(model),
            Value = false,
            ValueExpression = () => model.BoolProperty,
        };

        var componentId = await RenderAndGetInputCheckboxComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);
        var checkedAttribute = frames.Array.SingleOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "checked");
        Assert.Null(checkedAttribute.AttributeValue);
    }

    [Fact]
    public async Task CssClassIncludesInvalidStateWhenValidationErrorExists()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var fieldIdentifier = FieldIdentifier.Create(() => model.BoolProperty);
        var rootComponent = new TestInputHostComponent<bool, TestInputCheckboxComponent>
        {
            EditContext = editContext,
            Value = false,
            ValueExpression = () => model.BoolProperty,
        };

        var inputCheckboxComponent = await InputRenderer.RenderAndGetComponent(rootComponent);
        var initialCssClass = inputCheckboxComponent.GetCssClass();
        Assert.Contains("valid", initialCssClass);
        Assert.DoesNotContain("invalid", initialCssClass);

        var messages = new ValidationMessageStore(editContext);
        messages.Add(fieldIdentifier, "Checkbox must be checked");

        var updatedCssClass = inputCheckboxComponent.GetCssClass();
        Assert.Contains("invalid", updatedCssClass);
    }

    [Fact]
    public async Task WorksWithoutEditContext()
    {
        var rootComponent = new TestInputHostComponent<bool, TestInputCheckboxComponent>
        {
            Value = false,
            ValueExpression = () => new TestModel().BoolProperty,
        };

        var inputCheckboxComponent = await InputRenderer.RenderAndGetComponent(rootComponent);
        Assert.NotNull(inputCheckboxComponent);

        inputCheckboxComponent.SetCurrentValue(true);
        Assert.True(inputCheckboxComponent.GetCurrentValue());
    }

    [Fact]
    public async Task ComponentTypeAlwaysOverridesUserDefinedType()
    {
        var model = new TestModel();
        var hostComponent = new TestInputHostComponent<bool, InputCheckbox>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.BoolProperty,
            AdditionalAttributes = new Dictionary<string, object>
            {
                { "type", "text" }
            }
        };

        var componentId = await RenderAndGetInputCheckboxComponentIdAsync(hostComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var typeAttributeFrame = frames.Array.Single(frame =>
            frame.FrameType == RenderTreeFrameType.Attribute &&
            frame.AttributeName == "type");
        Assert.Equal("checkbox", typeAttributeFrame.AttributeValue);
    }

    [Fact]
    public async Task DisplayNameIsUsedInValidationMessages()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var fieldIdentifier = FieldIdentifier.Create(() => model.BoolProperty);

        var messages = new ValidationMessageStore(editContext);
        messages.Add(fieldIdentifier, "The Checkbox must be checked");

        var rootComponent = new TestInputHostComponent<bool, TestInputCheckboxComponent>
        {
            EditContext = editContext,
            Value = false,
            ValueExpression = () => model.BoolProperty,
        };

        var inputCheckboxComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        var fieldMessages = editContext.GetValidationMessages(fieldIdentifier);
        Assert.Single(fieldMessages);
        Assert.Equal("The Checkbox must be checked", fieldMessages.First());
    }

    [Fact]
    public async Task RendersCorrectlyWithNullAdditionalAttributes()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<bool, InputCheckbox>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.BoolProperty,
            AdditionalAttributes = null!
        };

        var componentId = await RenderAndGetInputCheckboxComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var typeAttribute = frames.Array.Single(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "type");
        Assert.Equal("checkbox", typeAttribute.AttributeValue);

        var idAttribute = frames.Array.SingleOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "id");
        Assert.NotNull(idAttribute.AttributeValue);

        var classAttribute = frames.Array.SingleOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class");
        Assert.NotNull(classAttribute.AttributeValue);
    }

    [Fact]
    public async Task ValueChangedCallbackInvokedCorrectlyForRapidChanges()
    {
        var model = new TestModel { BoolProperty = false };
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

        inputCheckboxComponent.SetCurrentValue(true);
        inputCheckboxComponent.SetCurrentValue(false);
        inputCheckboxComponent.SetCurrentValue(true);
        inputCheckboxComponent.SetCurrentValue(false);

        Assert.Equal(4, valueChangedCallLog.Count);
        Assert.Equal(new[] { true, false, true, false }, valueChangedCallLog);

        Assert.True(rootComponent.EditContext.IsModified(() => model.BoolProperty));
    }

    [Fact]
    public async Task CssClassCombinesFieldValidationStateWithAdditionalClass()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);

        var rootComponent = new TestInputHostComponent<bool, InputCheckbox>
        {
            EditContext = editContext,
            Value = false,
            ValueExpression = () => model.BoolProperty,
            AdditionalAttributes = new Dictionary<string, object>
            {
                { "class", "my-custom-class" }
            }
        };

        var componentId = await RenderAndGetInputCheckboxComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var classAttribute = frames.Array.Single(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class");

        var cssClass = classAttribute.AttributeValue?.ToString();

        Assert.NotNull(cssClass);
        Assert.Contains("my-custom-class", cssClass);
        Assert.Contains("valid", cssClass);
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
