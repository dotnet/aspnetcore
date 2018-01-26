// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.Rendering;
using Microsoft.AspNetCore.Blazor.RenderTree;
using Microsoft.AspNetCore.Blazor.Test.Shared;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Test
{
    public class RendererTest
    {
        [Fact]
        public void CanRenderTopLevelComponents()
        {
            // Arrange
            var renderer = new TestRenderer();
            var component = new TestComponent(builder =>
            {
                builder.OpenElement(0, "my element");
                builder.AddText(1, "some text");
                builder.CloseElement();
            });

            // Act
            var componentId = renderer.AssignComponentId(component);
            renderer.RenderComponent(componentId);

            // Assert
            Assert.Collection(renderer.RenderTreesByComponentId[componentId],
                node => AssertNode.Element(node, "my element", 1),
                node => AssertNode.Text(node, "some text"));
        }

        [Fact]
        public void CanRenderNestedComponents()
        {
            // Arrange
            var renderer = new TestRenderer();
            var component = new TestComponent(builder =>
            {
                builder.AddText(0, "Hello");
                builder.OpenComponentElement<MessageComponent>(1);
                builder.CloseElement();
            });

            // Act/Assert
            var componentId = renderer.AssignComponentId(component);
            renderer.RenderComponent(componentId);
            var componentNode = renderer.RenderTreesByComponentId[componentId]
                .Single(node => node.NodeType == RenderTreeNodeType.Component);
            var nestedComponentId = componentNode.ComponentId;

            // The nested component exists
            Assert.IsType<MessageComponent>(componentNode.Component);
            ((MessageComponent)(componentNode.Component)).Message = "Nested component output";

            // It isn't rendered until the consumer asks for it to be
            Assert.False(renderer.RenderTreesByComponentId.ContainsKey(nestedComponentId));

            // It can be rendered
            renderer.RenderComponent(nestedComponentId);
            Assert.Collection(renderer.RenderTreesByComponentId[nestedComponentId],
                node => AssertNode.Text(node, "Nested component output"));
        }

        [Fact]
        public void CanReRenderTopLevelComponents()
        {
            // Arrange
            var renderer = new TestRenderer();
            var component = new MessageComponent { Message = "Initial message" };
            var componentId = renderer.AssignComponentId(component);

            // Act/Assert: first render
            renderer.RenderComponent(componentId);
            Assert.Collection(renderer.RenderTreesByComponentId[componentId],
                node => AssertNode.Text(node, "Initial message"));

            // Act/Assert: second render
            component.Message = "Modified message";
            renderer.RenderComponent(componentId);
            Assert.Collection(renderer.RenderTreesByComponentId[componentId],
                node => AssertNode.Text(node, "Modified message"));
        }

        [Fact]
        public void CanReRenderNestedComponents()
        {
            // Arrange: parent component already rendered
            var renderer = new TestRenderer();
            var parentComponent = new TestComponent(builder =>
            {
                builder.OpenComponentElement<MessageComponent>(0);
                builder.CloseElement();
            });
            var parentComponentId = renderer.AssignComponentId(parentComponent);
            renderer.RenderComponent(parentComponentId);
            var nestedComponentNode = renderer.RenderTreesByComponentId[parentComponentId]
                .Single(node => node.NodeType == RenderTreeNodeType.Component);
            var nestedComponent = (MessageComponent)nestedComponentNode.Component;
            var nestedComponentId = nestedComponentNode.ComponentId;

            // Act/Assert: inital render
            nestedComponent.Message = "Render 1";
            renderer.RenderComponent(nestedComponentId);
            Assert.Collection(renderer.RenderTreesByComponentId[nestedComponentId],
                node => AssertNode.Text(node, "Render 1"));

            // Act/Assert: re-render
            nestedComponent.Message = "Render 2";
            renderer.RenderComponent(nestedComponentId);
            Assert.Collection(renderer.RenderTreesByComponentId[nestedComponentId],
                node => AssertNode.Text(node, "Render 2"));
        }

        [Fact]
        public void CanDispatchEventsToTopLevelComponents()
        {
            // Arrange: Render a component with an event handler
            var renderer = new TestRenderer();
            UIEventArgs receivedArgs = null;

            var component = new EventComponent
            {
                Handler = args => { receivedArgs = args; }
            };
            var componentId = renderer.AssignComponentId(component);
            renderer.RenderComponent(componentId);

            var (eventHandlerNodeIndex, _) = FirstWithIndex(
                renderer.RenderTreesByComponentId[componentId],
                node => node.AttributeValue != null);

            // Assert: Event not yet fired
            Assert.Null(receivedArgs);

            // Act/Assert: Event can be fired
            var eventArgs = new UIEventArgs();
            renderer.DispatchEvent(componentId, eventHandlerNodeIndex, eventArgs);
            Assert.Same(eventArgs, receivedArgs);
        }

        [Fact]
        public void CanDispatchEventsToNestedComponents()
        {
            UIEventArgs receivedArgs = null;

            // Arrange: Render parent component
            var renderer = new TestRenderer();
            var parentComponent = new TestComponent(builder =>
            {
                builder.OpenComponentElement<EventComponent>(0);
                builder.CloseElement();
            });
            var parentComponentId = renderer.AssignComponentId(parentComponent);
            renderer.RenderComponent(parentComponentId);

            // Arrange: Render nested component
            var nestedComponentNode = renderer.RenderTreesByComponentId[parentComponentId]
                .Single(node => node.NodeType == RenderTreeNodeType.Component);
            var nestedComponent = (EventComponent)nestedComponentNode.Component;
            nestedComponent.Handler = args => { receivedArgs = args; };
            var nestedComponentId = nestedComponentNode.ComponentId;
            renderer.RenderComponent(nestedComponentId);

            // Find nested component's event handler ndoe
            var (eventHandlerNodeIndex, _) = FirstWithIndex(
                renderer.RenderTreesByComponentId[nestedComponentId],
                node => node.AttributeValue != null);

            // Assert: Event not yet fired
            Assert.Null(receivedArgs);

            // Act/Assert: Event can be fired
            var eventArgs = new UIEventArgs();
            renderer.DispatchEvent(nestedComponentId, eventHandlerNodeIndex, eventArgs);
            Assert.Same(eventArgs, receivedArgs);
        }

        [Fact]
        public void CannotRenderUnknownComponents()
        {
            // Arrange
            var renderer = new TestRenderer();

            // Act/Assert
            Assert.Throws<ArgumentException>(() =>
            {
                renderer.RenderComponent(123);
            });
        }

        [Fact]
        public void CannotDispatchEventsToUnknownComponents()
        {
            // Arrange
            var renderer = new TestRenderer();

            // Act/Assert
            Assert.Throws<ArgumentException>(() =>
            {
                renderer.DispatchEvent(123, 0, new UIEventArgs());
            });
        }

        [Fact]
        public void ComponentsCanBeAssociatedWithMultipleRenderers()
        {
            // Arrange
            var renderer1 = new TestRenderer();
            var renderer2 = new TestRenderer();
            var component = new MessageComponent { Message = "Hello, world!" };
            var renderer1ComponentId = renderer1.AssignComponentId(component);
            renderer2.AssignComponentId(new TestComponent(null)); // Just so they don't get the same IDs
            var renderer2ComponentId = renderer2.AssignComponentId(component);

            // Act/Assert: Render component in renderer1
            renderer1.RenderComponent(renderer1ComponentId);
            Assert.True(renderer1.RenderTreesByComponentId.ContainsKey(renderer1ComponentId));
            Assert.False(renderer2.RenderTreesByComponentId.ContainsKey(renderer2ComponentId));

            // Act/Assert: Render same component in renderer2
            renderer2.RenderComponent(renderer2ComponentId);
            Assert.True(renderer2.RenderTreesByComponentId.ContainsKey(renderer2ComponentId));
        }

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

        [Fact]
        public void PreservesChildComponentInstancesWithNoAttributes()
        {
            // Arrange: First render, capturing child component instance
            var renderer = new TestRenderer();
            var message = "Hello";
            var component = new TestComponent(builder =>
            {
                builder.AddText(0, message);
                builder.OpenComponentElement<MessageComponent>(1);
                builder.CloseElement();
            });

            var rootComponentId = renderer.AssignComponentId(component);
            renderer.RenderComponent(rootComponentId);

            var nestedComponentInstance = (MessageComponent)renderer.RenderTreesByComponentId[rootComponentId]
                .Single(node => node.NodeType == RenderTreeNodeType.Component)
                .Component;

            // Act: Second render
            message = "Modified message";
            renderer.RenderComponent(rootComponentId);

            // Assert
            Assert.Collection(renderer.RenderTreesByComponentId[rootComponentId],
                node => AssertNode.Text(node, "Modified message"),
                node => Assert.Same(nestedComponentInstance, node.Component));
        }

        [Fact]
        public void UpdatesPropertiesOnRetainedChildComponentInstances()
        {
            // Arrange: First render, capturing child component instance
            var renderer = new TestRenderer();
            var objectThatWillNotChange = new object();
            var firstRender = true;
            var component = new TestComponent(builder =>
            {
                builder.OpenComponentElement<FakeComponent>(1);
                builder.AddAttribute(2, nameof(FakeComponent.IntProperty), firstRender ? 123 : 256);
                builder.AddAttribute(3, nameof(FakeComponent.ObjectProperty), objectThatWillNotChange);
                builder.AddAttribute(4, nameof(FakeComponent.StringProperty), firstRender ? "String that will change" : "String that did change");
                builder.CloseElement();
            });

            var rootComponentId = renderer.AssignComponentId(component);
            renderer.RenderComponent(rootComponentId);

            var originalComponentInstance = (FakeComponent)renderer.RenderTreesByComponentId[rootComponentId]
                .Single(node => node.NodeType == RenderTreeNodeType.Component)
                .Component;

            // Assert 1: properties were assigned
            Assert.Equal(123, originalComponentInstance.IntProperty);
            Assert.Equal("String that will change", originalComponentInstance.StringProperty);
            Assert.Same(objectThatWillNotChange, originalComponentInstance.ObjectProperty);

            // Act: Second render
            firstRender = false;
            renderer.RenderComponent(rootComponentId);

            var updatedComponentInstance = (FakeComponent)renderer.RenderTreesByComponentId[rootComponentId]
                .Single(node => node.NodeType == RenderTreeNodeType.Component)
                .Component;

            // Assert
            Assert.Same(originalComponentInstance, updatedComponentInstance);
            Assert.Equal(256, updatedComponentInstance.IntProperty);
            Assert.Equal("String that did change", updatedComponentInstance.StringProperty);
            Assert.Same(objectThatWillNotChange, updatedComponentInstance.ObjectProperty);
        }

        [Fact]
        public void ReRendersChildComponentsWhenPropertiesChange()
        {
            // Arrange: First render
            var renderer = new TestRenderer();
            var firstRender = true;
            var component = new TestComponent(builder =>
            {
                builder.OpenComponentElement<MessageComponent>(1);
                builder.AddAttribute(2, nameof(MessageComponent.Message), firstRender ? "first" : "second");
                builder.CloseElement();
            });

            var rootComponentId = renderer.AssignComponentId(component);
            renderer.RenderComponent(rootComponentId);

            var childComponentId = renderer.RenderTreesByComponentId[rootComponentId]
                .Single(node => node.NodeType == RenderTreeNodeType.Component)
                .ComponentId;

            // This isn't strictly necessary for the test, but it's more common for components
            // to be updated after their first render than before it
            renderer.RenderComponent(childComponentId);

            // Act: Second render
            firstRender = false;
            renderer.RenderComponent(rootComponentId);

            var updatedComponentNode = renderer.RenderTreesByComponentId[rootComponentId]
                .Single(node => node.NodeType == RenderTreeNodeType.Component);

            // Assert
            Assert.Collection(renderer.RenderTreesByComponentId[updatedComponentNode.ComponentId],
                node => AssertNode.Text(node, "second"));
        }

        private class NoOpRenderer : Renderer
        {
            public new int AssignComponentId(IComponent component)
                => base.AssignComponentId(component);

            public new void RenderComponent(int componentId)
                => base.RenderComponent(componentId);

            protected internal override void UpdateDisplay(int componentId, RenderTreeDiff renderTreeDiff)
            {
            }
        }

        private class TestRenderer : Renderer
        {
            public IDictionary<int, ArrayRange<RenderTreeNode>> RenderTreesByComponentId { get; }
                = new Dictionary<int, ArrayRange<RenderTreeNode>>();

            public new int AssignComponentId(IComponent component)
                => base.AssignComponentId(component);

            public new void RenderComponent(int componentId)
                => base.RenderComponent(componentId);

            public new void DispatchEvent(int componentId, int renderTreeIndex, UIEventArgs args)
                => base.DispatchEvent(componentId, renderTreeIndex, args);

            protected internal override void UpdateDisplay(int componentId, RenderTreeDiff renderTreeDiff)
            {
                RenderTreesByComponentId[componentId] = renderTreeDiff.CurrentState;
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

        private class MessageComponent : IComponent
        {
            public string Message { get; set; }

            public void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.AddText(0, Message);
            }
        }

        private class FakeComponent : IComponent
        {
            public int IntProperty { get; set; }
            public string StringProperty { get; set; }
            public object ObjectProperty { get; set; }

            public void BuildRenderTree(RenderTreeBuilder builder)
            {
            }
        }

        private class EventComponent : IComponent
        {
            public UIEventHandler Handler { get; set; }

            public void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, "some element");
                builder.AddAttribute(1, "some event", Handler);
                builder.CloseElement();
            }
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

        (int, T) FirstWithIndex<T>(IEnumerable<T> items, Predicate<T> predicate)
        {
            var index = 0;
            foreach (var item in items)
            {
                if (predicate(item))
                {
                    return (index, item);
                }

                index++;
            }

            throw new InvalidOperationException("No matching element was found.");
        }
    }
}
