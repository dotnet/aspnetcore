// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Test;

public class RendererDisposalTest
{
    [Fact]
    public void DisposedRenderer_ReleasesComponentWithoutEventHandler()
    {
        VerifyComponentReleased(includeHandler: false);
    }

    [Fact]
    public void DisposedRenderer_ReleasesComponentWithEventHandler()
    {
        VerifyComponentReleased(includeHandler: true);
    }

    [Fact]
    public void DisposedRenderer_ReleasesComponentWithFixedCascadingValue()
    {
        VerifyCascadingComponentReleased(isFixed: true);
    }

    [Fact]
    public void DisposedRenderer_ReleasesComponentWithNonFixedCascadingValue()
    {
        VerifyCascadingComponentReleased(isFixed: false);
    }

    [Fact]
    public async Task DisposedRenderer_ClearsEventHandlerReplacementsBeforeDisplayCompletes()
    {
        using var renderer = new TestRenderer();
        var component = new ReplacingEventHandlerComponent();
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);
        var oldHandlerId = renderer.Batches.Single().ReferenceFrames
            .Single(frame => frame.FrameType == RenderTreeFrameType.Attribute)
            .AttributeEventHandlerId;

        var displayCompletion = new TaskCompletionSource();
        renderer.NextRenderResultTask = displayCompletion.Task;

        try
        {
            await renderer.Dispatcher.InvokeAsync(component.ReplaceHandler);

            var newHandlerId = renderer.Batches[1].ReferenceFrames
                .Single(frame => frame.FrameType == RenderTreeFrameType.Attribute)
                .AttributeEventHandlerId;
            Assert.NotEqual(0UL, oldHandlerId);
            Assert.NotEqual(oldHandlerId, newHandlerId);

            // Replacement IDs are private bookkeeping, with no public count to inspect.
            var field = typeof(Renderer).GetField("_eventHandlerIdReplacements", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            var replacements = Assert.IsType<Dictionary<ulong, ulong>>(field.GetValue(renderer));
            Assert.Equal(newHandlerId, Assert.Single(replacements).Value);
            Assert.True(replacements.ContainsKey(oldHandlerId));
            Assert.False(displayCompletion.Task.IsCompleted);

            renderer.Dispose();

            Assert.Empty(replacements);
            Assert.False(displayCompletion.Task.IsCompleted);
        }
        finally
        {
            displayCompletion.SetResult();
        }
    }

    private static void VerifyCascadingComponentReleased(bool isFixed)
    {
        var (source, componentReference) = RenderAndDisposeCascadingComponent(isFixed);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var componentStillAlive = componentReference.IsAlive;

        GC.KeepAlive(source);

        Assert.False(
            componentStillAlive,
            "The cascading value source still retains the disposed component.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (
        CascadingValueSource<string> Source,
        WeakReference ComponentReference)
        RenderAndDisposeCascadingComponent(bool isFixed)
    {
        var source = new CascadingValueSource<string>("initial", isFixed);
        var services = new ServiceCollection();
        services.AddCascadingValue<string>(_ => source);

        using var provider = services.BuildServiceProvider();
        var renderer = new TestRenderer(provider);
        var component = new CascadingReproComponent();
        var componentReference = new WeakReference(component);

        var componentId = renderer.Dispatcher.InvokeAsync(
            () => renderer.AssignRootComponentId(component))
            .GetAwaiter().GetResult();

        renderer.RenderRootComponent(componentId);

        Assert.Equal("initial", component.Value);

        if (!isFixed)
        {
            source.NotifyChangedAsync("updated").GetAwaiter().GetResult();

            Assert.Equal("updated", component.Value);
            Assert.Equal(2, renderer.Batches.Count);
        }

        renderer.Dispose();

        Assert.True(component.Disposed);

        renderer.Batches.Clear();

        return (source, componentReference);
    }

    private static void VerifyComponentReleased(bool includeHandler)
    {
        var (renderer, componentReference) = RenderAndDispose(includeHandler);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var componentStillAlive = componentReference.IsAlive;

        GC.KeepAlive(renderer);

        Assert.False(
            componentStillAlive,
            "The disposed renderer still retains the component.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (TestRenderer Renderer, WeakReference ComponentReference)
        RenderAndDispose(bool includeHandler)
    {
        var renderer = new TestRenderer();
        var component = new ReproComponent
        {
            IncludeHandler = includeHandler
        };
        var componentReference = new WeakReference(component);

        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);

        if (includeHandler)
        {
            Assert.Contains(
                renderer.Batches.Single().ReferenceFrames,
                frame => frame.FrameType == RenderTreeFrameType.Attribute
                    && frame.AttributeEventHandlerId != 0);
        }

        renderer.Dispose();

        Assert.True(component.Disposed);

        // Captured test batches also retain delegates; exclude that test-only root.
        renderer.Batches.Clear();

        return (renderer, componentReference);
    }

    private sealed class ReplacingEventHandlerComponent : ComponentBase
    {
        private Action _handler = () => { };

        public void ReplaceHandler()
        {
            _handler = () => GC.KeepAlive(this);
            StateHasChanged();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "onclick", EventCallback.Factory.Create(this, _handler));
            builder.CloseElement();
        }
    }

    private sealed class CascadingReproComponent : ComponentBase, IDisposable
    {
        [CascadingParameter]
        public string Value { get; set; } = string.Empty;

        public bool Disposed { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, Value);
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }

    private sealed class ReproComponent : ComponentBase, IDisposable
    {
        public bool IncludeHandler { get; init; }

        public bool Disposed { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");

            if (IncludeHandler)
            {
                builder.AddAttribute(
                    1,
                    "onclick",
                    EventCallback.Factory.Create(this, HandleClick));
            }

            builder.AddContent(2, "Click");
            builder.CloseElement();
        }

        private void HandleClick()
        {
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}
