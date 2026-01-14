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

    private async Task<int> RenderAndGetInputCheckboxComponentIdAsync(TestInputHostComponent<bool, InputCheckbox> hostComponent)
    {
        var hostComponentId = _testRenderer.AssignRootComponentId(hostComponent);
        await _testRenderer.RenderRootComponentAsync(hostComponentId);
        var batch = _testRenderer.Batches.Single();
        return batch.GetComponentFrames<InputCheckbox>().Single().ComponentId;
    }

    private class TestModel
    {
        public bool BoolProperty { get; set; }
    }
}
