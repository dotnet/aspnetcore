// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Forms;

public class ValidationSummaryClientValidationTest
{
    [Fact]
    public async Task RendersContainerWithDataValmsgSummary_WhenServiceIsPresent()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        editContext.Properties[ClientSideValidator.ServiceKey] = new object();

        var testRenderer = new TestRenderer();
        var host = new ValidationSummaryHostComponent
        {
            EditContext = editContext,
        };

        var hostId = testRenderer.AssignRootComponentId(host);
        await testRenderer.RenderRootComponentAsync(hostId);

        var batch = testRenderer.Batches.Single();
        var componentId = batch.GetComponentFrames<ValidationSummary>().Single().ComponentId;
        var frames = testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Should always render <div data-valmsg-summary="true"><ul>...</ul></div>
        var divFrame = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Element && f.ElementName == "div");
        Assert.Equal("div", divFrame.ElementName);

        var summaryAttr = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "data-valmsg-summary");
        Assert.Equal("true", summaryAttr.AttributeValue);

        var ulFrame = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Element && f.ElementName == "ul");
        Assert.Equal("ul", ulFrame.ElementName);
    }

    [Fact]
    public async Task DoesNotRenderContainer_WhenServiceIsNotPresent_AndNoErrors()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);

        var testRenderer = new TestRenderer();
        var host = new ValidationSummaryHostComponent
        {
            EditContext = editContext,
        };

        var hostId = testRenderer.AssignRootComponentId(host);
        await testRenderer.RenderRootComponentAsync(hostId);

        var batch = testRenderer.Batches.Single();
        var componentId = batch.GetComponentFrames<ValidationSummary>().Single().ComponentId;
        var frames = testRenderer.GetCurrentRenderTreeFrames(componentId);

        // With no messages and no service, nothing should render
        var anyElement = frames.Array.Any(f => f.FrameType == RenderTreeFrameType.Element);
        Assert.False(anyElement);
    }

    [Fact]
    public async Task RendersOriginalUl_WhenServiceIsNotPresent_WithErrors()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);

        var msgStore = new ValidationMessageStore(editContext);
        msgStore.Add(editContext.Field("Name"), "Error 1");

        var testRenderer = new TestRenderer();
        var host = new ValidationSummaryHostComponent
        {
            EditContext = editContext,
        };

        var hostId = testRenderer.AssignRootComponentId(host);
        await testRenderer.RenderRootComponentAsync(hostId);

        var batch = testRenderer.Batches.Single();
        var componentId = batch.GetComponentFrames<ValidationSummary>().Single().ComponentId;
        var frames = testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Should render <ul class="validation-errors"> directly, no wrapping div
        var ulFrame = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Element && f.ElementName == "ul");
        Assert.Equal("ul", ulFrame.ElementName);

        // Should NOT have data-valmsg-summary
        var hasSummaryAttr = frames.Array.Any(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "data-valmsg-summary");
        Assert.False(hasSummaryAttr);
    }

    private class TestModel
    {
        public string Name { get; set; } = "";
    }

    private class ValidationSummaryHostComponent : AutoRenderComponent
    {
        public EditContext EditContext { get; set; } = default!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<EditContext>>(0);
            builder.AddComponentParameter(1, "Value", EditContext);
            builder.AddComponentParameter(2, "ChildContent", new RenderFragment(childBuilder =>
            {
                childBuilder.OpenComponent<ValidationSummary>(0);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }
}
