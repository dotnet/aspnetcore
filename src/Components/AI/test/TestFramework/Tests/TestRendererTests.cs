// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI.Tests.TestFramework;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.AI.Tests.TestFramework.Tests;

public class TestRendererTests
{
    [Fact]
    public void RenderComponent_SimpleElement_ProducesHtml()
    {
        using var renderer = new TestRenderer();

        var cut = renderer.RenderComponent<SimpleDiv>();

        Assert.Equal("<div class=\"hello\">Hello world</div>", cut.GetHtml());
    }

    [Fact]
    public void RenderComponent_NestedComponents_BuildsTree()
    {
        using var renderer = new TestRenderer();

        var cut = renderer.RenderComponent<ParentComponent>();
        var node = cut.GetNode();

        Assert.Single(node.Children);
        Assert.Equal(typeof(ChildComponent), node.Children[0].ComponentType);
    }

    [Fact]
    public void RenderComponent_FindComponent_FindsDescendant()
    {
        using var renderer = new TestRenderer();

        var cut = renderer.RenderComponent<ParentComponent>();
        var child = cut.FindComponent<ChildComponent>();

        Assert.NotNull(child);
        Assert.Contains("child-content", child.GetHtml());
    }

    [Fact]
    public void RenderComponent_WithParameters_PassesValues()
    {
        using var renderer = new TestRenderer();

        var cut = renderer.RenderComponent<ParameterizedComponent>(p =>
        {
            p["Title"] = "Test Title";
        });

        Assert.Contains("Test Title", cut.GetHtml());
    }

    [Fact]
    public void RenderComponent_CascadingValue_FlowsToDescendants()
    {
        using var renderer = new TestRenderer();

        var cut = renderer.RenderComponent<CascadingParent>();
        var html = cut.GetHtml();

        Assert.Contains("Received: cascaded-value", html);
    }

    [Fact]
    public async Task RenderComponent_ReRender_CapturesMultipleBatches()
    {
        using var renderer = new TestRenderer();

        var component = new CounterComponent();
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);

        Assert.Single(renderer.Batches);
        var node = renderer.Tree.GetNode(componentId);
        Assert.Single(node.Renders);
        Assert.Contains("Count: 0", node.Renders[0].Html);

        // Trigger a re-render with updated state
        await renderer.Dispatcher.InvokeAsync(() =>
        {
            component.Increment();
        });

        Assert.Equal(2, renderer.Batches.Count);
        Assert.Equal(2, node.RenderCount);
        Assert.Contains("Count: 1", node.Renders[1].Html);
    }

    [Fact]
    public void RenderComponent_BooleanAttribute_RendersCorrectly()
    {
        using var renderer = new TestRenderer();

        var cut = renderer.RenderComponent<DisabledInput>();

        Assert.Equal("<input disabled />", cut.GetHtml());
    }

    [Fact]
    public void RenderComponent_VoidElements_SelfClose()
    {
        using var renderer = new TestRenderer();

        var cut = renderer.RenderComponent<VoidElementComponent>();
        var html = cut.GetHtml();

        Assert.Contains("<br />", html);
        Assert.Contains("<hr />", html);
        Assert.DoesNotContain("</br>", html);
        Assert.DoesNotContain("</hr>", html);
    }

    // Test components

    private class SimpleDiv : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "hello");
            builder.AddContent(2, "Hello world");
            builder.CloseElement();
        }
    }

    private class ParentComponent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "parent");
            builder.OpenComponent<ChildComponent>(2);
            builder.CloseComponent();
            builder.CloseElement();
        }
    }

    private class ChildComponent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "child-content");
            builder.AddContent(2, "I am a child");
            builder.CloseElement();
        }
    }

    private class ParameterizedComponent : ComponentBase
    {
        [Parameter]
        public string Title { get; set; } = "";

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "h1");
            builder.AddContent(1, Title);
            builder.CloseElement();
        }
    }

    private class CascadingParent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<string>>(0);
            builder.AddComponentParameter(1, "Value", "cascaded-value");
            builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenComponent<CascadingChild>(0);
                inner.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    private class CascadingChild : ComponentBase
    {
        [CascadingParameter]
        public string? CascadedValue { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "p");
            builder.AddContent(1, $"Received: {CascadedValue}");
            builder.CloseElement();
        }
    }

    private class CounterComponent : ComponentBase
    {
        private int _count;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddContent(1, $"Count: {_count}");
            builder.CloseElement();
        }

        public void Increment()
        {
            _count++;
            StateHasChanged();
        }
    }

    private class DisabledInput : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "input");
            builder.AddAttribute(1, "disabled", true);
            builder.CloseElement();
        }
    }

    private class VoidElementComponent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.OpenElement(1, "br");
            builder.CloseElement();
            builder.OpenElement(2, "hr");
            builder.CloseElement();
            builder.CloseElement();
        }
    }
}
