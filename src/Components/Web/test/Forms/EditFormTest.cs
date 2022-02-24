// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Forms;

public class EditFormTest
{

    [Fact]
    public async Task ThrowsIfBothEditContextAndModelAreSupplied()
    {
        // Arrange
        var editForm = new EditForm
        {
            EditContext = new EditContext(new TestModel()),
            Model = new TestModel()
        };
        var testRenderer = new TestRenderer();
        var componentId = testRenderer.AssignRootComponentId(editForm);

        // Act/Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => testRenderer.RenderRootComponentAsync(componentId));
        Assert.StartsWith($"{nameof(EditForm)} requires a {nameof(EditForm.Model)} parameter, or an {nameof(EditContext)} parameter, but not both.", ex.Message);
    }

    [Fact]
    public async Task ThrowsIfBothEditContextAndModelAreNull()
    {
        // Arrange
        var editForm = new EditForm();
        var testRenderer = new TestRenderer();
        var componentId = testRenderer.AssignRootComponentId(editForm);

        // Act/Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => testRenderer.RenderRootComponentAsync(componentId));
        Assert.StartsWith($"{nameof(EditForm)} requires either a {nameof(EditForm.Model)} parameter, or an {nameof(EditContext)} parameter, please provide one of these.", ex.Message);
    }

    [Fact]
    public async Task ReturnsEditContextWhenModelParameterUsed()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestEditFormHostComponent
        {
            Model = model
        };
        var editFormComponent = await RenderAndGetTestEditFormComponentAsync(rootComponent);

        // Act
        var returnedEditContext = editFormComponent.EditContext;

        // Assert
        Assert.NotNull(returnedEditContext);
        Assert.Same(model, returnedEditContext.Model);
    }

    [Fact]
    public async Task ReturnsEditContextWhenEditContextParameterUsed()
    {
        // Arrange
        var editContext = new EditContext(new TestModel());
        var rootComponent = new TestEditFormHostComponent
        {
            EditContext = editContext
        };
        var editFormComponent = await RenderAndGetTestEditFormComponentAsync(rootComponent);

        // Act
        var returnedEditContext = editFormComponent.EditContext;

        // Assert
        Assert.Same(editContext, returnedEditContext);
    }

    private static EditForm FindEditFormComponent(CapturedBatch batch)
        => batch.ReferenceFrames
                .Where(f => f.FrameType == RenderTreeFrameType.Component)
                .Select(f => f.Component)
                .OfType<EditForm>()
                .Single();

    private static async Task<EditForm> RenderAndGetTestEditFormComponentAsync(TestEditFormHostComponent hostComponent)
    {
        var testRenderer = new TestRenderer();
        var componentId = testRenderer.AssignRootComponentId(hostComponent);
        await testRenderer.RenderRootComponentAsync(componentId);
        return FindEditFormComponent(testRenderer.Batches.Single());
    }

    class TestModel
    {
        public string StringProperty { get; set; }
    }

    class TestEditFormHostComponent : AutoRenderComponent
    {
        public EditContext EditContext { get; set; }
        public TestModel Model { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<EditForm>(0);
            builder.AddAttribute(1, "Model", Model);
            builder.AddAttribute(2, "EditContext", EditContext);
            builder.CloseComponent();
        }
    }
}
