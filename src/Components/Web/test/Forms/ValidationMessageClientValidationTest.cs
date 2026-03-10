// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Forms;

public class ValidationMessageClientValidationTest
{
    [Fact]
    public async Task RendersSpanWithDataValmsgFor_WhenServiceIsPresent()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var service = CreateService();
        editContext.Properties[ClientSideValidator.ServiceKey] = service;

        var testRenderer = new TestRenderer();
        var host = new ValidationMessageHostComponent<string>
        {
            EditContext = editContext,
            For = () => model.Name,
        };

        var hostId = testRenderer.AssignRootComponentId(host);
        await testRenderer.RenderRootComponentAsync(hostId);

        var batch = testRenderer.Batches.Single();
        var validationMsgComponentId = batch.GetComponentFrames<ValidationMessage<string>>().Single().ComponentId;
        var frames = testRenderer.GetCurrentRenderTreeFrames(validationMsgComponentId);

        // Should render <span data-valmsg-for="model.Name" class="field-validation-valid">
        var spanFrame = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Element && f.ElementName == "span");
        Assert.Equal("span", spanFrame.ElementName);

        var forAttr = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "data-valmsg-for");
        Assert.NotNull(forAttr.AttributeValue);
        Assert.Contains("Name", forAttr.AttributeValue.ToString());
    }

    [Fact]
    public async Task RendersOriginalDivs_WhenServiceIsNotPresent()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);

        // Add a validation error so something renders
        var msgStore = new ValidationMessageStore(editContext);
        var fieldId = FieldIdentifier.Create(() => model.Name);
        msgStore.Add(fieldId, "Test error");

        var testRenderer = new TestRenderer();
        var host = new ValidationMessageHostComponent<string>
        {
            EditContext = editContext,
            For = () => model.Name,
        };

        var hostId = testRenderer.AssignRootComponentId(host);
        await testRenderer.RenderRootComponentAsync(hostId);

        var batch = testRenderer.Batches.Single();
        var validationMsgComponentId = batch.GetComponentFrames<ValidationMessage<string>>().Single().ComponentId;
        var frames = testRenderer.GetCurrentRenderTreeFrames(validationMsgComponentId);

        // Should render <div class="validation-message">
        var divFrame = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Element && f.ElementName == "div");
        Assert.Equal("div", divFrame.ElementName);

        var classAttr = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class");
        Assert.Equal("validation-message", classAttr.AttributeValue);

        // Should NOT have data-valmsg-for
        var forAttr = frames.Array.Any(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "data-valmsg-for");
        Assert.False(forAttr);
    }

    [Fact]
    public async Task RendersSpanEvenWithNoMessages_WhenServiceIsPresent()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var service = CreateService();
        editContext.Properties[ClientSideValidator.ServiceKey] = service;

        var testRenderer = new TestRenderer();
        var host = new ValidationMessageHostComponent<string>
        {
            EditContext = editContext,
            For = () => model.Name,
        };

        var hostId = testRenderer.AssignRootComponentId(host);
        await testRenderer.RenderRootComponentAsync(hostId);

        var batch = testRenderer.Batches.Single();
        var validationMsgComponentId = batch.GetComponentFrames<ValidationMessage<string>>().Single().ComponentId;
        var frames = testRenderer.GetCurrentRenderTreeFrames(validationMsgComponentId);

        // Even with no messages, a <span> container should be rendered for the JS library
        var spanFrame = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Element && f.ElementName == "span");
        Assert.Equal("span", spanFrame.ElementName);
    }

    private static IClientValidationService CreateService()
    {
        return new TestClientValidationService();
    }

    private class TestModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = "";
    }

    /// <summary>
    /// Host component that cascades EditContext and renders a ValidationMessage.
    /// </summary>
    private class ValidationMessageHostComponent<TValue> : AutoRenderComponent
    {
        public EditContext EditContext { get; set; } = default!;
        public System.Linq.Expressions.Expression<Func<TValue>>? For { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<EditContext>>(0);
            builder.AddComponentParameter(1, "Value", EditContext);
            builder.AddComponentParameter(2, "ChildContent", new RenderFragment(childBuilder =>
            {
                childBuilder.OpenComponent<ValidationMessage<TValue>>(0);
                childBuilder.AddComponentParameter(1, "For", For);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }
}
