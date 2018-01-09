// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Blazor.Components;
using Microsoft.Blazor.Rendering;
using Microsoft.Blazor.RenderTree;
using Xunit;

namespace Microsoft.Blazor.Test
{
    public class RendererTest
    {
        [Fact]
        public void ComponentsAreNotPinnedInMemory()
        {
            // It's important that the Renderer base class does not itself pin in memory
            // any of the component instances that were attached to it (or by extension,
            // their descendants). This is because as the set of active components changes
            // over time, we need the GC to be able to release unused ones, and there isn't
            // any other mechanism for explicitly destroying components when they stop
            // being referenced.
            var renderer = new NoOpRenderer();

            AssertCanBeCollected(() =>
            {
                var component = new TestComponent(null);
                renderer.AssignComponentId(component);
                return component;
            });
        }

        [Fact]
        public void CannotRenderComponentsIfGCed()
        {
            // Arrange
            var renderer = new NoOpRenderer();

            // Act
            var componentId = new Func<int>(() =>
            {
                var component = new TestComponent(builder =>
                    throw new NotImplementedException("Should not be invoked"));

                return renderer.AssignComponentId(component);
            })();

            // Since there are no unit test references to 'component' here, the GC
            // should be able to collect it
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // Assert
            Assert.ThrowsAny<Exception>(() =>
            {
                renderer.RenderComponent(componentId);
            });
        }

        [Fact]
        public void CanRenderComponentsIfNotGCed()
        {
            // Arrange
            var renderer = new NoOpRenderer();
            var didRender = false;

            // Act
            var component = new TestComponent(builder =>
            {
                didRender = true;
            });
            var componentId = renderer.AssignComponentId(component);

            // Unlike the preceding test, we still have a reference to the component
            // instance on the stack here, so the following should not cause it to
            // be collected. Then when we call RenderComponent, there should be no error.
            GC.Collect();
            GC.WaitForPendingFinalizers();

            renderer.RenderComponent(componentId);

            // Assert
            Assert.True(didRender);
        }

        private class NoOpRenderer : Renderer
        {
            public new int AssignComponentId(IComponent component)
                => base.AssignComponentId(component);

            public new void RenderComponent(int componentId)
                => base.RenderComponent(componentId);

            protected override void UpdateDisplay(int componentId, ArraySegment<RenderTreeNode> renderTree)
            {
            }
        }

        private class TestComponent : IComponent
        {
            private Action<RenderTreeBuilder> _renderAction;

            public TestComponent(Action<RenderTreeBuilder> renderAction)
            {
                _renderAction = renderAction;
            }

            public void BuildRenderTree(RenderTreeBuilder builder)
                => _renderAction(builder);
        }

        void AssertCanBeCollected(Func<object> targetFactory)
        {
            // We have to construct the WeakReference in a separate scope
            // otherwise its target won't be collected on this GC cycle
            var weakRef = new Func<WeakReference>(
                () => new WeakReference(targetFactory()))();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.Null(weakRef.Target);
        }
    }
}
