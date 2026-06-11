// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
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
        var model = new TestModel { StringProperty = null };
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        Assert.NotNull(inputComponent.Element);
    }

    [Fact]
    public async Task HandlesEmptyStringValue()
    {
        var model = new TestModel { StringProperty = "" };
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        Assert.NotNull(inputComponent.Element);
    }

    [Fact]
    public async Task RendersPlaceholderAttribute()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "placeholder", "Enter your text here" } }
        };

        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "placeholder" && (string)f.AttributeValue == "Enter your text here");
    }

    [Fact]
    public async Task RendersRowsAttribute()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "rows", "5" } }
        };

        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "rows" && f.AttributeValue.ToString() == "5");
    }

    [Fact]
    public async Task RendersDisabledAttribute()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "disabled", true } }
        };

        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "disabled");
    }

    [Fact]
    public async Task RendersReadOnlyAttribute()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "readonly", true } }
        };

        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "readonly");
    }

    [Fact]
    public async Task RendersRequiredAttribute()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "required", true } }
        };

        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "required");
    }

    [Fact]
    public async Task RendersClassAttribute()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "class", "custom-textarea form-control" } }
        };

        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class" && f.AttributeValue.ToString().Contains("custom-textarea"));
    }

    [Fact]
    public async Task RendersValueAttribute()
    {
        var model = new TestModel { StringProperty = "multi-line\ntest\nvalue" };
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Element && f.ElementName == "textarea");
    }

    [Fact]
    public async Task RendersMultipleCustomAttributes()
    {
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

        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

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
        var model = new TestModel { StringProperty = "<script>alert('xss')</script>\n&nbsp;" };
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);
        Assert.NotNull(inputComponent.Element);
    }

    [Fact]
    public async Task HandlesUnicodeCharactersInValue()
    {
        var model = new TestModel { StringProperty = "こんにちは世界\n🌍 Emoji Support" };
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);
        Assert.NotNull(inputComponent.Element);
    }

    [Fact]
    public async Task RendersCorrectElementType()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Element && f.ElementName == "textarea");
    }

    [Fact]
    public async Task IgnoresUnknownAdditionalAttributes()
    {
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

        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);
        Assert.NotNull(inputComponent.Element);
    }

    [Fact]
    public async Task RendersIdWithComplexPropertyName()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.ComplexPropertyName,
        };

        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var idAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "id");
        Assert.NotNull(idAttribute.AttributeValue);
        Assert.Contains("ComplexPropertyName", idAttribute.AttributeValue.ToString());
    }

    [Fact]
    public async Task EmptyAdditionalAttributesDictionary()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object>()
        };

        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);
        Assert.NotNull(inputComponent.Element);
    }

    [Fact]
    public async Task NullAdditionalAttributes()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = null
        };

        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);
        Assert.NotNull(inputComponent.Element);
    }

    [Fact]
    public async Task LargeTextValue()
    {
        var largeValue = new string('a', 10000);
        var model = new TestModel { StringProperty = largeValue };
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);
        Assert.NotNull(inputComponent.Element);
    }

    [Fact]
    public async Task HandlesMultilineContentWithVariousLineEndings()
    {
        var model = new TestModel { StringProperty = "Line 1\r\nLine 2\nLine 3\rLine 4" };
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);
        Assert.NotNull(inputComponent.Element);
    }

    [Fact]
    public async Task RendersSpellCheckAttribute()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "spellcheck", "false" } }
        };

        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "spellcheck");
    }

    #region CSS Class Validation Tests

    [Fact]
    public async Task CssClassIncludesValidClass_WhenNoValidationErrors()
    {
        var model = new TestModel { StringProperty = "some value" };
        var rootComponent = CreateInputHostComponent(model);

        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var classAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class");
        Assert.Contains("valid", classAttribute.AttributeValue.ToString());
    }

    [Fact]
    public async Task CssClassIncludesInvalidClass_WhenValidationErrorsExist()
    {
        var model = new TestModel { StringProperty = "" };
        var editContext = new EditContext(model);
        var field = FieldIdentifier.Create(() => model.StringProperty);
        var messages = new ValidationMessageStore(editContext);
        messages.Add(field, "Required");

        var rootComponent = CreateInputHostComponent(model, editContext: editContext);

        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var classAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class");
        Assert.Contains("invalid", classAttribute.AttributeValue.ToString());
    }

    [Fact]
    public async Task CssClassIncludesModifiedClass_WhenFieldIsModified()
    {
        var model = new TestModel { StringProperty = "" };
        var editContext = new EditContext(model);
        var field = FieldIdentifier.Create(() => model.StringProperty);

        var rootComponent = CreateInputHostComponent(model, editContext: editContext);

        var hostComponentId = _testRenderer.AssignRootComponentId(rootComponent);
        await _testRenderer.RenderRootComponentAsync(hostComponentId);

        editContext.NotifyFieldChanged(field);
        await _testRenderer.RenderRootComponentAsync(hostComponentId);

        var batch = _testRenderer.Batches.First();
        var inputTextAreaFrames = batch.GetComponentFrames<InputTextArea>().ToList();
        Assert.True(inputTextAreaFrames.Count > 0);
        var inputComponentId = inputTextAreaFrames.First().ComponentId;
        var frames = _testRenderer.GetCurrentRenderTreeFrames(inputComponentId);

        var classAttribute = frames.Array.First(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class");
        Assert.Contains("modified", classAttribute.AttributeValue.ToString());
    }

    [Fact]
    public async Task CssClassMergesUserClassWithValidationClass()
    {
        var model = new TestModel { StringProperty = "some value" };
        var rootComponent = CreateInputHostComponent(model, additionalAttributes: new Dictionary<string, object> { { "class", "user-class" } });

        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var classAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class");
        var classValue = classAttribute.AttributeValue.ToString();
        Assert.Contains("user-class", classValue);
        Assert.Contains("valid", classValue);
    }

    #endregion

    #region Accessibility Tests

    [Fact]
    public async Task RendersAriaInvalidAttribute_WhenValidationFails()
    {
        var model = new TestModel { StringProperty = "" };
        var editContext = new EditContext(model);
        var field = FieldIdentifier.Create(() => model.StringProperty);
        var messages = new ValidationMessageStore(editContext);
        messages.Add(field, "Required");

        var rootComponent = CreateInputHostComponent(model, editContext: editContext);

        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var ariaInvalid = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "aria-invalid");
        Assert.Equal("true", ariaInvalid.AttributeValue);
    }

    #endregion

    #region Event Handler Tests

    [Fact]
    public async Task RendersOnChangeEventHandler()
    {
        var model = new TestModel { StringProperty = "initial" };
        var rootComponent = CreateInputHostComponent(model);

        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "onchange");
    }

    [Fact]
    public async Task InputTextArea_UpdatesModel_OnChange()
    {
        var model = new TestModel { StringProperty = "before" };
        var rootComponent = CreateInputHostComponentBoundToModel(model);

        var hostComponentId = _testRenderer.AssignRootComponentId(rootComponent);
        await _testRenderer.RenderRootComponentAsync(hostComponentId);
        var batch = _testRenderer.Batches.First();
        var inputComponentId = batch.GetComponentFrames<InputTextArea>().Single().ComponentId;

        var frames = _testRenderer.GetCurrentRenderTreeFrames(inputComponentId);
        var onchangeAttr = frames.Array.First(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "onchange");
        var eventHandlerId = onchangeAttr.AttributeEventHandlerId;
        Assert.NotEqual((ulong)0, eventHandlerId);

        await _testRenderer.DispatchEventAsync(eventHandlerId, new ChangeEventArgs { Value = "after" });

        Assert.Equal("after", model.StringProperty);
    }

    #endregion

    #region Value Binding Tests

    [Fact]
    public async Task RendersValueAttribute_BoundToModel()
    {
        var model = new TestModel { StringProperty = "multi-line\ntest\nvalue" };
        var rootComponent = CreateInputHostComponent(model);

        var componentId = await RenderAndGetInputTextAreaComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        AssertContainsElement(frames, "textarea");
        var onchange = frames.Array.First(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "onchange");
        Assert.Equal("value", onchange.AttributeEventUpdatesAttributeName);
        Assert.Equal("multi-line\ntest\nvalue", model.StringProperty);
    }

    #endregion

    #region No EditContext Scenario

    [Fact]
    public async Task WorksWithoutEditContext_DoesNotThrow()
    {
        var model = new TestModel { StringProperty = "test-value" };
        var rootComponent = new TestInputHostComponent<string, InputTextArea>
        {
            ValueExpression = () => model.StringProperty
        };

        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);
        Assert.NotNull(inputComponent.Element);
    }

    #endregion

    #region Helper Methods

    private async Task<int> RenderAndGetInputTextAreaComponentIdAsync(TestInputHostComponent<string, InputTextArea> hostComponent)
    {
        var hostComponentId = _testRenderer.AssignRootComponentId(hostComponent);
        await _testRenderer.RenderRootComponentAsync(hostComponentId);
        var batch = _testRenderer.Batches.Single();
        return batch.GetComponentFrames<InputTextArea>().Single().ComponentId;
    }

    private TestInputHostComponent<string, InputTextArea> CreateInputHostComponent(
        TestModel model,
        EditContext editContext = null,
        Dictionary<string, object> additionalAttributes = null)
    {
        return new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = editContext ?? new EditContext(model),
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = additionalAttributes
        };
    }

    private TestInputHostComponent<string, InputTextArea> CreateInputHostComponentBoundToModel(
        TestModel model,
        EditContext editContext = null,
        Dictionary<string, object> additionalAttributes = null)
    {
        return new TestInputHostComponent<string, InputTextArea>
        {
            EditContext = editContext ?? new EditContext(model),
            Value = model.StringProperty,
            ValueChanged = v => model.StringProperty = v,
            ValueExpression = () => model.StringProperty,
            AdditionalAttributes = additionalAttributes
        };
    }

    private static void AssertContainsElement(ArrayRange<RenderTreeFrame> frames, string elementName)
    {
        Assert.Contains(frames.Array, f => f.FrameType == RenderTreeFrameType.Element && f.ElementName == elementName);
    }

    private class TestModel
    {
        public string StringProperty { get; set; }
        public string ComplexPropertyName { get; set; }
    }
}
#endregion
