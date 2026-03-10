// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Forms;

public class InputTextClientValidationTest
{
    private readonly TestRenderer _testRenderer = new TestRenderer();

    [Fact]
    public async Task EmitsDataValAttributes_WhenServiceIsOnEditContext()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var service = CreateService();
        editContext.Properties[ClientSideValidator.ServiceKey] = service;

        var rootComponent = new TestInputHostComponent<string?, InputText>
        {
            EditContext = editContext,
            ValueExpression = () => model.Name,
        };

        var componentId = await RenderAndGetInputTextComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        AssertHasAttribute(frames, "data-val", "true");
        AssertHasAttribute(frames, "data-val-required");
        AssertHasAttribute(frames, "data-val-length");
    }

    [Fact]
    public async Task DoesNotEmitDataValAttributes_WhenNoServiceOnEditContext()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);

        var rootComponent = new TestInputHostComponent<string?, InputText>
        {
            EditContext = editContext,
            ValueExpression = () => model.Name,
        };

        var componentId = await RenderAndGetInputTextComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        AssertHasNoAttribute(frames, "data-val");
        AssertHasNoAttribute(frames, "data-val-required");
    }

    [Fact]
    public async Task DataValAttributes_DoNotOverrideExplicitAttributes()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var service = CreateService();
        editContext.Properties[ClientSideValidator.ServiceKey] = service;

        var rootComponent = new TestInputHostComponent<string?, InputText>
        {
            EditContext = editContext,
            ValueExpression = () => model.Name,
            AdditionalAttributes = new Dictionary<string, object>
            {
                { "data-val-required", "Custom message" }
            }
        };

        var componentId = await RenderAndGetInputTextComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // The explicit attribute value should win
        AssertHasAttribute(frames, "data-val-required", "Custom message");
    }

    [Fact]
    public async Task EmitsDataValTrue_WhenAnyValidationAttributeExists()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var service = CreateService();
        editContext.Properties[ClientSideValidator.ServiceKey] = service;

        var rootComponent = new TestInputHostComponent<string?, InputText>
        {
            EditContext = editContext,
            ValueExpression = () => model.Email,
        };

        var componentId = await RenderAndGetInputTextComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        AssertHasAttribute(frames, "data-val", "true");
    }

    [Fact]
    public async Task DoesNotEmitDataVal_ForFieldWithNoAttributes()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var service = CreateService();
        editContext.Properties[ClientSideValidator.ServiceKey] = service;

        var rootComponent = new TestInputHostComponent<string?, InputText>
        {
            EditContext = editContext,
            ValueExpression = () => model.Optional,
        };

        var componentId = await RenderAndGetInputTextComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        AssertHasNoAttribute(frames, "data-val");
    }

    private static IClientValidationService CreateService()
    {
        return new TestClientValidationService();
    }

    private async Task<int> RenderAndGetInputTextComponentIdAsync(TestInputHostComponent<string?, InputText> hostComponent)
    {
        var hostComponentId = _testRenderer.AssignRootComponentId(hostComponent);
        await _testRenderer.RenderRootComponentAsync(hostComponentId);
        var batch = _testRenderer.Batches.Single();
        return batch.GetComponentFrames<InputText>().Single().ComponentId;
    }

    private static void AssertHasAttribute(ArrayRange<RenderTreeFrame> frames, string name, string? expectedValue = null)
    {
        var attr = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == name);
        Assert.True(attr.FrameType == RenderTreeFrameType.Attribute,
            $"Expected attribute '{name}' not found in rendered output.");
        if (expectedValue is not null)
        {
            Assert.Equal(expectedValue, attr.AttributeValue?.ToString());
        }
    }

    private static void AssertHasNoAttribute(ArrayRange<RenderTreeFrame> frames, string name)
    {
        var hasAttr = frames.Array.Any(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == name);
        Assert.False(hasAttr, $"Unexpected attribute '{name}' found in rendered output.");
    }

    private class TestModel
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        public string Optional { get; set; } = "";
    }
}
