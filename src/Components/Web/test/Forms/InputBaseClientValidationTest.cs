// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms.ClientValidation;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Forms;

public class InputBaseClientValidationTest
{
    private readonly TestRenderer _testRenderer = new TestRenderer();

    [Fact]
    public async Task InputBase_RendersDataValAttributes_WhenClientValidationServiceRegistered()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        editContext.Properties[typeof(IClientValidationService)] = new TestClientValidationService(
            new Dictionary<string, object>
            {
                ["data-val"] = "true",
                ["data-val-required"] = "Generated required message.",
            });

        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = editContext,
            ValueExpression = () => model.Value,
        };

        var componentId = await RenderAndGetInputComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        AssertHasAttribute(frames, "data-val", "true");
        AssertHasAttribute(frames, "data-val-required", "Generated required message.");
    }

    [Fact]
    public async Task InputBase_DoesNotRenderDataValAttributes_WhenServiceNotRegistered()
    {
        var model = new TestModel();
        var editContext = new EditContext(model); // No IClientValidationService registered

        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = editContext,
            ValueExpression = () => model.Value,
        };

        var componentId = await RenderAndGetInputComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        AssertNoAttribute(frames, "data-val");
    }

    [Fact]
    public async Task InputBase_DeveloperAdditionalAttributes_TakePrecedenceOverGenerated()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        editContext.Properties[typeof(IClientValidationService)] = new TestClientValidationService(
            new Dictionary<string, object>
            {
                ["data-val"] = "true",
                ["data-val-required"] = "Generated message",
            });

        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = editContext,
            ValueExpression = () => model.Value,
            AdditionalAttributes = new Dictionary<string, object>
            {
                ["data-val-required"] = "Custom developer message",
            },
        };

        var componentId = await RenderAndGetInputComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Developer-specified attribute must win (first-wins merging in InputBase)
        AssertHasAttribute(frames, "data-val-required", "Custom developer message");
        // Generated data-val=true should still be present (no developer override)
        AssertHasAttribute(frames, "data-val", "true");
    }

    [Fact]
    public async Task InputBase_NoDataValAttributes_WhenServiceReturnsEmpty()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        editContext.Properties[typeof(IClientValidationService)] = new TestClientValidationService(
            new Dictionary<string, object>());

        var rootComponent = new TestInputHostComponent<string, InputText>
        {
            EditContext = editContext,
            ValueExpression = () => model.Value,
        };

        var componentId = await RenderAndGetInputComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        AssertNoAttribute(frames, "data-val");
    }

    // Helpers

    private async Task<int> RenderAndGetInputComponentIdAsync(TestInputHostComponent<string, InputText> hostComponent)
    {
        var hostComponentId = _testRenderer.AssignRootComponentId(hostComponent);
        await _testRenderer.RenderRootComponentAsync(hostComponentId);
        var batch = _testRenderer.Batches.Single();
        return batch.GetComponentFrames<InputText>().Single().ComponentId;
    }

    private static void AssertHasAttribute(ArrayRange<RenderTreeFrame> frames, string attributeName, object expectedValue)
    {
        var match = frames.Array.Take(frames.Count).FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == attributeName);

        Assert.True(match.FrameType == RenderTreeFrameType.Attribute,
            $"Expected attribute '{attributeName}' was not found in rendered output.");
        Assert.Equal(expectedValue, match.AttributeValue);
    }

    private static void AssertNoAttribute(ArrayRange<RenderTreeFrame> frames, string attributeName)
    {
        Assert.DoesNotContain(frames.Array.Take(frames.Count), f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == attributeName);
    }

    /// <summary>
    /// Test implementation of IClientValidationService that returns a fixed set of attributes
    /// (or null) regardless of which field is requested. Lets us test InputBase's merge behavior
    /// without depending on the internal DefaultClientValidationService.
    /// </summary>
    private sealed class TestClientValidationService : IClientValidationService
    {
        private readonly IReadOnlyDictionary<string, object> _attributes;

        public TestClientValidationService(IReadOnlyDictionary<string, object> attributes)
        {
            _attributes = attributes;
        }

        public IReadOnlyDictionary<string, object> GetClientValidationAttributes(FieldIdentifier fieldIdentifier)
            => _attributes;
    }

    private class TestModel
    {
        public string Value { get; set; } = "";
    }
}
