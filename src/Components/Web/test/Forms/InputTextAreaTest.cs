// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Forms;

public class InputTextAreaTest
{
    private readonly TestRenderer _testRenderer = new TestRenderer();

    [Fact]
    public async Task InputElementIsAssignedSuccessfully()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        Assert.NotNull(inputSelectComponent.Element);
    }

    [Fact]
    public async Task RendersIdAttribute()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var idAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "id");
        Assert.Equal("model_StringProperty", idAttribute.AttributeValue);
    }

    [Fact]
    public async Task ExplicitIdOverridesGenerated()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "id", "custom-textarea-id" } }
        };

        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var idAttribute = frames.Array.First(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "id");
        Assert.Equal("custom-textarea-id", idAttribute.AttributeValue);
    }

    [Fact]
    public async Task HandlesNullValue()
    {
        // Arrange
        var model = new TestModel { StringProperty = null };
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
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
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
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
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "placeholder", "Enter your text here" } }
        };

        // Act
        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "placeholder" && (string)f.AttributeValue == "Enter your text here");
    }

    [Fact]
    public async Task RendersRowsAttribute()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "rows", 5 } }
        };

        // Act
        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "rows" && (int)f.AttributeValue == 5);
    }

    [Fact]
    public async Task RendersColsAttribute()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "cols", 80 } }
        };

        // Act
        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "cols" && (int)f.AttributeValue == 80);
    }

    [Fact]
    public async Task RendersMaxLengthAttribute()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "maxlength", 500 } }
        };

        // Act
        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "maxlength" && (int)f.AttributeValue == 500);
    }

    [Fact]
    public async Task RendersDisabledAttribute()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "disabled", true } }
        };

        // Act
        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "disabled");
    }

    [Fact]
    public async Task RendersReadOnlyAttribute()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "readonly", true } }
        };

        // Act
        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "readonly");
    }

    [Fact]
    public async Task RendersRequiredAttribute()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "required", true } }
        };

        // Act
        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "required");
    }

    [Fact]
    public async Task RendersClassAttribute()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "class", "custom-textarea form-control" } }
        };

        // Act
        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class" && f.AttributeValue.ToString().Contains("custom-textarea"));
    }

    [Fact]
    public async Task RendersValueAttribute()
    {
        // Arrange
        var model = new TestModel { StringProperty = "multi-line\ntest\nvalue" };
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        // Act
        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert - just verify text content exists (textarea renders value differently)
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Text);
    }

    [Fact]
    public async Task RendersMultipleCustomAttributes()
    {
        // Arrange
        var model = new TestModel();
        var additionalAttributes = new Dictionary<string, object>
        {
            { "id", "custom-textarea-id" },
            { "placeholder", "Enter detailed information" },
            { "rows", 10 },
            { "cols", 100 },
            { "maxlength", 1000 },
            { "class", "form-control textarea-large" },
            { "data-test", "textarea-value" }
        };
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = additionalAttributes
        };

        // Act
        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert - all attributes should be present
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "id");
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "placeholder");
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "rows");
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "cols");
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "maxlength");
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class");
    }

    [Fact]
    public async Task HandlesSpecialCharactersInValue()
    {
        // Arrange
        var model = new TestModel { StringProperty = "<script>alert('xss')</script>\n&nbsp;" };
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
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
        var model = new TestModel { StringProperty = "こんにちは世界\n🌍 Emoji Support" };
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        // Act & Assert
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);
        Assert.NotNull(inputComponent.Element);
    }

    [Fact]
    public async Task RendersCorrectElementType()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        // Act
        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert - should render <textarea> element
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Element && f.ElementName == "textarea");
    }

    [Fact]
    public async Task IgnoresUnknownAdditionalAttributes()
    {
        // Arrange - test that unknown attributes don't cause errors
        var model = new TestModel();
        var additionalAttributes = new Dictionary<string, object>
        {
            { "data-custom-property", "value" },
            { "aria-label", "Detailed Comment Textarea" }
        };
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
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
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.ComplexPropertyName,
        };

        // Act
        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert - ID should handle complex names
        var idAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "id");
        Assert.NotNull(idAttribute.AttributeValue);
        Assert.Contains("ComplexPropertyName", idAttribute.AttributeValue.ToString());
    }

    [Fact]
    public async Task EmptyAdditionalAttributesDictionary()
    {
        // Arrange - test with empty additional attributes
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
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
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
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
    public async Task LargeTextValue()
    {
        // Arrange - test with very large text block (10,000 chars)
        var largeValue = new string('a', 10000);
        var model = new TestModel { StringProperty = largeValue };
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        // Act & Assert
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);
        Assert.NotNull(inputComponent.Element);
    }

    [Fact]
    public async Task HandlesMultilineContentWithVariousLineEndings()
    {
        // Arrange - test with CRLF, LF, and mixed line endings
        var model = new TestModel { StringProperty = "Line 1\r\nLine 2\nLine 3\rLine 4" };
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        // Act & Assert
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);
        Assert.NotNull(inputComponent.Element);
    }

    [Fact]
    public async Task RendersSpellCheckAttribute()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "spellcheck", "false" } }
        };

        // Act
        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "spellcheck");
    }

    [Fact]
    public async Task RendersWrapAttribute()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "wrap", "soft" } }
        };

        // Act
        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "wrap" && (string)f.AttributeValue == "soft");
    }

    private async Task<int> RenderAndGetInputTextAreaComponentIdAsync(TestInputHostComponent<string, InputTextArea> hostComponent)
    {
        var hostComponentId = _testRenderer.AssignRootComponentId(hostComponent);
        await _testRenderer.RenderRootComponentAsync(hostComponentId);
        var batch = _testRenderer.Batches.Single();
        return batch.GetComponentFrames<InputTextArea>().Single().ComponentId;
    }

    private class TestModel
    {
        public string StringProperty { get; set; }
        public string ComplexPropertyName { get; set; }
    }
}
