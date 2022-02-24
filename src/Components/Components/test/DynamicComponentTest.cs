// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components;

public class DynamicComponentTest
{
    [Fact]
    public void RejectsUnknownParameters()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            var parameters = new Dictionary<string, object>
            {
                    { "unknownparameter", 123 }
            };
            _ = new DynamicComponent().SetParametersAsync(ParameterView.FromDictionary(parameters));
        });

        Assert.StartsWith(
            $"{ nameof(DynamicComponent)} does not accept a parameter with the name 'unknownparameter'.",
            ex.Message);
    }

    [Fact]
    public void RequiresTypeParameter()
    {
        var instance = new DynamicComponent();
        var renderer = new TestRenderer();
        var componentId = renderer.AssignRootComponentId(instance);

        var ex = Assert.Throws<InvalidOperationException>(
            () => renderer.RenderRootComponent(componentId, ParameterView.Empty));

        Assert.StartsWith(
            $"{ nameof(DynamicComponent)} requires a non-null value for the parameter {nameof(DynamicComponent.Type)}.",
            ex.Message);
    }

    [Fact]
    public void CanRenderComponentByType()
    {
        // Arrange
        var instance = new DynamicComponent();
        var renderer = new TestRenderer();
        var componentId = renderer.AssignRootComponentId(instance);
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(DynamicComponent.Type), typeof(TestComponent) },
            });

        // Act
        renderer.RenderRootComponent(componentId, parameters);

        // Assert
        var batch = renderer.Batches.Single();
        AssertFrame.Component<TestComponent>(batch.ReferenceFrames[0], 2, 0);
        AssertFrame.Text(batch.ReferenceFrames[2], "Hello from TestComponent with IntProp=0", 0);
    }

    [Fact]
    public void CanRenderComponentByTypeWithParameters()
    {
        // Arrange
        var instance = new DynamicComponent();
        var renderer = new TestRenderer();
        var childParameters = new Dictionary<string, object>
            {
                { nameof(TestComponent.IntProp), 123 },
                { nameof(TestComponent.ChildContent), (RenderFragment)(builder =>
                {
                    builder.AddContent(0, "This is some child content");
                })},
            };
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(DynamicComponent.Type), typeof(TestComponent) },
                { nameof(DynamicComponent.Parameters), childParameters },
            });

        // Act
        renderer.RenderRootComponent(
            renderer.AssignRootComponentId(instance),
            parameters);

        // Assert
        var batch = renderer.Batches.Single();

        // It renders a reference to the child component with its parameters
        AssertFrame.Component<TestComponent>(batch.ReferenceFrames[0], 4, 0);
        AssertFrame.Attribute(batch.ReferenceFrames[1], nameof(TestComponent.IntProp), 123, 1);
        AssertFrame.Attribute(batch.ReferenceFrames[2], nameof(TestComponent.ChildContent), 1);

        // The child component itself is rendered
        AssertFrame.Text(batch.ReferenceFrames[4], "Hello from TestComponent with IntProp=123", 0);
        AssertFrame.Text(batch.ReferenceFrames[5], "This is some child content", 0);
    }

    [Fact]
    public void CanAccessToChildComponentInstance()
    {
        // Arrange
        var instance = new DynamicComponent();
        var renderer = new TestRenderer();
        var childParameters = new Dictionary<string, object>
            {
                { nameof(TestComponent.IntProp), 123 },
            };
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(DynamicComponent.Type), typeof(TestComponent) },
                { nameof(DynamicComponent.Parameters), childParameters },
            });

        // Act
        renderer.RenderRootComponent(
            renderer.AssignRootComponentId(instance),
            parameters);

        // Assert
        Assert.IsType<TestComponent>(instance.Instance);
        Assert.Equal(123, ((TestComponent)instance.Instance).IntProp);
    }

    private class TestComponent : AutoRenderComponent
    {
        [Parameter] public int IntProp { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, $"Hello from TestComponent with IntProp={IntProp}");
            builder.AddContent(1, ChildContent);
        }
    }
}
