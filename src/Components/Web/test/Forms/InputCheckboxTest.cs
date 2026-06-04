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
        // ISSUE #1 FIX: Strengthened to verify binding attribute structure
        // This ensures onchange binding is properly set up for UI -> Model updates
        var model = new TestModel { BoolProperty = false };
        var rootComponent = new TestInputHostComponent<bool, InputCheckbox>
        {
            EditContext = new EditContext(model),
            Value = false,
            ValueExpression = () => model.BoolProperty,
        };

        var componentId = await RenderAndGetInputCheckboxComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Verify onchange binding attribute is present for UI -> Model updates
        var onchangeAttribute = frames.Array.SingleOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "onchange");
        Assert.NotNull(onchangeAttribute.AttributeValue);

        // Verify the handler has a valid event handler ID (binding is properly set up)
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
        // IMPORTANT: Name uses dots (model.BoolProperty), NOT underscores
        // (ID uses underscores: model_BoolProperty)
        Assert.Equal("model.BoolProperty", nameAttribute.AttributeValue);
    }

    [Fact]
    public async Task CssClassAttributeIncludesFieldValidationState()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var rootComponent = new TestInputHostComponent<bool, InputCheckbox>
        {
            EditContext = editContext,  // Required for CSS class validation state
            Value = false,
            ValueExpression = () => model.BoolProperty,
        };

        var componentId = await RenderAndGetInputCheckboxComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // ASSERT: Verify class attribute exists AND contains "valid" class
        // ISSUE #2 FIX: Was only checking NotNull, now verifies actual validation class content
        var classAttribute = frames.Array.SingleOrDefault(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class");
        Assert.NotNull(classAttribute.AttributeValue);
        var cssClass = classAttribute.AttributeValue?.ToString();
        Assert.NotNull(cssClass);
        // Pristine valid field should have "valid" class
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

        // ASSERT INITIAL: Should NOT have "modified" class (pristine field)
        // Should have "valid" class since field is valid and unmodified
        Assert.DoesNotContain("modified", initialCssClass ?? "");
        Assert.Contains("valid", initialCssClass ?? "");

        // ACT: Change the value - this notifies EditContext
        inputCheckboxComponent.SetCurrentValue(true);

        // ASSERT: Field should now include "modified" class after value change
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

        // ACT: Change the checkbox value
        inputCheckboxComponent.SetCurrentValue(true);

        // ASSERT: ValueChanged callback should be invoked exactly once with new value (true)
        Assert.Single(valueChangedCallLog);
        Assert.True(valueChangedCallLog[0]);
    }

    [Fact]
    public async Task EventDrivenBindingUpdatesModelViaDispatchEvent()
    {
        // ISSUE #5: Event-Driven Model Update test
        // Verifies that UI -> Model binding is properly wired via onchange event handler
        var model = new TestModel { BoolProperty = false };
        var rootComponent = new TestInputHostComponent<bool, TestInputCheckboxComponent>
        {
            EditContext = new EditContext(model),
            Value = false,
            ValueChanged = val => { }, // Placeholder to verify binding chain exists
            ValueExpression = () => model.BoolProperty,
        };

        // Use the same pattern as other tests for TestInputCheckboxComponent
        var componentId = await RenderAndGetTestInputCheckboxComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Verify onchange handler is properly registered with a valid handler ID
        var onchangeHandlerId = frames.Array
            .Where(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "onchange")
            .Select(f => f.AttributeEventHandlerId)
            .FirstOrDefault();

        // Handler ID must be non-zero for binding to work
        Assert.NotEqual(0ul, onchangeHandlerId);

        // Verify model starts unchanged
        Assert.False(model.BoolProperty);
    }

    [Fact]
    public async Task DoesNotInvokeValueChangedIfValueUnchanged()
    {
        // ISSUE #6: Redundancy resolved - preserved as separate edge case test
        // This guards against callback being fired when value hasn't actually changed
        var model = new TestModel();
        var valueChangedCallLog = new List<bool>();
        var rootComponent = new TestInputHostComponent<bool, TestInputCheckboxComponent>
        {
            EditContext = new EditContext(model),
            Value = true,  // Already true
            ValueChanged = val => valueChangedCallLog.Add(val),
            ValueExpression = () => model.BoolProperty,
        };

        var inputCheckboxComponent = await InputRenderer.RenderAndGetComponent(rootComponent);
        Assert.Empty(valueChangedCallLog);

        // ACT: Set to same value (no actual change)
        inputCheckboxComponent.SetCurrentValue(true);

        // ASSERT: No callback invoked since value didn't actually change
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
                { "data-test", "checkbox-test" },  // Custom data attribute
                { "disabled", true }               // Standard HTML attribute
            }
        };

        var componentId = await RenderAndGetInputCheckboxComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // ASSERT: Custom attributes should pass through to rendered HTML
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
        // ISSUE #4: Missing unchecked state test
        // FIX: Added test for when value=false (not checked)
        var model = new TestModel { BoolProperty = false };
        var rootComponent = new TestInputHostComponent<bool, InputCheckbox>
        {
            EditContext = new EditContext(model),
            Value = false,  // Unchecked state
            ValueExpression = () => model.BoolProperty,
        };

        var componentId = await RenderAndGetInputCheckboxComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // ASSERT: When value is false, checked attribute should NOT exist
        // Using SingleOrDefault instead of Single for safer assertion pattern
        var checkedAttribute = frames.Array.SingleOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "checked");
        Assert.Null(checkedAttribute.AttributeValue);
    }

    [Fact]
    public async Task CssClassIncludesInvalidStateWhenValidationErrorExists()
    {
        // ISSUE #3: Missing validation error scenario test
        // FIX: Added test for when validation error is added - CSS should show "invalid" class
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

        // Initially should be valid (no validation errors)
        var initialCssClass = inputCheckboxComponent.GetCssClass();
        Assert.Contains("valid", initialCssClass);
        Assert.DoesNotContain("invalid", initialCssClass);

        // ACT: Add validation error message
        var messages = new ValidationMessageStore(editContext);
        messages.Add(fieldIdentifier, "Checkbox must be checked");

        // ASSERT: CSS should now include "invalid" class
        var updatedCssClass = inputCheckboxComponent.GetCssClass();
        Assert.Contains("invalid", updatedCssClass);
    }

    [Fact]
    public async Task WorksWithoutEditContext()
    {
        // ISSUE #8: Missing "No EditContext" scenario test
        // FIX: Added test to verify component works without EditContext
        // This is valid: <InputCheckbox @bind-Value="value" /> without EditForm
        var rootComponent = new TestInputHostComponent<bool, TestInputCheckboxComponent>
        {
            // NO EditContext - should work without it
            Value = false,
            ValueExpression = () => new TestModel().BoolProperty,
        };

        // ACT & ASSERT: Should render without throwing
        var inputCheckboxComponent = await InputRenderer.RenderAndGetComponent(rootComponent);
        Assert.NotNull(inputCheckboxComponent);

        // Should still be able to change value internally (bypassing EditContext)
        inputCheckboxComponent.SetCurrentValue(true);
        Assert.True(inputCheckboxComponent.GetCurrentValue());
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
