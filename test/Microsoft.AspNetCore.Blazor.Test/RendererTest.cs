// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.Rendering;
using Microsoft.AspNetCore.Blazor.RenderTree;
using Microsoft.AspNetCore.Blazor.Test.Helpers;
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
                builder.AddContent(1, "some text");
                builder.CloseElement();
            });

            // Act
            var componentId = renderer.AssignComponentId(component);
            component.TriggerRender();

            // Assert
            var batch = renderer.Batches.Single();
            var diff = batch.DiffsByComponentId[componentId].Single();
            Assert.Collection(diff.Edits,
                edit =>
                {
                    Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                    Assert.Equal(0, edit.ReferenceFrameIndex);
                });
            AssertFrame.Element(batch.ReferenceFrames[0], "my element", 2);
            AssertFrame.Text(batch.ReferenceFrames[1], "some text");
        }

        [Fact]
        public void CanRenderNestedComponents()
        {
            // Arrange
            var renderer = new TestRenderer();
            var component = new TestComponent(builder =>
            {
                builder.AddContent(0, "Hello");
                builder.OpenComponent<MessageComponent>(1);
                builder.AddAttribute(2, nameof(MessageComponent.Message), "Nested component output");
                builder.CloseComponent();
            });

            // Act/Assert
            var componentId = renderer.AssignComponentId(component);
            component.TriggerRender();
            var batch = renderer.Batches.Single();
            var componentFrame = batch.ReferenceFrames
                .Single(frame => frame.FrameType == RenderTreeFrameType.Component);
            var nestedComponentId = componentFrame.ComponentId;
            var nestedComponentDiff = batch.DiffsByComponentId[nestedComponentId].Single();

            // We rendered both components
            Assert.Equal(2, batch.DiffsByComponentId.Count);

            // The nested component exists
            Assert.IsType<MessageComponent>(componentFrame.Component);

            // The nested component was rendered as part of the batch
            Assert.Collection(nestedComponentDiff.Edits,
                edit =>
                {
                    Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                    AssertFrame.Text(
                        batch.ReferenceFrames[edit.ReferenceFrameIndex],
                        "Nested component output");
                });
        }

        [Fact]
        public void CanReRenderTopLevelComponents()
        {
            // Arrange
            var renderer = new TestRenderer();
            var component = new MessageComponent { Message = "Initial message" };
            var componentId = renderer.AssignComponentId(component);

            // Act/Assert: first render
            component.TriggerRender();
            var batch = renderer.Batches.Single();
            var firstDiff = batch.DiffsByComponentId[componentId].Single();
            Assert.Collection(firstDiff.Edits,
                edit =>
                {
                    Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                    Assert.Equal(0, edit.ReferenceFrameIndex);
                    AssertFrame.Text(batch.ReferenceFrames[0], "Initial message");
                });

            // Act/Assert: second render
            component.Message = "Modified message";
            component.TriggerRender();
            var secondBatch = renderer.Batches.Skip(1).Single();
            var secondDiff = secondBatch.DiffsByComponentId[componentId].Single();
            Assert.Collection(secondDiff.Edits,
                edit =>
                {
                    Assert.Equal(RenderTreeEditType.UpdateText, edit.Type);
                    Assert.Equal(0, edit.ReferenceFrameIndex);
                    AssertFrame.Text(secondBatch.ReferenceFrames[0], "Modified message");
                });
        }

        [Fact]
        public void CanReRenderNestedComponents()
        {
            // Arrange: parent component already rendered
            var renderer = new TestRenderer();
            var parentComponent = new TestComponent(builder =>
            {
                builder.OpenComponent<MessageComponent>(0);
                builder.CloseComponent();
            });
            var parentComponentId = renderer.AssignComponentId(parentComponent);
            parentComponent.TriggerRender();
            var nestedComponentFrame = renderer.Batches.Single()
                .ReferenceFrames
                .Single(frame => frame.FrameType == RenderTreeFrameType.Component);
            var nestedComponent = (MessageComponent)nestedComponentFrame.Component;
            var nestedComponentId = nestedComponentFrame.ComponentId;

            // Assert: inital render
            nestedComponent.Message = "Render 1";
            nestedComponent.TriggerRender();
            var batch = renderer.Batches[1];
            var firstDiff = batch.DiffsByComponentId[nestedComponentId].Single();
            Assert.Collection(firstDiff.Edits,
                edit =>
                {
                    Assert.Equal(RenderTreeEditType.UpdateText, edit.Type);
                    Assert.Equal(0, edit.ReferenceFrameIndex);
                    AssertFrame.Text(batch.ReferenceFrames[0], "Render 1");
                });

            // Act/Assert: re-render
            nestedComponent.Message = "Render 2";
            nestedComponent.TriggerRender();
            var secondBatch = renderer.Batches[2];
            var secondDiff = secondBatch.DiffsByComponentId[nestedComponentId].Single();
            Assert.Collection(secondDiff.Edits,
                edit =>
                {
                    Assert.Equal(RenderTreeEditType.UpdateText, edit.Type);
                    Assert.Equal(0, edit.ReferenceFrameIndex);
                    AssertFrame.Text(secondBatch.ReferenceFrames[0], "Render 2");
                });
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
            component.TriggerRender();

            var eventHandlerId = renderer.Batches.Single()
                .ReferenceFrames
                .First(frame => frame.AttributeValue != null)
                .AttributeEventHandlerId;

            // Assert: Event not yet fired
            Assert.Null(receivedArgs);

            // Act/Assert: Event can be fired
            var eventArgs = new UIEventArgs();
            renderer.DispatchEvent(componentId, eventHandlerId, eventArgs);
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
                builder.OpenComponent<EventComponent>(0);
                builder.CloseComponent();
            });
            var parentComponentId = renderer.AssignComponentId(parentComponent);
            parentComponent.TriggerRender();

            // Arrange: Render nested component
            var nestedComponentFrame = renderer.Batches.Single()
                .ReferenceFrames
                .Single(frame => frame.FrameType == RenderTreeFrameType.Component);
            var nestedComponent = (EventComponent)nestedComponentFrame.Component;
            nestedComponent.Handler = args => { receivedArgs = args; };
            var nestedComponentId = nestedComponentFrame.ComponentId;
            nestedComponent.TriggerRender();

            // Find nested component's event handler ID
            var eventHandlerId = renderer.Batches[1]
                .ReferenceFrames
                .First(frame => frame.AttributeValue != null)
                .AttributeEventHandlerId;

            // Assert: Event not yet fired
            Assert.Null(receivedArgs);

            // Act/Assert: Event can be fired
            var eventArgs = new UIEventArgs();
            renderer.DispatchEvent(nestedComponentId, eventHandlerId, eventArgs);
            Assert.Same(eventArgs, receivedArgs);
        }

        [Fact]
        public void ThrowsIfComponentDoesNotHandleEvents()
        {
            // Arrange: Render a component with an event handler
            var renderer = new TestRenderer();
            UIEventHandler handler = args => throw new NotImplementedException();
            var component = new TestComponent(builder =>
            {
                builder.OpenElement(0, "mybutton");
                builder.AddAttribute(1, "my click event", handler);
                builder.CloseElement();
            });

            var componentId = renderer.AssignComponentId(component);
            component.TriggerRender();

            var eventHandlerId = renderer.Batches.Single()
                .ReferenceFrames
                .First(frame => frame.AttributeValue != null)
                .AttributeEventHandlerId;
            var eventArgs = new UIEventArgs();

            // Act/Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                renderer.DispatchEvent(componentId, eventHandlerId, eventArgs);
            });
            Assert.Equal($"The component of type {typeof(TestComponent).FullName} cannot receive " +
                $"events because it does not implement {typeof(IHandleEvent).FullName}.", ex.Message);
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
            var component = new MultiRendererComponent();
            var renderer1ComponentId = renderer1.AssignComponentId(component);
            renderer2.AssignComponentId(new TestComponent(null)); // Just so they don't get the same IDs
            var renderer2ComponentId = renderer2.AssignComponentId(component);

            // Act/Assert
            component.TriggerRender();
            var renderer1Batch = renderer1.Batches.Single();
            var renderer1Diff = renderer1Batch.DiffsByComponentId[renderer1ComponentId].Single();
            Assert.Collection(renderer1Diff.Edits,
                edit =>
                {
                    Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                    AssertFrame.Text(renderer1Batch.ReferenceFrames[edit.ReferenceFrameIndex],
                        $"Hello from {nameof(MultiRendererComponent)}", 0);
                });

            var renderer2Batch = renderer2.Batches.Single();
            var renderer2Diff = renderer2Batch.DiffsByComponentId[renderer2ComponentId].Single();
            Assert.Collection(renderer2Diff.Edits,
                edit =>
                {
                    Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                    AssertFrame.Text(renderer2Batch.ReferenceFrames[edit.ReferenceFrameIndex],
                        $"Hello from {nameof(MultiRendererComponent)}", 0);
                });
        }

        [Fact]
        public void PreservesChildComponentInstancesWithNoAttributes()
        {
            // Arrange: First render, capturing child component instance
            var renderer = new TestRenderer();
            var message = "Hello";
            var component = new TestComponent(builder =>
            {
                builder.AddContent(0, message);
                builder.OpenComponent<MessageComponent>(1);
                builder.CloseComponent();
            });

            var rootComponentId = renderer.AssignComponentId(component);
            component.TriggerRender();

            var nestedComponentFrame = renderer.Batches.Single()
                .ReferenceFrames
                .Single(frame => frame.FrameType == RenderTreeFrameType.Component);
            var nestedComponentInstance = (MessageComponent)nestedComponentFrame.Component;

            // Act: Second render
            message = "Modified message";
            component.TriggerRender();

            // Assert
            var batch = renderer.Batches[1];
            var diff = batch.DiffsByComponentId[rootComponentId].Single();
            Assert.Collection(diff.Edits,
                edit =>
                {
                    Assert.Equal(RenderTreeEditType.UpdateText, edit.Type);
                    Assert.Equal(0, edit.ReferenceFrameIndex);
                });
            AssertFrame.Text(batch.ReferenceFrames[0], "Modified message");
            Assert.False(batch.DiffsByComponentId.ContainsKey(nestedComponentFrame.ComponentId));
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
                builder.OpenComponent<FakeComponent>(1);
                builder.AddAttribute(2, nameof(FakeComponent.IntProperty), firstRender ? 123 : 256);
                builder.AddAttribute(3, nameof(FakeComponent.ObjectProperty), objectThatWillNotChange);
                builder.AddAttribute(4, nameof(FakeComponent.StringProperty), firstRender ? "String that will change" : "String that did change");
                builder.CloseComponent();
            });

            var rootComponentId = renderer.AssignComponentId(component);
            component.TriggerRender();

            var originalComponentFrame = renderer.Batches.Single()
                .ReferenceFrames
                .Single(frame => frame.FrameType == RenderTreeFrameType.Component);
            var childComponentInstance = (FakeComponent)originalComponentFrame.Component;

            // Assert 1: properties were assigned
            Assert.Equal(123, childComponentInstance.IntProperty);
            Assert.Equal("String that will change", childComponentInstance.StringProperty);
            Assert.Same(objectThatWillNotChange, childComponentInstance.ObjectProperty);

            // Act: Second render
            firstRender = false;
            component.TriggerRender();

            // Assert
            Assert.Equal(256, childComponentInstance.IntProperty);
            Assert.Equal("String that did change", childComponentInstance.StringProperty);
            Assert.Same(objectThatWillNotChange, childComponentInstance.ObjectProperty);
        }

        [Fact]
        public void ReRendersChildComponentsWhenPropertiesChange()
        {
            // Arrange: First render
            var renderer = new TestRenderer();
            var firstRender = true;
            var component = new TestComponent(builder =>
            {
                builder.OpenComponent<MessageComponent>(1);
                builder.AddAttribute(2, nameof(MessageComponent.Message), firstRender ? "first" : "second");
                builder.CloseComponent();
            });

            var rootComponentId = renderer.AssignComponentId(component);
            component.TriggerRender();

            var childComponentId = renderer.Batches.Single()
                .ReferenceFrames
                .Single(frame => frame.FrameType == RenderTreeFrameType.Component)
                .ComponentId;

            // Act: Second render
            firstRender = false;
            component.TriggerRender();
            var diff = renderer.Batches[1].DiffsByComponentId[childComponentId].Single();

            // Assert
            Assert.Collection(diff.Edits,
                edit =>
                {
                    Assert.Equal(RenderTreeEditType.UpdateText, edit.Type);
                    Assert.Equal(0, edit.ReferenceFrameIndex);
                });
            AssertFrame.Text(renderer.Batches[1].ReferenceFrames[0], "second");
        }

        [Fact]
        public void RenderBatchIncludesListOfDisposedComponents()
        {
            // Arrange
            var renderer = new TestRenderer();
            var firstRender = true;
            var component = new TestComponent(builder =>
            {
                if (firstRender)
                {
                    // Nested descendants
                    builder.OpenComponent<ConditionalParentComponent<FakeComponent>>(100);
                    builder.AddAttribute(101, nameof(ConditionalParentComponent<FakeComponent>.IncludeChild), true);
                    builder.CloseComponent();
                }
                builder.OpenComponent<FakeComponent>(200);
                builder.CloseComponent();
            });

            var rootComponentId = renderer.AssignComponentId(component);

            // Act/Assert 1: First render, capturing child component IDs
            component.TriggerRender();
            var batch = renderer.Batches.Single();
            var rootComponentDiff = batch.DiffsByComponentId[rootComponentId].Single();
            var childComponentIds = rootComponentDiff
                .Edits
                .Select(edit => batch.ReferenceFrames[edit.ReferenceFrameIndex])
                .Where(frame => frame.FrameType == RenderTreeFrameType.Component)
                .Select(frame => frame.ComponentId)
                .ToList();
            var childComponent3 = batch.ReferenceFrames.Where(f => f.ComponentId == 3)
                .Single().Component;
            Assert.Equal(new[] { 1, 2 }, childComponentIds);
            Assert.IsType<FakeComponent>(childComponent3);

            // Act: Second render
            firstRender = false;
            component.TriggerRender();

            // Assert: Applicable children are included in disposal list
            Assert.Equal(2, renderer.Batches.Count);
            Assert.Equal(new[] { 1, 3 }, renderer.Batches[1].DisposedComponentIDs);

            // Act/Assert: If a disposed component requests a render, it's a no-op
            ((FakeComponent)childComponent3).RenderHandle.Render(builder
                => throw new NotImplementedException("Should not be invoked"));
            Assert.Equal(2, renderer.Batches.Count);
        }

        [Fact]
        public void DisposesEventHandlersWhenAttributeValueChanged()
        {
            // Arrange
            var renderer = new TestRenderer();
            var eventCount = 0;
            UIEventHandler origEventHandler = args => { eventCount++; };
            var component = new EventComponent { Handler = origEventHandler };
            var componentId = renderer.AssignComponentId(component);
            component.TriggerRender();
            var origEventHandlerId = renderer.Batches.Single()
                .ReferenceFrames
                .Where(f => f.FrameType == RenderTreeFrameType.Attribute)
                .Single(f => f.AttributeEventHandlerId != 0)
                .AttributeEventHandlerId;

            // Act/Assert 1: Event handler fires when we trigger it
            Assert.Equal(0, eventCount);
            renderer.DispatchEvent(componentId, origEventHandlerId, args: null);
            Assert.Equal(1, eventCount);

            // Now change the attribute value
            var newEventCount = 0;
            component.Handler = args => { newEventCount++; };
            component.TriggerRender();

            // Act/Assert 2: Can no longer fire the original event, but can fire the new event
            Assert.Throws<ArgumentException>(() =>
            {
                renderer.DispatchEvent(componentId, origEventHandlerId, args: null);
            });
            Assert.Equal(1, eventCount);
            Assert.Equal(0, newEventCount);
            renderer.DispatchEvent(componentId, origEventHandlerId + 1, args: null);
            Assert.Equal(1, newEventCount);
        }

        [Fact]
        public void DisposesEventHandlersWhenAttributeRemoved()
        {
            // Arrange
            var renderer = new TestRenderer();
            var eventCount = 0;
            UIEventHandler origEventHandler = args => { eventCount++; };
            var component = new EventComponent { Handler = origEventHandler };
            var componentId = renderer.AssignComponentId(component);
            component.TriggerRender();
            var origEventHandlerId = renderer.Batches.Single()
                .ReferenceFrames
                .Where(f => f.FrameType == RenderTreeFrameType.Attribute)
                .Single(f => f.AttributeEventHandlerId != 0)
                .AttributeEventHandlerId;

            // Act/Assert 1: Event handler fires when we trigger it
            Assert.Equal(0, eventCount);
            renderer.DispatchEvent(componentId, origEventHandlerId, args: null);
            Assert.Equal(1, eventCount);

            // Now remove the event attribute
            component.Handler = null;
            component.TriggerRender();

            // Act/Assert 2: Can no longer fire the original event
            Assert.Throws<ArgumentException>(() =>
            {
                renderer.DispatchEvent(componentId, origEventHandlerId, args: null);
            });
            Assert.Equal(1, eventCount);
        }

        [Fact]
        public void DisposesEventHandlersWhenOwnerComponentRemoved()
        {
            // Arrange
            var renderer = new TestRenderer();
            var eventCount = 0;
            UIEventHandler origEventHandler = args => { eventCount++; };
            var component = new ConditionalParentComponent<EventComponent>
            {
                IncludeChild = true,
                ChildParameters = new Dictionary<string, object>
                {
                    { nameof(EventComponent.Handler), origEventHandler }
                }
            };
            var rootComponentId = renderer.AssignComponentId(component);
            component.TriggerRender();
            var batch = renderer.Batches.Single();
            var rootComponentDiff = batch.DiffsByComponentId[rootComponentId].Single();
            var rootComponentFrame = batch.ReferenceFrames[0];
            var childComponentFrame = rootComponentDiff.Edits
                .Select(e => batch.ReferenceFrames[e.ReferenceFrameIndex])
                .Where(f => f.FrameType == RenderTreeFrameType.Component)
                .Single();
            var childComponentId = childComponentFrame.ComponentId;
            var childComponentDiff = batch.DiffsByComponentId[childComponentFrame.ComponentId].Single();
            var eventHandlerId = batch.ReferenceFrames
                .Skip(childComponentDiff.Edits[0].ReferenceFrameIndex) // Search from where the child component frames start
                .Where(f => f.FrameType == RenderTreeFrameType.Attribute)
                .Single(f => f.AttributeEventHandlerId != 0)
                .AttributeEventHandlerId;

            // Act/Assert 1: Event handler fires when we trigger it
            Assert.Equal(0, eventCount);
            renderer.DispatchEvent(childComponentId, eventHandlerId, args: null);
            Assert.Equal(1, eventCount);

            // Now remove the EventComponent
            component.IncludeChild = false;
            component.TriggerRender();

            // Act/Assert 2: Can no longer fire the original event
            Assert.Throws<ArgumentException>(() =>
            {
                renderer.DispatchEvent(eventHandlerId, eventHandlerId, args: null);
            });
            Assert.Equal(1, eventCount);
        }

        [Fact]
        public void DisposesEventHandlersWhenAncestorElementRemoved()
        {
            // Arrange
            var renderer = new TestRenderer();
            var eventCount = 0;
            UIEventHandler origEventHandler = args => { eventCount++; };
            var component = new EventComponent { Handler = origEventHandler };
            var componentId = renderer.AssignComponentId(component);
            component.TriggerRender();
            var origEventHandlerId = renderer.Batches.Single()
                .ReferenceFrames
                .Where(f => f.FrameType == RenderTreeFrameType.Attribute)
                .Single(f => f.AttributeEventHandlerId != 0)
                .AttributeEventHandlerId;

            // Act/Assert 1: Event handler fires when we trigger it
            Assert.Equal(0, eventCount);
            renderer.DispatchEvent(componentId, origEventHandlerId, args: null);
            Assert.Equal(1, eventCount);

            // Now remove the ancestor element
            component.SkipElement = true;
            component.TriggerRender();

            // Act/Assert 2: Can no longer fire the original event
            Assert.Throws<ArgumentException>(() =>
            {
                renderer.DispatchEvent(componentId, origEventHandlerId, args: null);
            });
            Assert.Equal(1, eventCount);
        }

        [Fact]
        public void AllRendersTriggeredSynchronouslyDuringEventHandlerAreHandledAsSingleBatch()
        {
            // Arrange: A root component with a child whose event handler explicitly queues
            // a re-render of both the root component and the child
            var renderer = new TestRenderer();
            var eventCount = 0;
            TestComponent rootComponent = null;
            EventComponent childComponent = null;
            rootComponent = new TestComponent(builder =>
            {
                builder.AddContent(0, "Child event count: " + eventCount);
                builder.OpenComponent<EventComponent>(1);
                builder.AddAttribute(2, nameof(EventComponent.Handler), args =>
                {
                    eventCount++;
                    rootComponent.TriggerRender();
                    childComponent.TriggerRender();
                });
                builder.CloseComponent();
            });
            var rootComponentId = renderer.AssignComponentId(rootComponent);
            rootComponent.TriggerRender();
            var origBatchReferenceFrames = renderer.Batches.Single().ReferenceFrames;
            var childComponentFrame = origBatchReferenceFrames
                .Single(f => f.Component is EventComponent);
            var childComponentId = childComponentFrame.ComponentId;
            childComponent = (EventComponent)childComponentFrame.Component;
            var origEventHandlerId = origBatchReferenceFrames
                .Where(f => f.FrameType == RenderTreeFrameType.Attribute)
                .Last(f => f.AttributeEventHandlerId != 0)
                .AttributeEventHandlerId;
            Assert.Single(renderer.Batches);

            // Act
            renderer.DispatchEvent(childComponentId, origEventHandlerId, args: null);

            // Assert
            Assert.Equal(2, renderer.Batches.Count);
            var batch = renderer.Batches.Last();
            Assert.Collection(batch.DiffsInOrder,
                diff =>
                {
                    // First we triggered the root component to re-render
                    Assert.Equal(rootComponentId, diff.ComponentId);
                    Assert.Collection(diff.Edits, edit =>
                    {
                        Assert.Equal(RenderTreeEditType.UpdateText, edit.Type);
                        AssertFrame.Text(
                            batch.ReferenceFrames[edit.ReferenceFrameIndex],
                            "Child event count: 1");
                    });
                },
                diff =>
                {
                    // Then the root re-render will have triggered an update to the child
                    Assert.Equal(childComponentId, diff.ComponentId);
                    Assert.Collection(diff.Edits, edit =>
                    {
                        Assert.Equal(RenderTreeEditType.UpdateText, edit.Type);
                        AssertFrame.Text(
                            batch.ReferenceFrames[edit.ReferenceFrameIndex],
                            "Render count: 2");
                    });
                },
                diff =>
                {
                    // Finally we explicitly requested a re-render of the child
                    Assert.Equal(childComponentId, diff.ComponentId);
                    Assert.Collection(diff.Edits, edit =>
                    {
                        Assert.Equal(RenderTreeEditType.UpdateText, edit.Type);
                        AssertFrame.Text(
                            batch.ReferenceFrames[edit.ReferenceFrameIndex],
                            "Render count: 3");
                    });
                });
        }

        [Fact]
        public void ComponentCannotTriggerRenderBeforeRenderHandleAssigned()
        {
            // Arrange
            var component = new TestComponent(builder => { });

            // Act/Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                component.TriggerRender();
            });
            Assert.Equal("The render handle is not yet assigned.", ex.Message);
        }

        [Fact]
        public void ComponentCanTriggerRenderWhenNoBatchIsInProgress()
        {
            // Arrange
            var renderer = new TestRenderer();
            var renderCount = 0;
            var component = new TestComponent(builder =>
            {
                builder.AddContent(0, $"Render count: {++renderCount}");
            });
            var componentId = renderer.AssignComponentId(component);

            // Act/Assert: Can trigger initial render
            Assert.Equal(0, renderCount);
            component.TriggerRender();
            Assert.Equal(1, renderCount);
            var batch1 = renderer.Batches.Single();
            var edit1 = batch1.DiffsByComponentId[componentId].Single().Edits.Single();
            Assert.Equal(RenderTreeEditType.PrependFrame, edit1.Type);
            AssertFrame.Text(batch1.ReferenceFrames[edit1.ReferenceFrameIndex],
                "Render count: 1", 0);

            // Act/Assert: Can trigger subsequent render
            component.TriggerRender();
            Assert.Equal(2, renderCount);
            var batch2 = renderer.Batches.Skip(1).Single();
            var edit2 = batch2.DiffsByComponentId[componentId].Single().Edits.Single();
            Assert.Equal(RenderTreeEditType.UpdateText, edit2.Type);
            AssertFrame.Text(batch2.ReferenceFrames[edit2.ReferenceFrameIndex],
                "Render count: 2", 0);
        }

        [Fact]
        public void ComponentCanTriggerRenderWhenExistingBatchIsInProgress()
        {
            // Arrange
            var renderer = new TestRenderer();
            TestComponent parent = null;
            var parentRenderCount = 0;
            parent = new TestComponent(builder =>
            {
                builder.OpenComponent<ReRendersParentComponent>(0);
                builder.AddAttribute(1, nameof(ReRendersParentComponent.Parent), parent);
                builder.CloseComponent();
                builder.AddContent(2, $"Parent render count: {++parentRenderCount}");
            });
            var parentComponentId = renderer.AssignComponentId(parent);

            // Act
            parent.TriggerRender();

            // Assert
            var batch = renderer.Batches.Single();
            Assert.Equal(4, batch.DiffsInOrder.Count);

            // First is the parent component's initial render
            var diff1 = batch.DiffsInOrder[0];
            Assert.Equal(parentComponentId, diff1.ComponentId);
            Assert.Collection(diff1.Edits,
                edit =>
                {
                    Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                    AssertFrame.Component<ReRendersParentComponent>(
                        batch.ReferenceFrames[edit.ReferenceFrameIndex]);
                },
                edit =>
                {
                    Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                    AssertFrame.Text(
                        batch.ReferenceFrames[edit.ReferenceFrameIndex],
                        "Parent render count: 1");
                });

            // Second is the child component's single render
            var diff2 = batch.DiffsInOrder[1];
            Assert.NotEqual(parentComponentId, diff2.ComponentId);
            var diff2edit = diff2.Edits.Single();
            Assert.Equal(RenderTreeEditType.PrependFrame, diff2edit.Type);
            AssertFrame.Text(batch.ReferenceFrames[diff2edit.ReferenceFrameIndex],
                "Child is here");

            // Third is the parent's triggered render
            var diff3 = batch.DiffsInOrder[2];
            Assert.Equal(parentComponentId, diff3.ComponentId);
            var diff3edit = diff3.Edits.Single();
            Assert.Equal(RenderTreeEditType.UpdateText, diff3edit.Type);
            AssertFrame.Text(batch.ReferenceFrames[diff3edit.ReferenceFrameIndex],
                "Parent render count: 2");

            // Fourth is child's rerender due to parent rendering
            var diff4 = batch.DiffsInOrder[3];
            Assert.NotEqual(parentComponentId, diff4.ComponentId);
            Assert.Empty(diff4.Edits);
        }

        [Fact]
        public void QueuedRenderIsSkippedIfComponentWasAlreadyDisposedInSameBatch()
        {
            // Arrange
            var renderer = new TestRenderer();
            var shouldRenderChild = true;
            TestComponent component = null;
            component = new TestComponent(builder =>
            {
                builder.AddContent(0, "Some frame so the child isn't at position zero");
                if (shouldRenderChild)
                {
                    builder.OpenComponent<RendersSelfAfterEventComponent>(1);
                    builder.AddAttribute(2, nameof(RendersSelfAfterEventComponent.OnClick), (Action)(() =>
                    {
                        // First we queue (1) a re-render of the root component, then the child component
                        // will queue (2) its own re-render. But by the time (1) completes, the child will
                        // have been disposed, even though (2) is still in the queue
                        shouldRenderChild = false;
                        component.TriggerRender();
                    }));
                    builder.CloseComponent();
                }
            });

            var componentId = renderer.AssignComponentId(component);
            component.TriggerRender();
            var childComponentId = renderer.Batches.Single()
                .ReferenceFrames
                .Where(f => f.ComponentId != 0)
                .Single()
                .ComponentId;
            var origEventHandlerId = renderer.Batches.Single()
                .ReferenceFrames
                .Where(f => f.FrameType == RenderTreeFrameType.Attribute)
                .Single(f => f.AttributeEventHandlerId != 0)
                .AttributeEventHandlerId;

            // Act
            // The fact that there's no error here is the main thing we're testing
            renderer.DispatchEvent(childComponentId, origEventHandlerId, args: null);

            // Assert: correct render result
            var newBatch = renderer.Batches.Skip(1).Single();
            Assert.Equal(1, newBatch.DisposedComponentIDs.Count);
            Assert.Equal(1, newBatch.DiffsByComponentId.Count);
            Assert.Collection(newBatch.DiffsByComponentId[componentId].Single().Edits,
                edit =>
                {
                    Assert.Equal(RenderTreeEditType.RemoveFrame, edit.Type);
                    Assert.Equal(1, edit.SiblingIndex);
                });
        }

        private class NoOpRenderer : Renderer
        {
            public NoOpRenderer() : base(new TestServiceProvider())
            {
            }

            public new int AssignComponentId(IComponent component)
                => base.AssignComponentId(component);

            protected override void UpdateDisplay(RenderBatch renderBatch)
            {
            }
        }

        private class TestComponent : IComponent
        {
            private RenderHandle _renderHandle;
            private RenderFragment _renderFragment;

            public TestComponent(RenderFragment renderFragment)
            {
                _renderFragment = renderFragment;
            }

            public void Init(RenderHandle renderHandle)
            {
                _renderHandle = renderHandle;
            }

            public void SetParameters(ParameterCollection parameters)
                => TriggerRender();

            public void TriggerRender()
                => _renderHandle.Render(_renderFragment);
        }

        private class MessageComponent : AutoRenderComponent
        {
            public string Message { get; set; }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.AddContent(0, Message);
            }
        }

        private class FakeComponent : IComponent
        {
            public int IntProperty { get; set; }
            public string StringProperty { get; set; }
            public object ObjectProperty { get; set; }
            public RenderHandle RenderHandle { get; private set; }

            public void Init(RenderHandle renderHandle)
                => RenderHandle = renderHandle;

            public void SetParameters(ParameterCollection parameters)
                => parameters.AssignToProperties(this);
        }

        private class EventComponent : AutoRenderComponent, IComponent, IHandleEvent
        {
            public UIEventHandler Handler { get; set; }
            public bool SkipElement { get; set; }
            private int renderCount = 0;

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, "grandparent");
                if (!SkipElement)
                {
                    builder.OpenElement(1, "parent");
                    builder.OpenElement(2, "some element");
                    if (Handler != null)
                    {
                        builder.AddAttribute(3, "some event", Handler);
                    }
                    builder.CloseElement();
                    builder.CloseElement();
                }
                builder.CloseElement();
                builder.AddContent(4, $"Render count: {++renderCount}");
            }

            public void HandleEvent(UIEventHandler handler, UIEventArgs args)
                => handler(args);
        }

        private class ConditionalParentComponent<T> : AutoRenderComponent where T : IComponent
        {
            public bool IncludeChild { get; set; }
            public IDictionary<string, object> ChildParameters { get; set; }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.AddContent(0, "Parent here");
                
                if (IncludeChild)
                {
                    builder.OpenComponent<T>(1);
                    if (ChildParameters != null)
                    {
                        var sequence = 2;
                        foreach (var kvp in ChildParameters)
                        {
                            builder.AddAttribute(sequence++, kvp.Key, kvp.Value);
                        }
                    }
                    builder.CloseComponent();
                }
            }
        }
        
        private class ReRendersParentComponent : AutoRenderComponent
        {
            public TestComponent Parent { get; set; }
            private bool _isFirstTime = true;

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                if (_isFirstTime) // Don't want an infinite loop
                {
                    _isFirstTime = false;
                    Parent.TriggerRender();
                }

                builder.AddContent(0, "Child is here");
            }
        }

        private class RendersSelfAfterEventComponent : IComponent, IHandleEvent
        {
            public Action OnClick { get; set; }

            private RenderHandle _renderHandle;

            public void Init(RenderHandle renderHandle)
                => _renderHandle = renderHandle;

            public void SetParameters(ParameterCollection parameters)
            {
                parameters.AssignToProperties(this);
                Render();
            }

            public void HandleEvent(UIEventHandler handler, UIEventArgs args)
            {
                handler(args);
                Render();
            }

            private void Render()
                => _renderHandle.Render(builder =>
                {
                    builder.OpenElement(0, "my button");
                    builder.AddAttribute(1, "my click handler", eventArgs => OnClick());
                    builder.CloseElement();
                });
        }

        private class MultiRendererComponent : IComponent
        {
            private readonly List<RenderHandle> _renderHandles
                = new List<RenderHandle>();

            public void Init(RenderHandle renderHandle)
                => _renderHandles.Add(renderHandle);

            public void SetParameters(ParameterCollection parameters)
            {
            }

            public void TriggerRender()
            {
                foreach (var renderHandle in _renderHandles)
                {
                    renderHandle.Render(builder =>
                    {
                        builder.AddContent(0, $"Hello from {nameof(MultiRendererComponent)}");
                    });
                }
            }
        }
    }
}
