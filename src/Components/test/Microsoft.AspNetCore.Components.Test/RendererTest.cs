// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Xunit;

namespace Microsoft.AspNetCore.Components.Test
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
            var componentId = renderer.AssignRootComponentId(component);
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
            var componentId = renderer.AssignRootComponentId(component);
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
            var componentId = renderer.AssignRootComponentId(component);

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
            var parentComponentId = renderer.AssignRootComponentId(parentComponent);
            parentComponent.TriggerRender();
            var nestedComponentFrame = renderer.Batches.Single()
                .ReferenceFrames
                .Single(frame => frame.FrameType == RenderTreeFrameType.Component);
            var nestedComponent = (MessageComponent)nestedComponentFrame.Component;
            var nestedComponentId = nestedComponentFrame.ComponentId;

            // Assert: initial render
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
                OnTest = args => { receivedArgs = args; }
            };
            var componentId = renderer.AssignRootComponentId(component);
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
        public void CanDispatchTypedEventsToTopLevelComponents()
        {
            // Arrange: Render a component with an event handler
            var renderer = new TestRenderer();
            UIMouseEventArgs receivedArgs = null;

            var component = new EventComponent
            {
                OnClick = args => { receivedArgs = args; }
            };
            var componentId = renderer.AssignRootComponentId(component);
            component.TriggerRender();

            var eventHandlerId = renderer.Batches.Single()
                .ReferenceFrames
                .First(frame => frame.AttributeValue != null)
                .AttributeEventHandlerId;

            // Assert: Event not yet fired
            Assert.Null(receivedArgs);

            // Act/Assert: Event can be fired
            var eventArgs = new UIMouseEventArgs();
            renderer.DispatchEvent(componentId, eventHandlerId, eventArgs);
            Assert.Same(eventArgs, receivedArgs);
        }

        [Fact]
        public void CanDispatchActionEventsToTopLevelComponents()
        {
            // Arrange: Render a component with an event handler
            var renderer = new TestRenderer();
            object receivedArgs = null;

            var component = new EventComponent
            {
                OnClickAction = () => { receivedArgs = new object(); }
            };
            var componentId = renderer.AssignRootComponentId(component);
            component.TriggerRender();

            var eventHandlerId = renderer.Batches.Single()
                .ReferenceFrames
                .First(frame => frame.AttributeValue != null)
                .AttributeEventHandlerId;

            // Assert: Event not yet fired
            Assert.Null(receivedArgs);

            // Act/Assert: Event can be fired
            var eventArgs = new UIMouseEventArgs();
            renderer.DispatchEvent(componentId, eventHandlerId, eventArgs);
            Assert.NotNull(receivedArgs);
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
            var parentComponentId = renderer.AssignRootComponentId(parentComponent);
            parentComponent.TriggerRender();

            // Arrange: Render nested component
            var nestedComponentFrame = renderer.Batches.Single()
                .ReferenceFrames
                .Single(frame => frame.FrameType == RenderTreeFrameType.Component);
            var nestedComponent = (EventComponent)nestedComponentFrame.Component;
            nestedComponent.OnTest = args => { receivedArgs = args; };
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
            Action<UIEventArgs> handler = args => throw new NotImplementedException();
            var component = new TestComponent(builder =>
            {
                builder.OpenElement(0, "mybutton");
                builder.AddAttribute(1, "onclick", handler);
                builder.CloseElement();
            });

            var componentId = renderer.AssignRootComponentId(component);
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
            var renderer1ComponentId = renderer1.AssignRootComponentId(component);
            renderer2.AssignRootComponentId(new TestComponent(null)); // Just so they don't get the same IDs
            var renderer2ComponentId = renderer2.AssignRootComponentId(component);

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

            var rootComponentId = renderer.AssignRootComponentId(component);
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

            var rootComponentId = renderer.AssignRootComponentId(component);
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

            var rootComponentId = renderer.AssignRootComponentId(component);
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

            var rootComponentId = renderer.AssignRootComponentId(component);

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
            Action<UIEventArgs> origEventHandler = args => { eventCount++; };
            var component = new EventComponent { OnTest = origEventHandler };
            var componentId = renderer.AssignRootComponentId(component);
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
            component.OnTest = args => { newEventCount++; };
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
            Action<UIEventArgs> origEventHandler = args => { eventCount++; };
            var component = new EventComponent { OnTest = origEventHandler };
            var componentId = renderer.AssignRootComponentId(component);
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
            component.OnTest = null;
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
            Action<UIEventArgs> origEventHandler = args => { eventCount++; };
            var component = new ConditionalParentComponent<EventComponent>
            {
                IncludeChild = true,
                ChildParameters = new Dictionary<string, object>
                {
                    { nameof(EventComponent.OnTest), origEventHandler }
                }
            };
            var rootComponentId = renderer.AssignRootComponentId(component);
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
            Action<UIEventArgs> origEventHandler = args => { eventCount++; };
            var component = new EventComponent { OnTest = origEventHandler };
            var componentId = renderer.AssignRootComponentId(component);
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
                builder.AddAttribute(2, nameof(EventComponent.OnTest), args =>
                {
                    eventCount++;
                    rootComponent.TriggerRender();
                    childComponent.TriggerRender();
                });
                builder.CloseComponent();
            });
            var rootComponentId = renderer.AssignRootComponentId(rootComponent);
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
            var componentId = renderer.AssignRootComponentId(component);

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
            var parentComponentId = renderer.AssignRootComponentId(parent);

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
                    builder.AddAttribute(2, "onclick", (Action<object>)((object obj) =>
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

            var componentId = renderer.AssignRootComponentId(component);
            component.TriggerRender();
            var childComponentId = renderer.Batches.Single()
                .ReferenceFrames
                .Where(f => f.ComponentId != 0)
                .Single()
                .ComponentId;
            var origEventHandlerId = renderer.Batches.Single()
                .ReferenceFrames
                .Where(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "onclick")
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

        [Fact]
        public void CanCombineBindAndConditionalAttribute()
        {
            // This test represents https://github.com/aspnet/Blazor/issues/624

            // Arrange: Rendered with textbox enabled
            var renderer = new TestRenderer();
            var component = new BindPlusConditionalAttributeComponent();
            var componentId = renderer.AssignRootComponentId(component);
            component.TriggerRender();
            var checkboxChangeEventHandlerId = renderer.Batches.Single()
                .ReferenceFrames
                .First(frame => frame.FrameType == RenderTreeFrameType.Attribute && frame.AttributeEventHandlerId != 0)
                .AttributeEventHandlerId;

            // Act: Toggle the checkbox
            var eventArgs = new UIChangeEventArgs { Value = true };
            renderer.DispatchEvent(componentId, checkboxChangeEventHandlerId, eventArgs);
            var latestBatch = renderer.Batches.Last();
            var latestDiff = latestBatch.DiffsInOrder.Single();
            var referenceFrames = latestBatch.ReferenceFrames;

            // Assert: Textbox's "disabled" attribute was removed
            Assert.Equal(2, renderer.Batches.Count);
            Assert.Equal(componentId, latestDiff.ComponentId);
            Assert.Contains(latestDiff.Edits, edit =>
                edit.SiblingIndex == 1
                && edit.RemovedAttributeName == "disabled");
        }

        [Fact]
        public void HandlesNestedElementCapturesDuringRefresh()
        {
            // This may seem like a very arbitrary test case, but at once stage there was a bug
            // whereby the diff output was incorrect given a ref capture on an element whose
            // parent element also had a ref capture

            // Arrange
            var attrValue = 0;
            var component = new TestComponent(builder =>
            {
                builder.OpenElement(0, "parent elem");
                builder.AddAttribute(1, "parent elem attr", attrValue);
                builder.AddElementReferenceCapture(2, _ => { });
                builder.OpenElement(3, "child elem");
                builder.AddElementReferenceCapture(4, _ => { });
                builder.AddContent(5, "child text");
                builder.CloseElement();
                builder.CloseElement();
            });
            var renderer = new TestRenderer();
            renderer.AssignRootComponentId(component);

            // Act: Update the attribute value on the parent
            component.TriggerRender();
            attrValue++;
            component.TriggerRender();

            // Assert
            var latestBatch = renderer.Batches.Skip(1).Single();
            var latestDiff = latestBatch.DiffsInOrder.Single();
            Assert.Collection(latestDiff.Edits,
                edit =>
                {
                    Assert.Equal(RenderTreeEditType.SetAttribute, edit.Type);
                    Assert.Equal(0, edit.SiblingIndex);
                    AssertFrame.Attribute(latestBatch.ReferenceFrames[edit.ReferenceFrameIndex],
                        "parent elem attr", 1);
                });
        }

        [Fact]
        public void CallsAfterRenderOnEachRender()
        {
            // Arrange
            var onAfterRenderCallCountLog = new List<int>();
            var component = new AfterRenderCaptureComponent();
            var renderer = new TestRenderer
            {
                OnUpdateDisplay = _ => onAfterRenderCallCountLog.Add(component.OnAfterRenderCallCount)
            };
            renderer.AssignRootComponentId(component);

            // Act
            component.TriggerRender();

            // Assert
            // When the display was first updated, OnAfterRender had not yet been called
            Assert.Equal(new[] { 0 }, onAfterRenderCallCountLog);
            // But OnAfterRender was called since then
            Assert.Equal(1, component.OnAfterRenderCallCount);

            // Act/Assert 2: On a subsequent render, the same happens again
            component.TriggerRender();
            Assert.Equal(new[] { 0, 1 }, onAfterRenderCallCountLog);
            Assert.Equal(2, component.OnAfterRenderCallCount);
        }

        [Fact]
        public void DoesNotCallOnAfterRenderForComponentsNotRendered()
        {
            // Arrange
            var showComponent3 = true;
            var parentComponent = new TestComponent(builder =>
            {
                // First child will be re-rendered because we'll change its param
                builder.OpenComponent<AfterRenderCaptureComponent>(0);
                builder.AddAttribute(1, "some param", showComponent3);
                builder.CloseComponent();

                // Second child will not be re-rendered because nothing changes
                builder.OpenComponent<AfterRenderCaptureComponent>(2);
                builder.CloseComponent();

                // Third component will be disposed
                if (showComponent3)
                {
                    builder.OpenComponent<AfterRenderCaptureComponent>(3);
                    builder.CloseComponent();
                }
            });
            var renderer = new TestRenderer();
            var parentComponentId = renderer.AssignRootComponentId(parentComponent);

            // Act: First render
            parentComponent.TriggerRender();

            // Assert: All child components were notified of "after render"
            var batch1 = renderer.Batches.Single();
            var parentComponentEdits1 = batch1.DiffsByComponentId[parentComponentId].Single().Edits;
            var childComponents = parentComponentEdits1
                .Select(
                    edit => (AfterRenderCaptureComponent)batch1.ReferenceFrames[edit.ReferenceFrameIndex].Component)
                .ToArray();
            Assert.Equal(1, childComponents[0].OnAfterRenderCallCount);
            Assert.Equal(1, childComponents[1].OnAfterRenderCallCount);
            Assert.Equal(1, childComponents[2].OnAfterRenderCallCount);

            // Act: Second render
            showComponent3 = false;
            parentComponent.TriggerRender();

            // Assert: Only the re-rendered component was notified of "after render"
            var batch2 = renderer.Batches.Skip(1).Single();
            Assert.Equal(2, batch2.DiffsInOrder.Count); // Parent and first child
            Assert.Equal(1, batch2.DisposedComponentIDs.Count); // Third child
            Assert.Equal(2, childComponents[0].OnAfterRenderCallCount); // Retained and re-rendered
            Assert.Equal(1, childComponents[1].OnAfterRenderCallCount); // Retained and not re-rendered
            Assert.Equal(1, childComponents[2].OnAfterRenderCallCount); // Disposed
        }

        [Fact]
        public async Task CanTriggerEventHandlerDisposedInEarlierPendingBatch()
        {
            // This represents the scenario where the same event handler is being triggered
            // rapidly, such as an input event while typing. It only applies to asynchronous
            // batch updates, i.e., server-side Blazor.
            // Sequence:
            // 1. The client dispatches event X twice (say) in quick succession
            // 2. The server receives the first instance, handles the event, and re-renders
            //    some component. The act of re-rendering causes the old event handler to be
            //    replaced by a new one, so the old one is flagged to be disposed.
            // 3. The server receives the second instance. Even though the corresponding event
            //    handler is flagged to be disposed, we have to still be able to find and
            //    execute it without errors.

            // Arrange
            var renderer = new TestAsyncRenderer
            {
                NextUpdateDisplayReturnTask = Task.CompletedTask
            };
            var numEventsFired = 0;
            EventComponent component = null;
            Action<UIEventArgs> eventHandler = null;

            eventHandler = _ =>
            {
                numEventsFired++;

                // Replace the old event handler with a different one,
                // (old the old handler ID will be disposed) then re-render.
                component.OnTest = args => eventHandler(args);
                component.TriggerRender();
            };

            component = new EventComponent { OnTest = eventHandler };
            var componentId = renderer.AssignRootComponentId(component);
            component.TriggerRender();

            var eventHandlerId = renderer.Batches.Single()
                .ReferenceFrames
                .First(frame => frame.AttributeValue != null)
                .AttributeEventHandlerId;

            // Act/Assert 1: Event can be fired for the first time
            var render1TCS = new TaskCompletionSource<object>();
            renderer.NextUpdateDisplayReturnTask = render1TCS.Task;
            renderer.DispatchEvent(componentId, eventHandlerId, new UIEventArgs());
            Assert.Equal(1, numEventsFired);

            // Act/Assert 2: *Same* event handler ID can be reused prior to completion of
            // preceding UI update
            var render2TCS = new TaskCompletionSource<object>();
            renderer.NextUpdateDisplayReturnTask = render2TCS.Task;
            renderer.DispatchEvent(componentId, eventHandlerId, new UIEventArgs());
            Assert.Equal(2, numEventsFired);

            // Act/Assert 3: After we complete the first UI update in which a given
            // event handler ID is disposed, we can no longer reuse that event handler ID
            render1TCS.SetResult(null);
            await Task.Delay(500); // From here we can't see when the async disposal is completed. Just give it plenty of time (Task.Yield isn't enough).
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                renderer.DispatchEvent(componentId, eventHandlerId, new UIEventArgs());
            });
            Assert.Equal($"There is no event handler with ID {eventHandlerId}", ex.Message);
            Assert.Equal(2, numEventsFired);
        }

        private class NoOpRenderer : Renderer
        {
            public NoOpRenderer() : base(new TestServiceProvider())
            {
            }

            public new int AssignRootComponentId(IComponent component)
                => base.AssignRootComponentId(component);

            protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
                => Task.CompletedTask;
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
            [Parameter]
            internal string Message { get; set; }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.AddContent(0, Message);
            }
        }

        private class FakeComponent : IComponent
        {
            [Parameter]
            internal int IntProperty { get; private set; }

            [Parameter]
            internal string StringProperty { get; private set; }

            [Parameter]
            internal object ObjectProperty { get; set; }

            public RenderHandle RenderHandle { get; private set; }

            public void Init(RenderHandle renderHandle)
                => RenderHandle = renderHandle;

            public void SetParameters(ParameterCollection parameters)
                => parameters.SetParameterProperties(this);
        }

        private class EventComponent : AutoRenderComponent, IComponent, IHandleEvent
        {
            [Parameter]
            internal Action<UIEventArgs> OnTest { get; set; }

            [Parameter]
            internal Action<UIMouseEventArgs> OnClick { get; set; }

            [Parameter]
            internal Action OnClickAction { get; set; }

            public bool SkipElement { get; set; }
            private int renderCount = 0;

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, "grandparent");
                if (!SkipElement)
                {
                    builder.OpenElement(1, "parent");
                    builder.OpenElement(2, "some element");
                    if (OnTest != null)
                    {
                        builder.AddAttribute(3, "ontest", OnTest);
                    }
                    if (OnClick != null)
                    {
                        builder.AddAttribute(4, "onclick", OnClick);
                    }
                    if (OnClickAction != null)
                    {
                        builder.AddAttribute(5, "onclickaction", OnClickAction);
                    }
                    builder.CloseElement();
                    builder.CloseElement();
                }
                builder.CloseElement();
                builder.AddContent(6, $"Render count: {++renderCount}");
            }

            public void HandleEvent(EventHandlerInvoker binding, UIEventArgs args)
            {
                binding.Invoke(args);
            }
        }

        private class ConditionalParentComponent<T> : AutoRenderComponent where T : IComponent
        {
            [Parameter]
            internal bool IncludeChild { get; set; }

            [Parameter]
            internal IDictionary<string, object> ChildParameters { get; set; }

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
            [Parameter]
            internal TestComponent Parent { get; private set; }

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
            [Parameter]
            Action<object> OnClick { get; set; }

            private RenderHandle _renderHandle;

            public void Init(RenderHandle renderHandle)
                => _renderHandle = renderHandle;

            public void SetParameters(ParameterCollection parameters)
            {
                parameters.SetParameterProperties(this);
                Render();
            }

            public void HandleEvent(EventHandlerInvoker binding, UIEventArgs args)
            {
                var task = binding.Invoke(args);
                Render();
            }

            private void Render()
                => _renderHandle.Render(builder =>
                {
                    builder.OpenElement(0, "my button");
                    builder.AddAttribute(1, "my click handler", eventArgs => OnClick(eventArgs));
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

        private class BindPlusConditionalAttributeComponent : AutoRenderComponent, IHandleEvent
        {
            public bool CheckboxEnabled;
            public string SomeStringProperty;

            public void HandleEvent(EventHandlerInvoker binding, UIEventArgs args)
            {
                binding.Invoke(args);
                TriggerRender();
            }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, "input");
                builder.AddAttribute(1, "type", "checkbox");
                builder.AddAttribute(2, "value", BindMethods.GetValue(CheckboxEnabled));
                builder.AddAttribute(3, "onchange", BindMethods.SetValueHandler(__value => CheckboxEnabled = __value, CheckboxEnabled));
                builder.CloseElement();
                builder.OpenElement(4, "input");
                builder.AddAttribute(5, "value", BindMethods.GetValue(SomeStringProperty));
                builder.AddAttribute(6, "onchange", BindMethods.SetValueHandler(__value => SomeStringProperty = __value, SomeStringProperty));
                builder.AddAttribute(7, "disabled", !CheckboxEnabled);
                builder.CloseElement();
            }
        }

        private class AfterRenderCaptureComponent : AutoRenderComponent, IComponent, IHandleAfterRender
        {
            public int OnAfterRenderCallCount { get; private set; }

            public void OnAfterRender()
            {
                OnAfterRenderCallCount++;
            }

            void IComponent.SetParameters(ParameterCollection parameters)
            {
                TriggerRender();
            }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
            }
        }

        class TestAsyncRenderer : TestRenderer
        {
            public Task NextUpdateDisplayReturnTask { get; set; }

            protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
            {
                base.UpdateDisplayAsync(renderBatch);
                return NextUpdateDisplayReturnTask;
            }
        }
    }
}
