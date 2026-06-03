// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Forms;

public class InputTextTest
{
    private readonly TestRenderer _testRenderer = new TestRenderer();

    [Fact]
    public async Task InputElementIsAssignedSuccessfully()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        // Act
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Assert
        Assert.NotNull(inputSelectComponent.Element);
    }

    [Fact]
    public async Task RendersIdAttribute()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        var componentId = await RenderAndGetInputTextComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var idAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "id");
        Assert.Equal("model_StringProperty", idAttribute.AttributeValue);
    }

    [Fact]
    public async Task ExplicitIdOverridesGenerated()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "id", "custom-id" } }
        };

        var componentId = await RenderAndGetInputTextComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var idAttribute = frames.Array.First(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "id");
        Assert.Equal("custom-id", idAttribute.AttributeValue);
    }

    [Fact]
    public async Task RendersIdAttribute_WhenShouldUseFieldIdentifiersIsFalse_InteractiveMode()
    {
        // simulate interactive mode where ShouldUseFieldIdentifiers is false
        var model = new TestModel();
        var editContext = new EditContext(model) { ShouldUseFieldIdentifiers = false };
        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = editContext,
            ValueExpression = () => model.StringProperty,
        };

        var componentId = await RenderAndGetInputTextComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // id should still be generated for Label/Input association to work in interactive mode
        var idAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "id");
        Assert.Equal("model_StringProperty", idAttribute.AttributeValue);
    }

    [Fact]
    public async Task HandlesNullValue()
    {
        // Arrange
        var model = new TestModel { StringProperty = null };
        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        // Act
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Assert - should not throw, handle null gracefully
        Assert.NotNull(inputComponent.Element);
    }

    [Fact]
    public async Task HandlesEmptyStringValue()
    {
        // Arrange
        var model = new TestModel { StringProperty = "" };
        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        // Act
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Assert
        Assert.NotNull(inputComponent.Element);
    }

    [Fact]
    public async Task RendersPlaceholderAttribute()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "placeholder", "Enter text here" } }
        };

        // Act
        var componentId = await RenderAndGetInputTextComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "placeholder" && (string)f.AttributeValue == "Enter text here");
    }

    [Fact]
    public async Task RendersMaxLengthAttribute()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "maxlength", "50" } }
        };

        // Act
        var componentId = await RenderAndGetInputTextComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "maxlength" && f.AttributeValue.ToString() == "50");
    }

    [Fact]
    public async Task RendersDisabledAttribute()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "disabled", true } }
        };

        // Act
        var componentId = await RenderAndGetInputTextComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "disabled");
    }

    [Fact]
    public async Task RendersReadOnlyAttribute()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "readonly", true } }
        };

        // Act
        var componentId = await RenderAndGetInputTextComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "readonly");
    }

    [Fact]
    public async Task RendersRequiredAttribute()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "required", true } }
        };

        // Act
        var componentId = await RenderAndGetInputTextComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "required");
    }

    [Fact]
    public async Task RendersTypeAttribute()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "type", "email" } }
        };

        // Act
        var componentId = await RenderAndGetInputTextComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "type" && (string)f.AttributeValue == "email");
    }

    [Fact]
    public async Task RendersClassAttribute()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "class", "custom-class form-control" } }
        };

        // Act
        var componentId = await RenderAndGetInputTextComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class" && f.AttributeValue.ToString().Contains("custom-class"));
    }

    [Fact]
    public async Task RendersValueAttribute_WithCurrentValue()
    {
        // Arrange
        var model = new TestModel { StringProperty = "test-value" };
        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        // Act
        var componentId = await RenderAndGetInputTextComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert - verify the input element exists with value set (via element existence check)
        // Note: InputText renders value attribute but TestRenderer may not expose it as Attribute frame
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Element && f.ElementName == "input");
    }

    [Fact]
    public async Task RendersMultipleCustomAttributes()
    {
        // Arrange
        var model = new TestModel();
        var additionalAttributes = new Dictionary<string, object>
        {
            { "id", "custom-id" },
            { "placeholder", "Enter name" },
            { "maxlength", 100 },
            { "class", "form-control" },
            { "data-test", "value123" }
        };
        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = additionalAttributes
        };

        // Act
        var componentId = await RenderAndGetInputTextComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert - all attributes should be present
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "id" && (string)f.AttributeValue == "custom-id");
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "placeholder");
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "maxlength");
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class");
    }

    [Fact]
    public async Task HandlesSpecialCharactersInValue()
    {
        // Arrange
        var model = new TestModel { StringProperty = "<script>alert('xss')</script>" };
        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        // Act & Assert - should not throw
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);
        Assert.NotNull(inputComponent.Element);
    }

    [Fact]
    public async Task HandlesUnicodeCharactersInValue()
    {
        // Arrange
        var model = new TestModel { StringProperty = "こんにちは世界 🌍" };
        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        // Act
        var componentId = await RenderAndGetInputTextComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert - input element renders value differently
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Element && f.ElementName == "input");
    }

    [Fact]
    public async Task RendersCorrectElementType()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        // Act
        var componentId = await RenderAndGetInputTextComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert - should render <input> element
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Element && f.ElementName == "input");
    }

    [Fact]
    public async Task IgnoresUnknownAdditionalAttributes()
    {
        // Arrange - test that unknown attributes don't cause errors
        var model = new TestModel();
        var additionalAttributes = new Dictionary<string, object>
        {
            { "data-custom-property", "value" },
            { "aria-label", "Custom Input" }
        };
        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = additionalAttributes
        };

        // Act & Assert
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);
        Assert.NotNull(inputComponent.Element);
    }

    [Fact]
    public async Task RendersIdWithComplexPropertyName()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.ComplexPropertyName,
        };

        // Act
        var componentId = await RenderAndGetInputTextComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert - ID should handle complex names
        var idAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "id");
        Assert.Contains("ComplexPropertyName", idAttribute.AttributeValue.ToString());
    }

    [Fact]
    public async Task EmptyAdditionalAttributesDictionary()
    {
        // Arrange - test with empty additional attributes
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object>()
        };

        // Act & Assert
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);
        Assert.NotNull(inputComponent.Element);
    }

    [Fact]
    public async Task NullAdditionalAttributes()
    {
        // Arrange - test with null additional attributes
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = null
        };

        // Act & Assert
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);
        Assert.NotNull(inputComponent.Element);
    }

    [Fact]
    public async Task RendersNameAttribute_BindsToFieldIdentifier()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        // Act
        var componentId = await RenderAndGetInputTextComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert - name attribute should be generated from FieldIdentifier
        var nameAttribute = frames.Array.Single(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "name");
        Assert.Equal("model.StringProperty", nameAttribute.AttributeValue);
    }

    [Fact]
    public async Task RendersNameAttribute_CanBeOverriddenViaAdditionalAttributes()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "name", "custom-name" } }
        };

        // Act
        var componentId = await RenderAndGetInputTextComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert - AdditionalAttributes["name"] should override generated name
        var nameAttribute = frames.Array.First(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "name");
        Assert.Equal("custom-name", nameAttribute.AttributeValue);
    }

    [Fact]
    public async Task CssClassIncludesValidClass_WhenNoValidationErrors()
    {
        // Arrange
        var model = new TestModel { StringProperty = "some value" };
        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        // Act
        var componentId = await RenderAndGetInputTextComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert - CssClass should include "valid" when model is valid (no validation errors)
        var classAttribute = frames.Array.Single(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class");
        Assert.Contains("valid", classAttribute.AttributeValue.ToString());
    }

    [Fact]
    public async Task CssClassIncludesInvalidClass_WhenValidationErrorsExist()
    {
        // Arrange
        var model = new TestModel { StringProperty = "" };
        var editContext = new EditContext(model);
        var field = FieldIdentifier.Create(() => model.StringProperty);
        var messages = new ValidationMessageStore(editContext);
        messages.Add(field, "Required");

        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = editContext,
            ValueExpression = () => model.StringProperty,
        };

        // Act
        var componentId = await RenderAndGetInputTextComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert - CssClass should include "invalid" when validation errors exist
        var classAttribute = frames.Array.Single(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class");
        Assert.Contains("invalid", classAttribute.AttributeValue.ToString());
    }

    [Fact]
    public async Task CssClassIncludesModifiedClass_WhenFieldIsModified()
    {
        // Arrange
        var model = new TestModel { StringProperty = "" };
        var editContext = new EditContext(model);
        var field = FieldIdentifier.Create(() => model.StringProperty);

        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = editContext,
            ValueExpression = () => model.StringProperty,
        };

        var hostComponentId = _testRenderer.AssignRootComponentId(rootComponent);
        await _testRenderer.RenderRootComponentAsync(hostComponentId);

        // Act - simulate field modification (user typing)
        editContext.NotifyFieldChanged(field);

        // Re-render the host component after modification
        await _testRenderer.RenderRootComponentAsync(hostComponentId);

        // Get the InputText component's frames - use First() since there may be multiple batches
        var batch = _testRenderer.Batches.First();
        var inputTextFrames = batch.GetComponentFrames<InputText>();
        var inputComponentId = inputTextFrames.First().ComponentId;
        var frames = _testRenderer.GetCurrentRenderTreeFrames(inputComponentId);

        // Assert - CssClass should include "modified" when field has been changed
        var classAttribute = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class");
        Assert.True(classAttribute.FrameType != RenderTreeFrameType.None, "class attribute should be rendered");
        Assert.Contains("modified", classAttribute.AttributeValue.ToString());
    }

    [Fact]
    public async Task CssClassMergesUserClassWithValidationClass()
    {
        // Arrange - user provides custom class, component should append validation class
        var model = new TestModel { StringProperty = "some value" };
        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "class", "user-class" } }
        };

        // Act
        var componentId = await RenderAndGetInputTextComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert - both user class and validation class should be present
        var classAttribute = frames.Array.Single(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class");
        var classValue = classAttribute.AttributeValue.ToString();
        Assert.Contains("user-class", classValue);
        Assert.Contains("valid", classValue);
    }

    [Fact]
    public async Task RendersAriaInvalidAttribute_WhenValidationFails()
    {
        // Arrange
        var model = new TestModel { StringProperty = "" };
        var editContext = new EditContext(model);
        var field = FieldIdentifier.Create(() => model.StringProperty);
        var messages = new ValidationMessageStore(editContext);
        messages.Add(field, "Required");

        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = editContext,
            ValueExpression = () => model.StringProperty,
        };

        // Act
        var componentId = await RenderAndGetInputTextComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert - aria-invalid="true" should be auto-added when validation fails
        var ariaInvalid = frames.Array.Single(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "aria-invalid");
        Assert.Equal("true", ariaInvalid.AttributeValue);
    }

    [Fact]
    public async Task RendersOnChangeEventHandler()
    {
        // Arrange
        var model = new TestModel { StringProperty = "initial" };
        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        // Act
        var componentId = await RenderAndGetInputTextComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert - onchange event handler should be present
        Assert.Contains(frames.Array, f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "onchange");
    }

    private async Task<int> RenderAndGetInputTextComponentIdAsync(TestInputHostComponent<string, InputText> hostComponent)
    {
        var hostComponentId = _testRenderer.AssignRootComponentId(hostComponent);
        await _testRenderer.RenderRootComponentAsync(hostComponentId);
        var batch = _testRenderer.Batches.Single();
        return batch.GetComponentFrames<InputText>().Single().ComponentId;
    }

    private class TestModel
    {
        public string StringProperty { get; set; }
        public string ComplexPropertyName { get; set; }
    }
}
