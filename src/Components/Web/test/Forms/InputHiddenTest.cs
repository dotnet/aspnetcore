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
