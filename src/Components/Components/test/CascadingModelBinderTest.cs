// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Binding;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components;

public class CascadingModelBinderTest
{
    [Fact]
    public void CascadingModelBinder_UsesBindingIdWhenNoDefaultName()
    {
        ModelBindingContext capturedContext = null;
        RenderFragment<ModelBindingContext> contents = (ctx) => b => { capturedContext = ctx; };

        var renderer = new TestRenderer();
        var testComponent = new TestComponent(builder =>
        {
            builder.OpenComponent<CascadingModelBinder>(0);
            builder.AddAttribute(1, nameof(CascadingModelBinder.BindingContextId), "/path");
            builder.AddAttribute(2, nameof(CascadingModelBinder.ChildContent), contents);
            builder.CloseComponent();
        });
        var id = renderer.AssignRootComponentId(testComponent);

        // Act
        renderer.RenderRootComponent(id);

        // Assert
        Assert.NotNull(capturedContext);
        Assert.Null(capturedContext.Name);
        Assert.Equal("/path", capturedContext.BindingContextId);
    }

    [Fact]
    public void CascadingModelBinder_CanProvideName()
    {
        ModelBindingContext capturedContext = null;
        RenderFragment<ModelBindingContext> contents = (ctx) => b => { capturedContext = ctx; };

        var renderer = new TestRenderer();
        var testComponent = new TestComponent(builder =>
        {
            builder.OpenComponent<CascadingModelBinder>(0);
            builder.AddAttribute(1, nameof(CascadingModelBinder.Name), "named-context");
            builder.AddAttribute(2, nameof(CascadingModelBinder.ChildContent), contents);
            builder.CloseComponent();
        });
        var id = renderer.AssignRootComponentId(testComponent);

        // Act
        renderer.RenderRootComponent(id);

        // Assert
        Assert.NotNull(capturedContext);
        Assert.Equal("named-context", capturedContext.Name);
        Assert.Equal("named-context", capturedContext.BindingContextId);
    }

    [Fact]
    public void CascadingModelBinder_CanNestNamedContexts()
    {
        ModelBindingContext capturedContext = null;
        RenderFragment<ModelBindingContext> contents = (ctx) => b => { capturedContext = ctx; };
        RenderFragment<ModelBindingContext> nested = (ctx) => b =>
        {
            b.OpenComponent<CascadingModelBinder>(0);
            b.AddAttribute(1, nameof(CascadingModelBinder.Name), "child-context");
            b.AddAttribute(2, nameof(CascadingModelBinder.ChildContent), contents);
            b.CloseComponent();
        };

        var renderer = new TestRenderer();
        var testComponent = new TestComponent(builder =>
        {
            builder.OpenComponent<CascadingModelBinder>(0);
            builder.AddAttribute(1, nameof(CascadingModelBinder.Name), "parent-context");
            builder.AddAttribute(2, nameof(CascadingModelBinder.ChildContent), nested);
            builder.CloseComponent();
        });
        var id = renderer.AssignRootComponentId(testComponent);

        // Act
        renderer.RenderRootComponent(id);

        // Assert
        Assert.NotNull(capturedContext);
        Assert.Equal("parent-context.child-context", capturedContext.Name);
        Assert.Equal("parent-context.child-context", capturedContext.BindingContextId);
    }

    [Fact]
    public void CascadingModelBinder_CanNestWithDefaultContext()
    {
        ModelBindingContext capturedContext = null;
        RenderFragment<ModelBindingContext> contents = (ctx) => b => { capturedContext = ctx; };
        RenderFragment<ModelBindingContext> nested = (ctx) => b =>
        {
            b.OpenComponent<CascadingModelBinder>(0);
            b.AddAttribute(1, nameof(CascadingModelBinder.Name), "child-context");
            b.AddAttribute(2, nameof(CascadingModelBinder.ChildContent), contents);
            b.CloseComponent();
        };

        var renderer = new TestRenderer();
        var testComponent = new TestComponent(builder =>
        {
            builder.OpenComponent<CascadingModelBinder>(0);
            builder.AddAttribute(1, nameof(CascadingModelBinder.BindingContextId), "/path");
            builder.AddAttribute(2, nameof(CascadingModelBinder.ChildContent), nested);
            builder.CloseComponent();
        });
        var id = renderer.AssignRootComponentId(testComponent);

        // Act
        renderer.RenderRootComponent(id);

        // Assert
        Assert.NotNull(capturedContext);
        Assert.Equal("child-context", capturedContext.Name);
        Assert.Equal("child-context", capturedContext.BindingContextId);
    }

    [Fact]
    public void Throws_IfDefaultContextIsNotTheRoot()
    {
        ModelBindingContext capturedContext = null;
        RenderFragment<ModelBindingContext> contents = (ctx) => b => { capturedContext = ctx; };
        RenderFragment<ModelBindingContext> nested = (ctx) => b =>
        {
            b.OpenComponent<CascadingModelBinder>(0);
            b.AddAttribute(1, nameof(CascadingModelBinder.BindingContextId), "/path");
            b.AddAttribute(2, nameof(CascadingModelBinder.ChildContent), contents);
            b.CloseComponent();
        };

        var renderer = new TestRenderer();
        var testComponent = new TestComponent(builder =>
        {
            builder.OpenComponent<CascadingModelBinder>(0);
            builder.AddAttribute(1, nameof(CascadingModelBinder.Name), "parent-context");
            builder.AddAttribute(2, nameof(CascadingModelBinder.ChildContent), nested);
            builder.CloseComponent();
        });
        var id = renderer.AssignRootComponentId(testComponent);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => renderer.RenderRootComponent(id));
        Assert.Equal($"Nested binding contexts must define a Name. (Parent context) = 'parent-context'.", exception.Message);
    }

    class TestComponent : AutoRenderComponent
    {
        private readonly RenderFragment _renderFragment;

        public TestComponent(RenderFragment renderFragment)
        {
            _renderFragment = renderFragment;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
            => _renderFragment(builder);
    }
}
