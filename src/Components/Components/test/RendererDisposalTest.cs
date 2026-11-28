// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;

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
    public void DisposedRenderer_DisposesComponentWhenCascadingSubscriptionDisposalThrows()
    {
        var expectedException = new InvalidOperationException("Subscription disposal failed.");
        var subscription = new ThrowingCascadingSubscription(expectedException);
        var services = new ServiceCollection();
        services.TryAddCascadingValueSupplier<TestCascadingParameterAttribute>(
            _ => (_, _, _) => subscription);
        using var provider = services.BuildServiceProvider();
        using var renderer = new TestRenderer(provider) { ShouldHandleExceptions = true };
        var component = new ComponentWithCustomCascadingSubscription();
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);
        Assert.Equal("initial", component.Value);
        Assert.False(subscription.Disposed);
        Assert.False(component.Disposed);

        renderer.Dispose();

        Assert.True(subscription.Disposed);
        Assert.Same(expectedException, Assert.Single(renderer.HandledExceptions));
        Assert.True(component.Disposed);
    }

    [Fact]
    public async Task DisposedRenderer_ReportsSubscriptionAndAsyncComponentDisposalExceptions()
    {
        var subscriptionException = new InvalidOperationException("Subscription disposal failed.");
        var componentException = new InvalidOperationException("Component disposal failed.");
        var subscription = new ThrowingCascadingSubscription(subscriptionException);
        var services = new ServiceCollection();
        services.TryAddCascadingValueSupplier<TestCascadingParameterAttribute>(
            _ => (_, _, _) => subscription);
        using var provider = services.BuildServiceProvider();
        await using var renderer = new TestRenderer(provider) { ShouldHandleExceptions = true };
        var component = new AsyncDisposableComponentWithCustomCascadingSubscription(componentException);
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);
        Assert.Equal("initial", component.Value);

        await renderer.DisposeAsync();

        Assert.True(subscription.Disposed);
        Assert.True(component.Disposed);
        var exception = Assert.IsType<AggregateException>(Assert.Single(renderer.HandledExceptions));
        Assert.Collection(exception.InnerExceptions,
            exception => Assert.Same(subscriptionException, exception),
            exception => Assert.Same(componentException, exception));
    }

    [Fact]
    public void DisposedRenderer_DisposesAllCascadingSubscriptionsWhenDisposalThrows()
    {
        var firstException = new InvalidOperationException("First subscription disposal failed.");
        var secondException = new InvalidOperationException("Second subscription disposal failed.");
        var subscriptions = new[]
        {
            new ThrowingCascadingSubscription(firstException),
            new ThrowingCascadingSubscription(secondException),
        };
        var subscriptionIndex = 0;
        var services = new ServiceCollection();
        services.TryAddCascadingValueSupplier<TestCascadingParameterAttribute>(
            _ => (_, _, _) => subscriptions[subscriptionIndex++]);
        using var provider = services.BuildServiceProvider();
        using var renderer = new TestRenderer(provider) { ShouldHandleExceptions = true };
        var component = new ComponentWithTwoCustomCascadingSubscriptions();
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);
        Assert.Equal(2, subscriptionIndex);

        renderer.Dispose();

        Assert.All(subscriptions, subscription => Assert.True(subscription.Disposed));
        Assert.True(component.Disposed);
        var exception = Assert.IsType<AggregateException>(Assert.Single(renderer.HandledExceptions));
        Assert.Collection(exception.InnerExceptions,
            exception => Assert.Same(firstException, exception),
            exception => Assert.Same(secondException, exception));
    }

    [Fact]
    public async Task RemovingComponents_AggregatesSynchronousSubscriptionDisposalExceptionsAfterCleanup()
    {
        var firstException = new InvalidOperationException("First subscription disposal failed.");
        var secondException = new InvalidOperationException("Second subscription disposal failed.");
        var subscriptions = new Queue<ThrowingCascadingSubscription>(
        [
            new(firstException),
            new(secondException),
        ]);
        var services = new ServiceCollection();
        services.TryAddCascadingValueSupplier<TestCascadingParameterAttribute>(
            _ => (_, _, _) => subscriptions.Dequeue());
        using var provider = services.BuildServiceProvider();
        using var renderer = new TestRenderer(provider) { ShouldHandleExceptions = true };
        var parent = new SubscriptionParentComponent();
        var componentId = renderer.AssignRootComponentId(parent);
        renderer.RenderRootComponent(componentId);
        Assert.Equal(2, parent.Children.Count);
        var cleanupCompletedWhenReported = new List<bool>();
        renderer.OnExceptionHandled = () =>
            cleanupCompletedWhenReported.Add(parent.Children.All(child => child.Disposed));

        parent.ChildCount = 0;
        await renderer.RenderRootComponentAsync(componentId);

        var exception = Assert.IsType<AggregateException>(Assert.Single(renderer.HandledExceptions));
        Assert.Collection(exception.InnerExceptions,
            exception => Assert.Same(firstException, exception),
            exception => Assert.Same(secondException, exception));
        Assert.True(Assert.Single(cleanupCompletedWhenReported));
    }

    [Fact]
    public async Task DisposedRenderer_PropagatesSynchronousSubscriptionAndComponentDisposalExceptions()
    {
        var subscriptionException = new InvalidOperationException("Subscription disposal failed.");
        var componentException = new InvalidOperationException("Component disposal failed.");
        var services = new ServiceCollection();
        services.TryAddCascadingValueSupplier<TestCascadingParameterAttribute>(
            _ => (_, _, _) => new ThrowingCascadingSubscription(subscriptionException));
        using var provider = services.BuildServiceProvider();
        using var renderer = new TestRenderer(provider);
        var component = new ThrowingDisposableComponentWithCustomCascadingSubscription(componentException);
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);

        var exception = await renderer.Dispatcher.InvokeAsync(
            () => Assert.Throws<AggregateException>(renderer.Dispose));

        Assert.True(component.Disposed);
        Assert.Collection(exception.InnerExceptions,
            exception => Assert.Same(subscriptionException, exception),
            exception => Assert.Same(componentException, exception));
    }

    [Fact]
    public async Task RemovingComponent_UnsubscribesWhenComponentStateDisposalOmitsBaseCall()
    {
        var subscription = new Mock<CascadingParameterSubscription>();
        subscription.Setup(subscription => subscription.GetCurrentValue()).Returns("initial");
        var services = new ServiceCollection();
        services.TryAddCascadingValueSupplier<TestCascadingParameterAttribute>(
            _ => (_, _, _) => subscription.Object);
        using var provider = services.BuildServiceProvider();
        using var renderer = new CustomDisposalRenderer(provider);
        var parent = new SubscriptionParentComponent { ChildCount = 1 };
        var componentId = renderer.AssignRootComponentId(parent);
        renderer.RenderRootComponent(componentId);
        var child = Assert.Single(parent.Children);
        Assert.Equal("initial", child.Value);
        subscription.Verify(subscription => subscription.Dispose(), Times.Never);

        parent.ChildCount = 0;
        await renderer.RenderRootComponentAsync(componentId);

        Assert.True(child.Disposed);
        subscription.Verify(subscription => subscription.Dispose(), Times.Once);
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

    private sealed class TestCascadingParameterAttribute : CascadingParameterAttributeBase;

    private sealed class ThrowingCascadingSubscription(Exception exception) : CascadingParameterSubscription
    {
        public bool Disposed { get; private set; }

        public override object GetCurrentValue() => "initial";

        public override void Dispose()
        {
            Disposed = true;
            throw exception;
        }
    }

    private class ComponentWithCustomCascadingSubscription : ComponentBase, IDisposable
    {
        [TestCascadingParameter]
        public string Value { get; set; } = string.Empty;

        public bool Disposed { get; private set; }

        public void Dispose()
        {
            Disposed = true;
        }
    }

    private sealed class AsyncDisposableComponentWithCustomCascadingSubscription(Exception exception)
        : ComponentWithCustomCascadingSubscription, IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            Dispose();
            throw exception;
        }
    }

    private sealed class ComponentWithTwoCustomCascadingSubscriptions : ComponentWithCustomCascadingSubscription
    {
        [TestCascadingParameter]
        public string SecondValue { get; set; } = string.Empty;
    }

    private sealed class ThrowingDisposableComponentWithCustomCascadingSubscription(Exception exception)
        : ComponentWithCustomCascadingSubscription, IDisposable
    {
        void IDisposable.Dispose()
        {
            Dispose();
            throw exception;
        }
    }

    private sealed class SubscriptionParentComponent : ComponentBase
    {
        public int ChildCount { get; set; } = 2;

        public List<ComponentWithCustomCascadingSubscription> Children { get; } = new();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            for (var i = 0; i < ChildCount; i++)
            {
                builder.OpenComponent<ComponentWithCustomCascadingSubscription>(0);
                builder.AddComponentReferenceCapture(1,
                    component => Children.Add((ComponentWithCustomCascadingSubscription)component));
                builder.CloseComponent();
            }
        }
    }

    private sealed class CustomDisposalRenderer(IServiceProvider services) : TestRenderer(services)
    {
        protected override ComponentState CreateComponentState(int componentId, IComponent component, ComponentState parentComponentState)
            => component is ComponentWithCustomCascadingSubscription
                ? new CustomDisposalComponentState(this, componentId, component, parentComponentState)
                : base.CreateComponentState(componentId, component, parentComponentState);
    }

    private sealed class CustomDisposalComponentState(
        Renderer renderer, int componentId, IComponent component, ComponentState parentComponentState)
        : ComponentState(renderer, componentId, component, parentComponentState)
    {
        public override ValueTask DisposeAsync()
        {
            ((IDisposable)CurrentRenderTree).Dispose();
            ((IDisposable)Component).Dispose();
            return ValueTask.CompletedTask;
        }
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
