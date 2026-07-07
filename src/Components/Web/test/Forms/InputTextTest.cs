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

    // Regression tests for issue #56463 - "Unexpected DOM persistence: Omitted attributes not removed during re-render"
    // (See https://github.com/dotnet/aspnetcore/issues/56463)
    // The original report reproduced the bug against the real InputText component using conditional
    // AdditionalAttributes passed from the parent. The tests below use the same code path against a
    // TestInputConditionalAttributeHostComponent which mirrors the issue's Razor pattern.

    [Fact]
    public async Task InputText_RemovesAttributeFromChildren_WhenOmittedOnSubsequentRender()
    {
        // Arrange
        // InputText itself always emits id/name/class/value (built into BuildRenderTree), so
        // we use a "data-" attribute name (a true splat-only attribute) to isolate the
        // CaptureUnmatchedValues behavior that issue #56463 is about.
        var model = new TestModel();
        var hostComponent = new TestInputConditionalAttributeHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            IncludeAttribute = true,
            AttributeName = "data-test-id",
            AttributeValue = "example-id",
        };

        var hostComponentId = _testRenderer.AssignRootComponentId(hostComponent);
        await _testRenderer.RenderRootComponentAsync(hostComponentId);

        var inputTextComponentId = _testRenderer.Batches.Single()
            .GetComponentFrames<InputText>().Single().ComponentId;

        var firstRenderFrames = _testRenderer.GetCurrentRenderTreeFrames(inputTextComponentId);
        Assert.Contains(firstRenderFrames.Array, f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "data-test-id" &&
            (string)f.AttributeValue == "example-id");

        // Act - omit the attribute on a re-render (this is the bug repro)
        hostComponent.IncludeAttribute = false;
        await _testRenderer.RenderRootComponentAsync(hostComponentId);

        // Assert - the diff against the <input> element must include a RemoveAttribute("data-test-id") edit.
        // Without the fix in ComponentProperties.SetProperties (issue #56463), no RemoveAttribute edit
        // is generated, leaving the stale attribute on the DOM <input> element.
        var inputTextDiff = _testRenderer.Batches[1]
            .DiffsByComponentId[inputTextComponentId]
            .Single();
        Assert.Contains(
            inputTextDiff.Edits,
            edit => edit.Type == RenderTreeEditType.RemoveAttribute && edit.RemovedAttributeName == "data-test-id");
    }

    [Fact]
    public async Task InputText_OmitsAttributeFromFirstRender_WhenNotSupplied()
    {
        // Covers the inverse case: a host that never supplies the attribute must not
        // emit (or remove) it. This guards against the fix accidentally over-clearing
        // captured values that have never been set.
        // Arrange
        var model = new TestModel();
        var hostComponent = new TestInputConditionalAttributeHostComponent<string, InputText>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
            IncludeAttribute = false,
            AttributeName = "data-test-omit-id",
            AttributeValue = "should-not-appear",
        };

        var hostComponentId = _testRenderer.AssignRootComponentId(hostComponent);
        await _testRenderer.RenderRootComponentAsync(hostComponentId);

        var inputTextComponentId = _testRenderer.Batches.Single()
            .GetComponentFrames<InputText>().Single().ComponentId;

        var frames = _testRenderer.GetCurrentRenderTreeFrames(inputTextComponentId);

        // The supplied AdditionalAttributes dictionary must be empty, so the attribute should never appear.
        Assert.DoesNotContain(frames.Array, f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "data-test-omit-id");
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
    }
}
