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
            AdditionalAttributes = new Dictionary<string, object> { { "maxlength", 50 } }
        };

        // Act
        var componentId = await RenderAndGetInputTextComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "maxlength" && (int)f.AttributeValue == 50);
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
    public async Task RendersValueAttribute()
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

        // Assert
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "value" && (string)f.AttributeValue == "test-value");
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

        // Assert
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "value" && (string)f.AttributeValue == "こんにちは世界 🌍");
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
    public async Task LongStringValue()
    {
        // Arrange - test with very long string
        var longValue = new string('a', 10000);
        var model = new TestModel { StringProperty = longValue };
        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        // Act & Assert
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);
        Assert.NotNull(inputComponent.Element);
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
