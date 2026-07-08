// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Forms;

public class ValidationMessageTest
{
    [Fact]
    public async Task DoesNotRerenderForValidationStateChangesOfOtherFields()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var hostComponent = new TestValidationMessageHostComponent
        {
            EditContext = editContext,
            For = () => model.StringProperty,
        };
        var renderer = new TestRenderer();
        var rootComponentId = renderer.AssignRootComponentId(hostComponent);
        await renderer.RenderRootComponentAsync(rootComponentId);

        // The initial render must have produced exactly one batch.
        Assert.Single(renderer.Batches);

        // notify a field-specific validation state change for a different field.
        var otherFieldIdentifier = FieldIdentifier.Create(() => model.DateProperty);
        await renderer.Dispatcher.InvokeAsync(() => editContext.NotifyValidationStateChanged(otherFieldIdentifier));

        // the ValidationMessage was not re-rendered because the change is for a different field.
        Assert.Single(renderer.Batches);
    }

    [Fact]
    public async Task RerendersForFormLevelValidationStateChanges()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var hostComponent = new TestValidationMessageHostComponent
        {
            EditContext = editContext,
            For = () => model.StringProperty,
        };
        var renderer = new TestRenderer();
        var rootComponentId = renderer.AssignRootComponentId(hostComponent);
        await renderer.RenderRootComponentAsync(rootComponentId);

        Assert.Single(renderer.Batches);

        await renderer.Dispatcher.InvokeAsync(() => editContext.NotifyValidationStateChanged());

        Assert.Equal(2, renderer.Batches.Count);
    }

    [Fact]
    public async Task RerendersForValidationStateChangesOfOwnField()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var hostComponent = new TestValidationMessageHostComponent
        {
            EditContext = editContext,
            For = () => model.StringProperty,
        };
        var renderer = new TestRenderer();
        var rootComponentId = renderer.AssignRootComponentId(hostComponent);
        await renderer.RenderRootComponentAsync(rootComponentId);

        Assert.Single(renderer.Batches);

        var fieldIdentifier = FieldIdentifier.Create(() => model.StringProperty);
        await renderer.Dispatcher.InvokeAsync(() => editContext.NotifyValidationStateChanged(fieldIdentifier));

        Assert.Equal(2, renderer.Batches.Count);
    }

    private class TestModel
    {
        public string StringProperty { get; set; }

        public DateTime DateProperty { get; set; }
    }

    private class TestValidationMessageHostComponent : AutoRenderComponent
    {
        public EditContext EditContext { get; set; }

        public Expression<Func<string>> For { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<EditContext>>(0);
            builder.AddComponentParameter(1, "Value", EditContext);
            builder.AddComponentParameter(2, "ChildContent", new RenderFragment(childBuilder =>
            {
                childBuilder.OpenComponent<ValidationMessage<string>>(0);
                childBuilder.AddComponentParameter(1, "For", For);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }
}
