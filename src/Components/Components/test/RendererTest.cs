// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Components.CompilerServices;
using Microsoft.AspNetCore.Components.HotReload;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components.Test;

public class RendererTest
{
    // Nothing should exceed the timeout in a successful run of the the tests, this is just here to catch
    // failures.
    private static readonly TimeSpan Timeout = Debugger.IsAttached ? System.Threading.Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(10);

    private const string EventActionsName = nameof(NestedAsyncComponent.EventActions);
    private const string WhatToRenderName = nameof(NestedAsyncComponent.WhatToRender);
    private const string LogName = nameof(NestedAsyncComponent.Log);

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
            builder.AddComponentParameter(2, nameof(MessageComponent.Message), "Nested component output");
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
    public async Task CanRenderAsyncTopLevelComponents()
    {
        // Arrange
        var renderer = new TestRenderer();
        var tcs = new TaskCompletionSource();
        var component = new AsyncComponent(tcs.Task, 5); // Triggers n renders, the first one creating <p>n</p> and the n-1 renders asynchronously update the value.

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        var renderTask = renderer.Dispatcher.InvokeAsync(() => renderer.RenderRootComponentAsync(componentId));

        // Assert
        Assert.False(renderTask.IsCompleted);
        tcs.SetResult();
        await renderTask;
        Assert.Equal(5, renderer.Batches.Count);

        // First render
        var create = renderer.Batches[0];
        var diff = create.DiffsByComponentId[componentId].Single();
        Assert.Collection(diff.Edits,
            edit =>
            {
                Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                Assert.Equal(0, edit.ReferenceFrameIndex);
            });
        AssertFrame.Element(create.ReferenceFrames[0], "p", 2);
        AssertFrame.Text(create.ReferenceFrames[1], "5");

        // Second render
        for (var i = 1; i < 5; i++)
        {

            var update = renderer.Batches[i];
            var updateDiff = update.DiffsByComponentId[componentId].Single();
            Assert.Collection(updateDiff.Edits,
                edit =>
                {
                    Assert.Equal(RenderTreeEditType.StepIn, edit.Type);
                },
                edit =>
                {
                    Assert.Equal(RenderTreeEditType.UpdateText, edit.Type);
                },
                edit =>
                {
                    Assert.Equal(RenderTreeEditType.StepOut, edit.Type);
                });
            AssertFrame.Text(update.ReferenceFrames[0], (5 - i).ToString(CultureInfo.InvariantCulture));
        }
    }

    [Fact]
    public async Task CanRenderAsyncNestedComponents()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new NestedAsyncComponent();

        // Act/Assert
        var componentId = renderer.AssignRootComponentId(component);
        var log = new ConcurrentQueue<(int id, NestedAsyncComponent.EventType @event)>();
        await renderer.Dispatcher.InvokeAsync(() => renderer.RenderRootComponentAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [EventActionsName] = new Dictionary<int, IList<NestedAsyncComponent.ExecutionAction>>
            {
                [0] = new List<NestedAsyncComponent.ExecutionAction>
                    {
                        NestedAsyncComponent.ExecutionAction.On(0, NestedAsyncComponent.EventType.OnInit),
                        NestedAsyncComponent.ExecutionAction.On(0, NestedAsyncComponent.EventType.OnInitAsyncAsync, async:true),
                        NestedAsyncComponent.ExecutionAction.On(0, NestedAsyncComponent.EventType.OnParametersSet),
                        NestedAsyncComponent.ExecutionAction.On(0, NestedAsyncComponent.EventType.OnParametersSetAsyncAsync, async: true),
                    },
                [1] = new List<NestedAsyncComponent.ExecutionAction>
                    {
                        NestedAsyncComponent.ExecutionAction.On(1, NestedAsyncComponent.EventType.OnInit),
                        NestedAsyncComponent.ExecutionAction.On(1, NestedAsyncComponent.EventType.OnInitAsyncAsync, async:true),
                        NestedAsyncComponent.ExecutionAction.On(1, NestedAsyncComponent.EventType.OnParametersSet),
                        NestedAsyncComponent.ExecutionAction.On(1, NestedAsyncComponent.EventType.OnParametersSetAsyncAsync, async: true),
                    }
            },
            [WhatToRenderName] = new Dictionary<int, Func<NestedAsyncComponent, RenderFragment>>
            {
                [0] = CreateRenderFactory(new[] { 1 }),
                [1] = CreateRenderFactory(Array.Empty<int>())
            },
            [LogName] = log
        })));

        var logForParent = log.Where(l => l.id == 0).ToArray();
        var logForChild = log.Where(l => l.id == 1).ToArray();

        AssertStream(0, logForParent);
        AssertStream(1, logForChild);
    }

    [Fact]
    public void CanReRenderRootComponentsWithNewParameters()
    {
        // This differs from the other "CanReRender..." tests above in that the root component is being supplied
        // with new parameters from outside, as opposed to making its own decision to re-render.

        // Arrange
        var renderer = new TestRenderer();
        var component = new MessageComponent();
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponentAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(MessageComponent.Message)] = "Hello"
        }));

        // Assert 1: First render
        var batch = renderer.Batches.Single();
        var diff = batch.DiffsByComponentId[componentId].Single();
        Assert.Collection(diff.Edits, edit =>
        {
            Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
            Assert.Equal(0, edit.ReferenceFrameIndex);
        });
        AssertFrame.Text(batch.ReferenceFrames[0], "Hello");

        // Act 2: Update params
        renderer.RenderRootComponentAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(MessageComponent.Message)] = "Goodbye"
        }));

        // Assert 2: Second render
        var batch2 = renderer.Batches.Skip(1).Single();
        var diff2 = batch2.DiffsByComponentId[componentId].Single();
        Assert.Collection(diff2.Edits, edit =>
        {
            Assert.Equal(RenderTreeEditType.UpdateText, edit.Type);
            Assert.Equal(0, edit.ReferenceFrameIndex);
        });
        AssertFrame.Text(batch2.ReferenceFrames[0], "Goodbye");
    }

    [Fact]
    public async Task CanAddAndRenderNewRootComponentsWhileNotQuiescent()
    {
        // Arrange 1: An async root component
        var renderer = new TestRenderer();
        var tcs1 = new TaskCompletionSource();
        var component1 = new AsyncComponent(tcs1.Task, 1);
        var component1Id = renderer.AssignRootComponentId(component1);

        // Act/Assert 1: Its SetParametersAsync task remains incomplete
        var renderTask1 = renderer.Dispatcher.InvokeAsync(() => renderer.RenderRootComponentAsync(component1Id));
        Assert.False(renderTask1.IsCompleted);

        // Arrange/Act 2: Can add a second root component while not quiescent
        var tcs2 = new TaskCompletionSource();
        var component2 = new AsyncComponent(tcs2.Task, 1);
        var component2Id = renderer.AssignRootComponentId(component2);
        var renderTask2 = renderer.Dispatcher.InvokeAsync(() => renderer.RenderRootComponentAsync(component2Id));

        // Assert 2
        Assert.False(renderTask1.IsCompleted);
        Assert.False(renderTask2.IsCompleted);

        // Completing the first task isn't enough to consider the system quiescent, because there's now a second task
        tcs1.SetResult();

        // renderTask1 should not complete until we finish tcs2.
        // We can't really prove that absolutely, but at least show it doesn't happen during a certain time period.
        await Assert.ThrowsAsync<TimeoutException>(() => renderTask1.WaitAsync(TimeSpan.FromMilliseconds(250)));
        Assert.False(renderTask1.IsCompleted);
        Assert.False(renderTask2.IsCompleted);

        // Completing the second task does finally complete both render tasks
        tcs2.SetResult();
        await Task.WhenAll(renderTask1, renderTask2);
    }

    [Fact]
    public async Task AsyncComponentTriggeringRootReRenderDoesNotDeadlock()
    {
        // Arrange
        var renderer = new TestRenderer();
        var tcs = new TaskCompletionSource();
        int? componentId = null;
        var hasRendered = false;
        var component = new CallbackDuringSetParametersAsyncComponent
        {
            Callback = async () =>
            {
                await tcs.Task;
                if (!hasRendered)
                {
                    hasRendered = true;

                    // If we were to await here, then it would deadlock, because the component would be saying it's not
                    // finished rendering until the rendering system has already finished. The point of this test is to
                    // show that, as long as we don't await quiescence here, nothing within the system will be doing so
                    // and hence the whole process can complete.
                    _ = renderer.RenderRootComponentAsync(componentId.Value, ParameterView.Empty);
                }
            }
        };
        componentId = renderer.AssignRootComponentId(component);

        // Act
        var renderTask = renderer.Dispatcher.InvokeAsync(() => renderer.RenderRootComponentAsync(componentId.Value));

        // Assert
        Assert.False(renderTask.IsCompleted);
        tcs.SetResult();
        await renderTask;
    }

    [Fact]
    public async Task CanRenderAsyncComponentsWithSyncChildComponents()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new NestedAsyncComponent();

        // Act/Assert
        var componentId = renderer.AssignRootComponentId(component);
        var log = new ConcurrentQueue<(int id, NestedAsyncComponent.EventType @event)>();
        await renderer.Dispatcher.InvokeAsync(() => renderer.RenderRootComponentAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [EventActionsName] = new Dictionary<int, IList<NestedAsyncComponent.ExecutionAction>>
            {
                [0] = new List<NestedAsyncComponent.ExecutionAction>
                    {
                        NestedAsyncComponent.ExecutionAction.On(0, NestedAsyncComponent.EventType.OnInit),
                        NestedAsyncComponent.ExecutionAction.On(0, NestedAsyncComponent.EventType.OnInitAsyncAsync, async:true),
                        NestedAsyncComponent.ExecutionAction.On(0, NestedAsyncComponent.EventType.OnParametersSet),
                        NestedAsyncComponent.ExecutionAction.On(0, NestedAsyncComponent.EventType.OnParametersSetAsyncAsync, async: true),
                    },
                [1] = new List<NestedAsyncComponent.ExecutionAction>
                    {
                        NestedAsyncComponent.ExecutionAction.On(1, NestedAsyncComponent.EventType.OnInit),
                        NestedAsyncComponent.ExecutionAction.On(1, NestedAsyncComponent.EventType.OnInitAsyncAsync),
                        NestedAsyncComponent.ExecutionAction.On(1, NestedAsyncComponent.EventType.OnParametersSet),
                        NestedAsyncComponent.ExecutionAction.On(1, NestedAsyncComponent.EventType.OnParametersSetAsyncAsync),
                    }
            },
            [WhatToRenderName] = new Dictionary<int, Func<NestedAsyncComponent, RenderFragment>>
            {
                [0] = CreateRenderFactory(new[] { 1 }),
                [1] = CreateRenderFactory(Array.Empty<int>())
            },
            [LogName] = log
        })));

        var logForParent = log.Where(l => l.id == 0).ToArray();
        var logForChild = log.Where(l => l.id == 1).ToArray();

        AssertStream(0, logForParent);
        AssertStream(1, logForChild);
    }

    [Fact]
    public async Task CanRenderAsyncComponentsWithAsyncChildInit()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new NestedAsyncComponent();

        // Act/Assert
        var componentId = renderer.AssignRootComponentId(component);
        var log = new ConcurrentQueue<(int id, NestedAsyncComponent.EventType @event)>();
        await renderer.Dispatcher.InvokeAsync(() => renderer.RenderRootComponentAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [EventActionsName] = new Dictionary<int, IList<NestedAsyncComponent.ExecutionAction>>
            {
                [0] = new List<NestedAsyncComponent.ExecutionAction>
                    {
                        NestedAsyncComponent.ExecutionAction.On(0, NestedAsyncComponent.EventType.OnInit),
                        NestedAsyncComponent.ExecutionAction.On(0, NestedAsyncComponent.EventType.OnInitAsyncAsync, async:true),
                        NestedAsyncComponent.ExecutionAction.On(0, NestedAsyncComponent.EventType.OnParametersSet),
                        NestedAsyncComponent.ExecutionAction.On(0, NestedAsyncComponent.EventType.OnParametersSetAsyncAsync, async: true),
                    },
                [1] = new List<NestedAsyncComponent.ExecutionAction>
                    {
                        NestedAsyncComponent.ExecutionAction.On(1, NestedAsyncComponent.EventType.OnInit),
                        NestedAsyncComponent.ExecutionAction.On(1, NestedAsyncComponent.EventType.OnInitAsyncAsync, async:true),
                        NestedAsyncComponent.ExecutionAction.On(1, NestedAsyncComponent.EventType.OnParametersSet),
                        NestedAsyncComponent.ExecutionAction.On(1, NestedAsyncComponent.EventType.OnParametersSetAsyncAsync),
                    }
            },
            [WhatToRenderName] = new Dictionary<int, Func<NestedAsyncComponent, RenderFragment>>
            {
                [0] = CreateRenderFactory(new[] { 1 }),
                [1] = CreateRenderFactory(Array.Empty<int>())
            },
            [LogName] = log
        })));

        var logForParent = log.Where(l => l.id == 0).ToArray();
        var logForChild = log.Where(l => l.id == 1).ToArray();

        AssertStream(0, logForParent);
        AssertStream(1, logForChild);
    }

    [Fact]
    public async Task CanRenderAsyncComponentsWithMultipleAsyncChildren()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new NestedAsyncComponent();

        // Act/Assert
        var componentId = renderer.AssignRootComponentId(component);
        var log = new ConcurrentQueue<(int id, NestedAsyncComponent.EventType @event)>();
        await renderer.Dispatcher.InvokeAsync(() => renderer.RenderRootComponentAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [EventActionsName] = new Dictionary<int, IList<NestedAsyncComponent.ExecutionAction>>
            {
                [0] = new List<NestedAsyncComponent.ExecutionAction>
                    {
                        NestedAsyncComponent.ExecutionAction.On(0, NestedAsyncComponent.EventType.OnInit),
                        NestedAsyncComponent.ExecutionAction.On(0, NestedAsyncComponent.EventType.OnInitAsyncAsync, async:true),
                        NestedAsyncComponent.ExecutionAction.On(0, NestedAsyncComponent.EventType.OnParametersSet),
                        NestedAsyncComponent.ExecutionAction.On(0, NestedAsyncComponent.EventType.OnParametersSetAsyncAsync, async: true),
                    },
                [1] = new List<NestedAsyncComponent.ExecutionAction>
                    {
                        NestedAsyncComponent.ExecutionAction.On(1, NestedAsyncComponent.EventType.OnInit),
                        NestedAsyncComponent.ExecutionAction.On(1, NestedAsyncComponent.EventType.OnInitAsyncAsync, async:true),
                        NestedAsyncComponent.ExecutionAction.On(1, NestedAsyncComponent.EventType.OnParametersSet),
                        NestedAsyncComponent.ExecutionAction.On(1, NestedAsyncComponent.EventType.OnParametersSetAsyncAsync, async:true),
                    },
                [2] = new List<NestedAsyncComponent.ExecutionAction>
                    {
                        NestedAsyncComponent.ExecutionAction.On(2, NestedAsyncComponent.EventType.OnInit),
                        NestedAsyncComponent.ExecutionAction.On(2, NestedAsyncComponent.EventType.OnInitAsyncAsync, async:true),
                        NestedAsyncComponent.ExecutionAction.On(2, NestedAsyncComponent.EventType.OnParametersSet),
                        NestedAsyncComponent.ExecutionAction.On(2, NestedAsyncComponent.EventType.OnParametersSetAsyncAsync, async:true),
                    },
                [3] = new List<NestedAsyncComponent.ExecutionAction>
                    {
                        NestedAsyncComponent.ExecutionAction.On(3, NestedAsyncComponent.EventType.OnInit),
                        NestedAsyncComponent.ExecutionAction.On(3, NestedAsyncComponent.EventType.OnInitAsyncAsync, async:true),
                        NestedAsyncComponent.ExecutionAction.On(3, NestedAsyncComponent.EventType.OnParametersSet),
                        NestedAsyncComponent.ExecutionAction.On(3, NestedAsyncComponent.EventType.OnParametersSetAsyncAsync, async:true),
                    }
            },
            [WhatToRenderName] = new Dictionary<int, Func<NestedAsyncComponent, RenderFragment>>
            {
                [0] = CreateRenderFactory(new[] { 1, 2 }),
                [1] = CreateRenderFactory(new[] { 3 }),
                [2] = CreateRenderFactory(Array.Empty<int>()),
                [3] = CreateRenderFactory(Array.Empty<int>())
            },
            [LogName] = log
        })));

        var logForParent = log.Where(l => l.id == 0).ToArray();
        var logForFirstChild = log.Where(l => l.id == 1).ToArray();
        var logForSecondChild = log.Where(l => l.id == 2).ToArray();
        var logForThirdChild = log.Where(l => l.id == 3).ToArray();

        AssertStream(0, logForParent);
        AssertStream(1, logForFirstChild);
        AssertStream(2, logForSecondChild);
        AssertStream(3, logForThirdChild);
    }

    [Fact]
    public void DispatchingEventsWithoutAsyncWorkShouldCompleteSynchronously()
    {
        // Arrange: Render a component with an event handler
        var renderer = new TestRenderer();
        EventArgs receivedArgs = null;

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
        var eventArgs = new EventArgs();
        var task = renderer.DispatchEventAsync(eventHandlerId, eventArgs);

        // This should always be run synchronously
        Assert.True(task.IsCompletedSuccessfully);
    }

    [Fact]
    public void CanDispatchEventsToTopLevelComponents()
    {
        // Arrange: Render a component with an event handler
        var renderer = new TestRenderer();
        EventArgs receivedArgs = null;

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
        var eventArgs = new EventArgs();
        var renderTask = renderer.DispatchEventAsync(eventHandlerId, eventArgs);
        Assert.True(renderTask.IsCompletedSuccessfully);
        Assert.Same(eventArgs, receivedArgs);
    }

    [Fact]
    public void CanGetEventArgsTypeForHandler()
    {
        // Arrange: Render a component with an event handler
        var renderer = new TestRenderer();

        var component = new EventComponent
        {
            OnArbitraryDelegateEvent = (Func<DerivedEventArgs, Task>)(args => Task.CompletedTask),
        };
        var componentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();

        var eventHandlerId = renderer.Batches.Single()
            .ReferenceFrames
            .First(frame => frame.AttributeValue != null)
            .AttributeEventHandlerId;

        // Assert: Can determine event args type
        var eventArgsType = renderer.GetEventArgsType(eventHandlerId);
        Assert.Same(typeof(DerivedEventArgs), eventArgsType);
    }

    [Fact]
    public void CanGetEventArgsTypeForParameterlessHandler()
    {
        // Arrange: Render a component with an event handler
        var renderer = new TestRenderer();

        var component = new EventComponent
        {
            OnArbitraryDelegateEvent = (Func<Task>)(() => Task.CompletedTask),
        };
        var componentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();

        var eventHandlerId = renderer.Batches.Single()
            .ReferenceFrames
            .First(frame => frame.AttributeValue != null)
            .AttributeEventHandlerId;

        // Assert: Can determine event args type
        var eventArgsType = renderer.GetEventArgsType(eventHandlerId);
        Assert.Same(typeof(EventArgs), eventArgsType);
    }

    [Fact]
    public void CannotGetEventArgsTypeForMultiParameterHandler()
    {
        // Arrange: Render a component with an event handler
        var renderer = new TestRenderer();

        var component = new EventComponent
        {
            OnArbitraryDelegateEvent = (Action<EventArgs, string>)((x, y) => { }),
        };
        var componentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();

        var eventHandlerId = renderer.Batches.Single()
            .ReferenceFrames
            .First(frame => frame.AttributeValue != null)
            .AttributeEventHandlerId;

        // Assert: Cannot determine event args type
        var ex = Assert.Throws<InvalidOperationException>(() => renderer.GetEventArgsType(eventHandlerId));
        Assert.Contains("declares more than one parameter", ex.Message);
    }

    [Fact]
    public void CannotGetEventArgsTypeForHandlerWithNonEventArgsParameter()
    {
        // Arrange: Render a component with an event handler
        var renderer = new TestRenderer();

        var component = new EventComponent
        {
            OnArbitraryDelegateEvent = (Action<DateTime>)(arg => { }),
        };
        var componentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();

        var eventHandlerId = renderer.Batches.Single()
            .ReferenceFrames
            .First(frame => frame.AttributeValue != null)
            .AttributeEventHandlerId;

        // Assert: Cannot determine event args type
        var ex = Assert.Throws<InvalidOperationException>(() => renderer.GetEventArgsType(eventHandlerId));
        Assert.Contains($"must inherit from {typeof(EventArgs).FullName}", ex.Message);
    }

    [Fact]
    public void DispatchEventHandlesSynchronousExceptionsFromEventHandlers()
    {
        // Arrange: Render a component with an event handler
        var renderer = new TestRenderer
        {
            ShouldHandleExceptions = true
        };

        var component = new EventComponent
        {
            OnTest = args => throw new Exception("Error")
        };
        var componentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();

        var eventHandlerId = renderer.Batches.Single()
            .ReferenceFrames
            .First(frame => frame.AttributeValue != null)
            .AttributeEventHandlerId;

        // Assert: Event not yet fired
        Assert.Empty(renderer.HandledExceptions);

        // Act/Assert: Event can be fired
        var eventArgs = new EventArgs();
        var renderTask = renderer.DispatchEventAsync(eventHandlerId, eventArgs);
        Assert.True(renderTask.IsCompletedSuccessfully);

        var exception = Assert.Single(renderer.HandledExceptions);
        Assert.Equal("Error", exception.Message);
    }

    [Fact]
    public void CanDispatchTypedEventsToTopLevelComponents()
    {
        // Arrange: Render a component with an event handler
        var renderer = new TestRenderer();
        DerivedEventArgs receivedArgs = null;

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
        var eventArgs = new DerivedEventArgs();
        var renderTask = renderer.DispatchEventAsync(eventHandlerId, eventArgs);
        Assert.True(renderTask.IsCompletedSuccessfully);
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
        var eventArgs = new DerivedEventArgs();
        var renderTask = renderer.DispatchEventAsync(eventHandlerId, eventArgs);
        Assert.True(renderTask.IsCompletedSuccessfully);
        Assert.NotNull(receivedArgs);
    }

    [Fact]
    public void CanDispatchEventsToNestedComponents()
    {
        EventArgs receivedArgs = null;

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
        var eventArgs = new EventArgs();
        var renderTask = renderer.DispatchEventAsync(eventHandlerId, eventArgs);
        Assert.True(renderTask.IsCompletedSuccessfully);
        Assert.Same(eventArgs, receivedArgs);
    }

    [Fact]
    public async Task CanAsyncDispatchEventsToTopLevelComponents()
    {
        // Arrange: Render a component with an event handler
        var renderer = new TestRenderer();
        EventArgs receivedArgs = null;

        var state = 0;
        var tcs = new TaskCompletionSource();

        var component = new EventComponent
        {
            OnTestAsync = async (args) =>
            {
                receivedArgs = args;
                state = 1;
                await tcs.Task;
                state = 2;
            },
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
        var eventArgs = new EventArgs();
        var task = renderer.DispatchEventAsync(eventHandlerId, eventArgs);
        Assert.Equal(1, state);
        Assert.Same(eventArgs, receivedArgs);

        tcs.SetResult();
        await task;

        Assert.Equal(2, state);
    }

    [Fact]
    public async Task CanAsyncDispatchTypedEventsToTopLevelComponents()
    {
        // Arrange: Render a component with an event handler
        var renderer = new TestRenderer();
        DerivedEventArgs receivedArgs = null;

        var state = 0;
        var tcs = new TaskCompletionSource();

        var component = new EventComponent
        {
            OnClickAsync = async (args) =>
            {
                receivedArgs = args;
                state = 1;
                await tcs.Task;
                state = 2;
            }
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
        var eventArgs = new DerivedEventArgs();
        var task = renderer.DispatchEventAsync(eventHandlerId, eventArgs);
        Assert.Equal(1, state);
        Assert.Same(eventArgs, receivedArgs);

        tcs.SetResult();
        await task;

        Assert.Equal(2, state);
    }

    [Fact]
    public async Task CanAsyncDispatchActionEventsToTopLevelComponents()
    {
        // Arrange: Render a component with an event handler
        var renderer = new TestRenderer();
        object receivedArgs = null;

        var state = 0;
        var tcs = new TaskCompletionSource();

        var component = new EventComponent
        {
            OnClickAsyncAction = async () =>
            {
                receivedArgs = new object();
                state = 1;
                await tcs.Task;
                state = 2;
            }
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
        var eventArgs = new DerivedEventArgs();
        var task = renderer.DispatchEventAsync(eventHandlerId, eventArgs);
        Assert.Equal(1, state);
        Assert.NotNull(receivedArgs);

        tcs.SetResult();
        await task;

        Assert.Equal(2, state);
    }

    [Fact]
    public async Task CanAsyncDispatchEventsToNestedComponents()
    {
        EventArgs receivedArgs = null;

        var state = 0;
        var tcs = new TaskCompletionSource();

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
        nestedComponent.OnTestAsync = async (args) =>
        {
            receivedArgs = args;
            state = 1;
            await tcs.Task;
            state = 2;
        };
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
        var eventArgs = new EventArgs();
        var task = renderer.DispatchEventAsync(eventHandlerId, eventArgs);
        Assert.Equal(1, state);
        Assert.Same(eventArgs, receivedArgs);

        tcs.SetResult();
        await task;

        Assert.Equal(2, state);
    }

    // This tests the behaviour of dispatching an event when the event-handler
    // delegate is a bound-delegate with a target that points to the parent component.
    //
    // This is a very common case when a component accepts a delegate parameter that
    // will be hooked up to a DOM event handler. It's essential that this will dispatch
    // to the parent component so that manual StateHasChanged calls are not necessary.
    [Fact]
    public async Task EventDispatching_DelegateParameter_MethodToDelegateConversion()
    {
        // Arrange
        var outerStateChangeCount = 0;

        var renderer = new TestRenderer();
        var parentComponent = new OuterEventComponent();
        parentComponent.RenderFragment = (builder) =>
        {
            builder.OpenComponent<EventComponent>(0);
            builder.AddComponentParameter(1, nameof(EventComponent.OnClickAction), (Action)parentComponent.SomeMethod);
            builder.CloseComponent();
        };
        parentComponent.OnEvent = () =>
        {
            outerStateChangeCount++;
        };

        var parentComponentId = renderer.AssignRootComponentId(parentComponent);
        await parentComponent.TriggerRenderAsync();

        var eventHandlerId = renderer.Batches[0]
            .ReferenceFrames
            .First(frame => frame.AttributeName == "onclickaction")
            .AttributeEventHandlerId;

        // Act
        var eventArgs = new DerivedEventArgs();
        await renderer.DispatchEventAsync(eventHandlerId, eventArgs);

        // Assert
        Assert.Equal(1, parentComponent.SomeMethodCallCount);
        Assert.Equal(1, outerStateChangeCount);
    }

    // This is the inverse case of EventDispatching_DelegateParameter_MethodToDelegateConversion
    // where the event-handling delegate has a target that is not a component.
    //
    // This is a degenerate case that we don't expect to occur in applications often,
    // but it's important to verify the semantics.
    [Fact]
    public async Task EventDispatching_DelegateParameter_NoTargetLambda()
    {
        // Arrange
        var outerStateChangeCount = 0;

        var renderer = new TestRenderer();
        var parentComponent = new OuterEventComponent();
        parentComponent.RenderFragment = (builder) =>
        {
            builder.OpenComponent<EventComponent>(0);
            builder.AddComponentParameter(1, nameof(EventComponent.OnClickAction), (Action)(() =>
            {
                parentComponent.SomeMethod();
            }));
            builder.CloseComponent();
        };
        parentComponent.OnEvent = () =>
        {
            outerStateChangeCount++;
        };

        var parentComponentId = renderer.AssignRootComponentId(parentComponent);
        await parentComponent.TriggerRenderAsync();

        var eventHandlerId = renderer.Batches[0]
            .ReferenceFrames
            .First(frame => frame.AttributeName == "onclickaction")
            .AttributeEventHandlerId;

        // Act
        var eventArgs = new DerivedEventArgs();
        await renderer.DispatchEventAsync(eventHandlerId, eventArgs);

        // Assert
        Assert.Equal(1, parentComponent.SomeMethodCallCount);
        Assert.Equal(0, outerStateChangeCount);
    }

    // This is a similar case to EventDispatching_DelegateParameter_MethodToDelegateConversion
    // but uses our event handling infrastructure to achieve the same effect. The call to CreateDelegate
    // is not necessary for correctness in this case - it should just no op.
    [Fact]
    public async Task EventDispatching_EventCallback_MethodToDelegateConversion()
    {
        // Arrange
        var outerStateChangeCount = 0;

        var renderer = new TestRenderer();
        var parentComponent = new OuterEventComponent();
        parentComponent.RenderFragment = (builder) =>
        {
            builder.OpenComponent<EventComponent>(0);
            builder.AddComponentParameter(1, nameof(EventComponent.OnClickEventCallback), EventCallback.Factory.Create(parentComponent, (Action)parentComponent.SomeMethod));
            builder.CloseComponent();
        };
        parentComponent.OnEvent = () =>
        {
            outerStateChangeCount++;
        };

        var parentComponentId = renderer.AssignRootComponentId(parentComponent);
        await parentComponent.TriggerRenderAsync();

        var eventHandlerId = renderer.Batches[0]
            .ReferenceFrames
            .First(frame => frame.AttributeName == "onclick")
            .AttributeEventHandlerId;

        // Act
        var eventArgs = new DerivedEventArgs();
        await renderer.DispatchEventAsync(eventHandlerId, eventArgs);

        // Assert
        Assert.Equal(1, parentComponent.SomeMethodCallCount);
        Assert.Equal(1, outerStateChangeCount);
    }

    // This is a similar case to EventDispatching_DelegateParameter_NoTargetLambda but it uses
    // our event-handling infrastructure to avoid the need for a manual StateHasChanged()
    [Fact]
    public async Task EventDispatching_EventCallback_NoTargetLambda()
    {
        // Arrange
        var outerStateChangeCount = 0;

        var renderer = new TestRenderer();
        var parentComponent = new OuterEventComponent();
        parentComponent.RenderFragment = (builder) =>
        {
            builder.OpenComponent<EventComponent>(0);
            builder.AddComponentParameter(1, nameof(EventComponent.OnClickEventCallback), EventCallback.Factory.Create(parentComponent, (Action)(() =>
            {
                parentComponent.SomeMethod();
            })));
            builder.CloseComponent();
        };
        parentComponent.OnEvent = () =>
        {
            outerStateChangeCount++;
        };

        var parentComponentId = renderer.AssignRootComponentId(parentComponent);
        await parentComponent.TriggerRenderAsync();

        var eventHandlerId = renderer.Batches[0]
            .ReferenceFrames
            .First(frame => frame.AttributeName == "onclick")
            .AttributeEventHandlerId;

        // Act
        var eventArgs = new DerivedEventArgs();
        await renderer.DispatchEventAsync(eventHandlerId, eventArgs);

        // Assert
        Assert.Equal(1, parentComponent.SomeMethodCallCount);
        Assert.Equal(1, outerStateChangeCount);
    }

    // This is a similar case to EventDispatching_DelegateParameter_NoTargetLambda but it uses
    // our event-handling infrastructure to avoid the need for a manual StateHasChanged()
    [Fact]
    public async Task EventDispatching_EventCallback_AsyncNoTargetLambda()
    {
        // Arrange
        var outerStateChangeCount = 0;

        var renderer = new TestRenderer();
        var parentComponent = new OuterEventComponent();
        parentComponent.RenderFragment = (builder) =>
        {
            builder.OpenComponent<EventComponent>(0);
            builder.AddComponentParameter(1, nameof(EventComponent.OnClickEventCallback), EventCallback.Factory.Create(parentComponent, (Func<Task>)(() =>
            {
                parentComponent.SomeMethod();
                return Task.CompletedTask;
            })));
            builder.CloseComponent();
        };
        parentComponent.OnEvent = () =>
        {
            outerStateChangeCount++;
        };

        var parentComponentId = renderer.AssignRootComponentId(parentComponent);
        await parentComponent.TriggerRenderAsync();

        var eventHandlerId = renderer.Batches[0]
            .ReferenceFrames
            .First(frame => frame.AttributeName == "onclick")
            .AttributeEventHandlerId;

        // Act
        var eventArgs = new DerivedEventArgs();
        await renderer.DispatchEventAsync(eventHandlerId, eventArgs);

        // Assert
        Assert.Equal(1, parentComponent.SomeMethodCallCount);
        Assert.Equal(1, outerStateChangeCount);
    }

    [Fact]
    public async Task EventDispatching_EventCallbackOfT_MethodToDelegateConversion()
    {
        // Arrange
        var outerStateChangeCount = 0;

        var renderer = new TestRenderer();
        var parentComponent = new OuterEventComponent();
        parentComponent.RenderFragment = (builder) =>
        {
            builder.OpenComponent<EventComponent>(0);
            builder.AddComponentParameter(1, nameof(EventComponent.OnClickEventCallbackOfT), EventCallback.Factory.Create<DerivedEventArgs>(parentComponent, (Action)parentComponent.SomeMethod));
            builder.CloseComponent();
        };
        parentComponent.OnEvent = () =>
        {
            outerStateChangeCount++;
        };

        var parentComponentId = renderer.AssignRootComponentId(parentComponent);
        await parentComponent.TriggerRenderAsync();

        var eventHandlerId = renderer.Batches[0]
            .ReferenceFrames
            .First(frame => frame.AttributeName == "onclick")
            .AttributeEventHandlerId;

        // Act
        var eventArgs = new DerivedEventArgs();
        await renderer.DispatchEventAsync(eventHandlerId, eventArgs);

        // Assert
        Assert.Equal(1, parentComponent.SomeMethodCallCount);
        Assert.Equal(1, outerStateChangeCount);
    }

    // This is a similar case to EventDispatching_DelegateParameter_NoTargetLambda but it uses
    // our event-handling infrastructure to avoid the need for a manual StateHasChanged()
    [Fact]
    public async Task EventDispatching_EventCallbackOfT_NoTargetLambda()
    {
        // Arrange
        var outerStateChangeCount = 0;

        var renderer = new TestRenderer();
        var parentComponent = new OuterEventComponent();
        parentComponent.RenderFragment = (builder) =>
        {
            builder.OpenComponent<EventComponent>(0);
            builder.AddComponentParameter(1, nameof(EventComponent.OnClickEventCallbackOfT), EventCallback.Factory.Create<DerivedEventArgs>(parentComponent, (Action)(() =>
            {
                parentComponent.SomeMethod();
            })));
            builder.CloseComponent();
        };
        parentComponent.OnEvent = () =>
        {
            outerStateChangeCount++;
        };

        var parentComponentId = renderer.AssignRootComponentId(parentComponent);
        await parentComponent.TriggerRenderAsync();

        var eventHandlerId = renderer.Batches[0]
            .ReferenceFrames
            .First(frame => frame.AttributeName == "onclick")
            .AttributeEventHandlerId;

        // Act
        var eventArgs = new DerivedEventArgs();
        await renderer.DispatchEventAsync(eventHandlerId, eventArgs);

        // Assert
        Assert.Equal(1, parentComponent.SomeMethodCallCount);
        Assert.Equal(1, outerStateChangeCount);
    }

    // This is a similar case to EventDispatching_DelegateParameter_NoTargetLambda but it uses
    // our event-handling infrastructure to avoid the need for a manual StateHasChanged()
    [Fact]
    public async Task EventDispatching_EventCallbackOfT_AsyncNoTargetLambda()
    {
        // Arrange
        var outerStateChangeCount = 0;

        var renderer = new TestRenderer();
        var parentComponent = new OuterEventComponent();
        parentComponent.RenderFragment = (builder) =>
        {
            builder.OpenComponent<EventComponent>(0);
            builder.AddComponentParameter(1, nameof(EventComponent.OnClickEventCallbackOfT), EventCallback.Factory.Create<DerivedEventArgs>(parentComponent, (Func<Task>)(() =>
            {
                parentComponent.SomeMethod();
                return Task.CompletedTask;
            })));
            builder.CloseComponent();
        };
        parentComponent.OnEvent = () =>
        {
            outerStateChangeCount++;
        };

        var parentComponentId = renderer.AssignRootComponentId(parentComponent);
        await parentComponent.TriggerRenderAsync();

        var eventHandlerId = renderer.Batches[0]
            .ReferenceFrames
            .First(frame => frame.AttributeName == "onclick")
            .AttributeEventHandlerId;

        // Act
        var eventArgs = new DerivedEventArgs();
        await renderer.DispatchEventAsync(eventHandlerId, eventArgs);

        // Assert
        Assert.Equal(1, parentComponent.SomeMethodCallCount);
        Assert.Equal(1, outerStateChangeCount);
    }

    [Fact]
    public async Task DispatchEventAsync_Delegate_SynchronousCompletion()
    {
        // Arrange
        var renderer = new TestRenderer();
        var parentComponent = new OuterEventComponent
        {
            RenderFragment = (builder) =>
        {
            builder.OpenComponent<EventComponent>(0);
            builder.AddComponentParameter(1, nameof(EventComponent.OnClickAction), (Action)(() =>
            {
                // Do nothing.
            }));
            builder.CloseComponent();
        }
        };

        var parentComponentId = renderer.AssignRootComponentId(parentComponent);
        await parentComponent.TriggerRenderAsync();

        var eventHandlerId = renderer.Batches[0]
            .ReferenceFrames
            .First(frame => frame.AttributeName == "onclickaction")
            .AttributeEventHandlerId;

        // Act
        var task = renderer.DispatchEventAsync(eventHandlerId, new DerivedEventArgs());

        // Assert
        Assert.Equal(TaskStatus.RanToCompletion, task.Status);
        await task; // Does not throw
    }

    [Fact]
    public async Task DispatchEventAsync_EventCallback_SynchronousCompletion()
    {
        // Arrange
        var renderer = new TestRenderer();
        var parentComponent = new OuterEventComponent();
        parentComponent.RenderFragment = (builder) =>
        {
            builder.OpenComponent<EventComponent>(0);
            builder.AddComponentParameter(1, nameof(EventComponent.OnClickEventCallback), EventCallback.Factory.Create(parentComponent, (Action)(() =>
            {
                // Do nothing.
            })));
            builder.CloseComponent();
        };

        var parentComponentId = renderer.AssignRootComponentId(parentComponent);
        await parentComponent.TriggerRenderAsync();

        var eventHandlerId = renderer.Batches[0]
            .ReferenceFrames
            .First(frame => frame.AttributeName == "onclick")
            .AttributeEventHandlerId;

        // Act
        var task = renderer.DispatchEventAsync(eventHandlerId, new DerivedEventArgs());

        // Assert
        Assert.Equal(TaskStatus.RanToCompletion, task.Status);
        await task; // Does not throw
    }

    [Fact]
    public async Task DispatchEventAsync_EventCallbackOfT_SynchronousCompletion()
    {
        // Arrange
        DerivedEventArgs arg = null;

        var renderer = new TestRenderer();
        var parentComponent = new OuterEventComponent();
        parentComponent.RenderFragment = (builder) =>
        {
            builder.OpenComponent<EventComponent>(0);
            builder.AddComponentParameter(1, nameof(EventComponent.OnClickEventCallbackOfT), EventCallback.Factory.Create(parentComponent, (Action<DerivedEventArgs>)((e) =>
            {
                arg = e;
            })));
            builder.CloseComponent();
        };

        var parentComponentId = renderer.AssignRootComponentId(parentComponent);
        await parentComponent.TriggerRenderAsync();

        var eventHandlerId = renderer.Batches[0]
            .ReferenceFrames
            .First(frame => frame.AttributeName == "onclick")
            .AttributeEventHandlerId;

        // Act
        var task = renderer.DispatchEventAsync(eventHandlerId, new DerivedEventArgs());

        // Assert
        Assert.NotNull(arg);
        Assert.Equal(TaskStatus.RanToCompletion, task.Status);
        await task; // Does not throw
    }

    [Fact]
    public async Task DispatchEventAsync_Delegate_SynchronousCancellation()
    {
        // Arrange
        var renderer = new TestRenderer();
        var parentComponent = new OuterEventComponent
        {
            RenderFragment = (builder) =>
        {
            builder.OpenComponent<EventComponent>(0);
            builder.AddComponentParameter(1, nameof(EventComponent.OnClickAction), (Action)(() =>
            {
                throw new OperationCanceledException();
            }));
            builder.CloseComponent();
        }
        };

        var parentComponentId = renderer.AssignRootComponentId(parentComponent);
        await parentComponent.TriggerRenderAsync();

        var eventHandlerId = renderer.Batches[0]
            .ReferenceFrames
            .First(frame => frame.AttributeName == "onclickaction")
            .AttributeEventHandlerId;

        // Act
        var task = renderer.DispatchEventAsync(eventHandlerId, new DerivedEventArgs());

        // Assert
        Assert.Equal(TaskStatus.Canceled, task.Status);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    [Fact]
    public async Task DispatchEventAsync_EventCallback_SynchronousCancellation()
    {
        // Arrange
        var renderer = new TestRenderer();
        var parentComponent = new OuterEventComponent();
        parentComponent.RenderFragment = (builder) =>
        {
            builder.OpenComponent<EventComponent>(0);
            builder.AddComponentParameter(1, nameof(EventComponent.OnClickEventCallback), EventCallback.Factory.Create(parentComponent, (Action)(() =>
            {
                throw new OperationCanceledException();
            })));
            builder.CloseComponent();
        };

        var parentComponentId = renderer.AssignRootComponentId(parentComponent);
        await parentComponent.TriggerRenderAsync();

        var eventHandlerId = renderer.Batches[0]
            .ReferenceFrames
            .First(frame => frame.AttributeName == "onclick")
            .AttributeEventHandlerId;

        // Act
        var task = renderer.DispatchEventAsync(eventHandlerId, new DerivedEventArgs());

        // Assert
        Assert.Equal(TaskStatus.Canceled, task.Status);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    [Fact]
    public async Task DispatchEventAsync_EventCallbackOfT_SynchronousCancellation()
    {
        // Arrange
        DerivedEventArgs arg = null;

        var renderer = new TestRenderer();
        var parentComponent = new OuterEventComponent();
        parentComponent.RenderFragment = (builder) =>
        {
            builder.OpenComponent<EventComponent>(0);
            builder.AddComponentParameter(1, nameof(EventComponent.OnClickEventCallbackOfT), EventCallback.Factory.Create(parentComponent, (Action<DerivedEventArgs>)((e) =>
            {
                arg = e;
                throw new OperationCanceledException();
            })));
            builder.CloseComponent();
        };

        var parentComponentId = renderer.AssignRootComponentId(parentComponent);
        await parentComponent.TriggerRenderAsync();

        var eventHandlerId = renderer.Batches[0]
            .ReferenceFrames
            .First(frame => frame.AttributeName == "onclick")
            .AttributeEventHandlerId;

        // Act
        var task = renderer.DispatchEventAsync(eventHandlerId, new DerivedEventArgs());

        // Assert
        Assert.NotNull(arg);
        Assert.Equal(TaskStatus.Canceled, task.Status);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    [Fact]
    public async Task DispatchEventAsync_Delegate_SynchronousException()
    {
        // Arrange
        var renderer = new TestRenderer();
        var parentComponent = new OuterEventComponent
        {
            RenderFragment = (builder) =>
        {
            builder.OpenComponent<EventComponent>(0);
            builder.AddComponentParameter(1, nameof(EventComponent.OnClickAction), (Action)(() =>
            {
                throw new InvalidTimeZoneException();
            }));
            builder.CloseComponent();
        }
        };

        var parentComponentId = renderer.AssignRootComponentId(parentComponent);
        await parentComponent.TriggerRenderAsync();

        var eventHandlerId = renderer.Batches[0]
            .ReferenceFrames
            .First(frame => frame.AttributeName == "onclickaction")
            .AttributeEventHandlerId;

        // Act
        var task = renderer.DispatchEventAsync(eventHandlerId, new DerivedEventArgs());

        // Assert
        Assert.Equal(TaskStatus.Faulted, task.Status);
        await Assert.ThrowsAsync<InvalidTimeZoneException>(() => task);
    }

    [Fact]
    public async Task DispatchEventAsync_EventCallback_SynchronousException()
    {
        // Arrange
        var renderer = new TestRenderer();
        var parentComponent = new OuterEventComponent();
        parentComponent.RenderFragment = (builder) =>
        {
            builder.OpenComponent<EventComponent>(0);
            builder.AddComponentParameter(1, nameof(EventComponent.OnClickEventCallback), EventCallback.Factory.Create(parentComponent, (Action)(() =>
            {
                throw new InvalidTimeZoneException();
            })));
            builder.CloseComponent();
        };

        var parentComponentId = renderer.AssignRootComponentId(parentComponent);
        await parentComponent.TriggerRenderAsync();

        var eventHandlerId = renderer.Batches[0]
            .ReferenceFrames
            .First(frame => frame.AttributeName == "onclick")
            .AttributeEventHandlerId;

        // Act
        var task = renderer.DispatchEventAsync(eventHandlerId, new DerivedEventArgs());

        // Assert
        Assert.Equal(TaskStatus.Faulted, task.Status);
        await Assert.ThrowsAsync<InvalidTimeZoneException>(() => task);
    }

    [Fact]
    public async Task DispatchEventAsync_EventCallbackOfT_SynchronousException()
    {
        // Arrange
        DerivedEventArgs arg = null;

        var renderer = new TestRenderer();
        var parentComponent = new OuterEventComponent();
        parentComponent.RenderFragment = (builder) =>
        {
            builder.OpenComponent<EventComponent>(0);
            builder.AddComponentParameter(1, nameof(EventComponent.OnClickEventCallbackOfT), EventCallback.Factory.Create<DerivedEventArgs>(parentComponent, (Action<DerivedEventArgs>)((e) =>
            {
                arg = e;
                throw new InvalidTimeZoneException();
            })));
            builder.CloseComponent();
        };

        var parentComponentId = renderer.AssignRootComponentId(parentComponent);
        await parentComponent.TriggerRenderAsync();

        var eventHandlerId = renderer.Batches[0]
            .ReferenceFrames
            .First(frame => frame.AttributeName == "onclick")
            .AttributeEventHandlerId;

        // Act
        var task = renderer.DispatchEventAsync(eventHandlerId, new DerivedEventArgs());

        // Assert
        Assert.NotNull(arg);
        Assert.Equal(TaskStatus.Faulted, task.Status);
        await Assert.ThrowsAsync<InvalidTimeZoneException>(() => task);
    }

    [Fact]
    public async Task DispatchEventAsync_Delegate_AsynchronousCompletion()
    {
        // Arrange
        var tcs = new TaskCompletionSource();

        var renderer = new TestRenderer();
        var parentComponent = new OuterEventComponent
        {
            RenderFragment = (builder) =>
        {
            builder.OpenComponent<EventComponent>(0);
            builder.AddComponentParameter(1, nameof(EventComponent.OnClickAsyncAction), (Func<Task>)(async () =>
            {
                await tcs.Task;
            }));
            builder.CloseComponent();
        }
        };

        var parentComponentId = renderer.AssignRootComponentId(parentComponent);
        await parentComponent.TriggerRenderAsync();

        var eventHandlerId = renderer.Batches[0]
            .ReferenceFrames
            .First(frame => frame.AttributeName == "onclickaction")
            .AttributeEventHandlerId;

        // Act
        var task = renderer.DispatchEventAsync(eventHandlerId, new DerivedEventArgs());

        // Assert
        Assert.Equal(TaskStatus.WaitingForActivation, task.Status);
        tcs.SetResult();
        await task; // Does not throw
    }

    [Fact]
    public async Task DispatchEventAsync_EventCallback_AsynchronousCompletion()
    {
        // Arrange
        var tcs = new TaskCompletionSource();

        var renderer = new TestRenderer();
        var parentComponent = new OuterEventComponent();
        parentComponent.RenderFragment = (builder) =>
        {
            builder.OpenComponent<EventComponent>(0);
            builder.AddComponentParameter(1, nameof(EventComponent.OnClickEventCallback), EventCallback.Factory.Create(parentComponent, async () =>
            {
                await tcs.Task;
            }));
            builder.CloseComponent();
        };

        var parentComponentId = renderer.AssignRootComponentId(parentComponent);
        await parentComponent.TriggerRenderAsync();

        var eventHandlerId = renderer.Batches[0]
            .ReferenceFrames
            .First(frame => frame.AttributeName == "onclick")
            .AttributeEventHandlerId;

        // Act
        var task = renderer.DispatchEventAsync(eventHandlerId, new DerivedEventArgs());

        // Assert
        Assert.Equal(TaskStatus.WaitingForActivation, task.Status);
        tcs.SetResult();
        await task; // Does not throw
    }

    [Fact]
    public async Task DispatchEventAsync_EventCallbackOfT_AsynchronousCompletion()
    {
        // Arrange
        var tcs = new TaskCompletionSource();

        DerivedEventArgs arg = null;

        var renderer = new TestRenderer();
        var parentComponent = new OuterEventComponent();
        parentComponent.RenderFragment = (builder) =>
        {
            builder.OpenComponent<EventComponent>(0);
            builder.AddComponentParameter(1, nameof(EventComponent.OnClickEventCallbackOfT), EventCallback.Factory.Create<DerivedEventArgs>(parentComponent, async (e) =>
            {
                arg = e;
                await tcs.Task;
            }));
            builder.CloseComponent();
        };

        var parentComponentId = renderer.AssignRootComponentId(parentComponent);
        await parentComponent.TriggerRenderAsync();

        var eventHandlerId = renderer.Batches[0]
            .ReferenceFrames
            .First(frame => frame.AttributeName == "onclick")
            .AttributeEventHandlerId;

        // Act
        var task = renderer.DispatchEventAsync(eventHandlerId, new DerivedEventArgs());

        // Assert
        Assert.NotNull(arg);
        Assert.Equal(TaskStatus.WaitingForActivation, task.Status);
        tcs.SetResult();
        await task; // Does not throw
    }

    [Fact]
    public async Task DispatchEventAsync_Delegate_AsynchronousCancellation()
    {
        // Arrange
        var tcs = new TaskCompletionSource();

        var renderer = new TestRenderer();
        var parentComponent = new OuterEventComponent
        {
            RenderFragment = (builder) =>
        {
            builder.OpenComponent<EventComponent>(0);
            builder.AddComponentParameter(1, nameof(EventComponent.OnClickAsyncAction), (Func<Task>)(async () =>
            {
                await tcs.Task;
                throw new TaskCanceledException();
            }));
            builder.CloseComponent();
        }
        };

        var parentComponentId = renderer.AssignRootComponentId(parentComponent);
        await parentComponent.TriggerRenderAsync();

        var eventHandlerId = renderer.Batches[0]
            .ReferenceFrames
            .First(frame => frame.AttributeName == "onclickaction")
            .AttributeEventHandlerId;

        // Act
        var task = renderer.DispatchEventAsync(eventHandlerId, new DerivedEventArgs());

        // Assert
        Assert.Equal(TaskStatus.WaitingForActivation, task.Status);
        tcs.SetResult();

        await task; // Does not throw
        Assert.Empty(renderer.HandledExceptions);
    }

    [Fact]
    public async Task DispatchEventAsync_EventCallback_AsynchronousCancellation()
    {
        // Arrange
        var tcs = new TaskCompletionSource();

        var renderer = new TestRenderer();
        var parentComponent = new OuterEventComponent();
        parentComponent.RenderFragment = (builder) =>
        {
            builder.OpenComponent<EventComponent>(0);
            builder.AddComponentParameter(1, nameof(EventComponent.OnClickEventCallback), EventCallback.Factory.Create(parentComponent, async () =>
            {
                await tcs.Task;
                throw new TaskCanceledException();
            }));
            builder.CloseComponent();
        };

        var parentComponentId = renderer.AssignRootComponentId(parentComponent);
        await parentComponent.TriggerRenderAsync();

        var eventHandlerId = renderer.Batches[0]
            .ReferenceFrames
            .First(frame => frame.AttributeName == "onclick")
            .AttributeEventHandlerId;

        // Act
        var task = renderer.DispatchEventAsync(eventHandlerId, new DerivedEventArgs());

        // Assert
        Assert.Equal(TaskStatus.WaitingForActivation, task.Status);
        tcs.SetResult();

        await task; // Does not throw
        Assert.Empty(renderer.HandledExceptions);
    }

    [Fact]
    public async Task DispatchEventAsync_EventCallbackOfT_AsynchronousCancellation()
    {
        // Arrange
        var tcs = new TaskCompletionSource();

        DerivedEventArgs arg = null;

        var renderer = new TestRenderer();
        var parentComponent = new OuterEventComponent();
        parentComponent.RenderFragment = (builder) =>
        {
            builder.OpenComponent<EventComponent>(0);
            builder.AddComponentParameter(1, nameof(EventComponent.OnClickEventCallbackOfT), EventCallback.Factory.Create<DerivedEventArgs>(parentComponent, async (e) =>
            {
                arg = e;
                await tcs.Task;
                throw new TaskCanceledException();
            }));
            builder.CloseComponent();
        };

        var parentComponentId = renderer.AssignRootComponentId(parentComponent);
        await parentComponent.TriggerRenderAsync();

        var eventHandlerId = renderer.Batches[0]
            .ReferenceFrames
            .First(frame => frame.AttributeName == "onclick")
            .AttributeEventHandlerId;

        // Act
        var task = renderer.DispatchEventAsync(eventHandlerId, new DerivedEventArgs());

        // Assert
        Assert.NotNull(arg);
        Assert.Equal(TaskStatus.WaitingForActivation, task.Status);
        tcs.SetResult();

        await task; // Does not throw
        Assert.Empty(renderer.HandledExceptions);
    }

    [Fact]
    public async Task DispatchEventAsync_Delegate_AsynchronousException()
    {
        // Arrange
        var tcs = new TaskCompletionSource();

        var renderer = new TestRenderer();
        var parentComponent = new OuterEventComponent
        {
            RenderFragment = (builder) =>
        {
            builder.OpenComponent<EventComponent>(0);
            builder.AddComponentParameter(1, nameof(EventComponent.OnClickAsyncAction), (Func<Task>)(async () =>
            {
                await tcs.Task;
                throw new InvalidTimeZoneException();
            }));
            builder.CloseComponent();
        }
        };

        var parentComponentId = renderer.AssignRootComponentId(parentComponent);
        await parentComponent.TriggerRenderAsync();

        var eventHandlerId = renderer.Batches[0]
            .ReferenceFrames
            .First(frame => frame.AttributeName == "onclickaction")
            .AttributeEventHandlerId;

        // Act
        var task = renderer.DispatchEventAsync(eventHandlerId, new DerivedEventArgs());

        // Assert
        Assert.Equal(TaskStatus.WaitingForActivation, task.Status);
        tcs.SetResult();

        await Assert.ThrowsAsync<InvalidTimeZoneException>(() => task);
    }

    [Fact]
    public async Task DispatchEventAsync_EventCallback_AsynchronousException()
    {
        // Arrange
        var tcs = new TaskCompletionSource();

        var renderer = new TestRenderer();
        var parentComponent = new OuterEventComponent();
        parentComponent.RenderFragment = (builder) =>
        {
            builder.OpenComponent<EventComponent>(0);
            builder.AddComponentParameter(1, nameof(EventComponent.OnClickEventCallback), EventCallback.Factory.Create(parentComponent, async () =>
            {
                await tcs.Task;
                throw new InvalidTimeZoneException();
            }));
            builder.CloseComponent();
        };

        var parentComponentId = renderer.AssignRootComponentId(parentComponent);
        await parentComponent.TriggerRenderAsync();

        var eventHandlerId = renderer.Batches[0]
            .ReferenceFrames
            .First(frame => frame.AttributeName == "onclick")
            .AttributeEventHandlerId;

        // Act
        var task = renderer.DispatchEventAsync(eventHandlerId, new DerivedEventArgs());

        // Assert
        Assert.Equal(TaskStatus.WaitingForActivation, task.Status);
        tcs.SetResult();

        await Assert.ThrowsAsync<InvalidTimeZoneException>(() => task);
    }

    [Fact]
    public async Task DispatchEventAsync_EventCallbackOfT_AsynchronousException()
    {
        // Arrange
        var tcs = new TaskCompletionSource();

        DerivedEventArgs arg = null;

        var renderer = new TestRenderer();
        var parentComponent = new OuterEventComponent();
        parentComponent.RenderFragment = (builder) =>
        {
            builder.OpenComponent<EventComponent>(0);
            builder.AddComponentParameter(1, nameof(EventComponent.OnClickEventCallbackOfT), EventCallback.Factory.Create<DerivedEventArgs>(parentComponent, async (e) =>
            {
                arg = e;
                await tcs.Task;
                throw new InvalidTimeZoneException();
            }));
            builder.CloseComponent();
        };

        var parentComponentId = renderer.AssignRootComponentId(parentComponent);
        await parentComponent.TriggerRenderAsync();

        var eventHandlerId = renderer.Batches[0]
            .ReferenceFrames
            .First(frame => frame.AttributeName == "onclick")
            .AttributeEventHandlerId;

        // Act
        var task = renderer.DispatchEventAsync(eventHandlerId, new DerivedEventArgs());

        // Assert
        Assert.NotNull(arg);
        Assert.Equal(TaskStatus.WaitingForActivation, task.Status);
        tcs.SetResult();

        await Assert.ThrowsAsync<InvalidTimeZoneException>(() => task);
    }

    [Fact]
    public async Task CannotDispatchEventsWithUnknownEventHandlers()
    {
        // Arrange
        var renderer = new TestRenderer();

        // Act/Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            return renderer.DispatchEventAsync(0, new EventArgs());
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
            builder.AddComponentParameter(2, nameof(FakeComponent.IntProperty), firstRender ? 123 : 256);
            builder.AddComponentParameter(3, nameof(FakeComponent.ObjectProperty), objectThatWillNotChange);
            builder.AddComponentParameter(4, nameof(FakeComponent.StringProperty), firstRender ? "String that will change" : "String that did change");
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
            builder.AddComponentParameter(2, nameof(MessageComponent.Message), firstRender ? "first" : "second");
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
    public void ReRendersChildComponentWhenUnmatchedValuesChange()
    {
        // Arrange: First render
        var renderer = new TestRenderer();
        var firstRender = true;
        var component = new TestComponent(builder =>
        {
            builder.OpenComponent<MyStrongComponent>(1);
            builder.AddComponentParameter(1, "class", firstRender ? "first" : "second");
            builder.AddComponentParameter(2, "id", "some_text");
            builder.AddComponentParameter(3, nameof(MyStrongComponent.Text), "hi there.");
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
                Assert.Equal(RenderTreeEditType.SetAttribute, edit.Type);
                Assert.Equal(0, edit.ReferenceFrameIndex);
            });
        AssertFrame.Attribute(renderer.Batches[1].ReferenceFrames[0], "class", "second");
    }

    // This is a sanity check that diffs of "unmatched" values *just work* without any specialized
    // code in the renderer to handle it. All of the data that's used in the diff is contained in
    // the render tree, and the diff process does not need to inspect the state of the component.
    [Fact]
    public void ReRendersDoesNotReRenderChildComponentWhenUnmatchedValuesDoNotChange()
    {
        // Arrange: First render
        var renderer = new TestRenderer();
        var component = new TestComponent(builder =>
        {
            builder.OpenComponent<MyStrongComponent>(1);
            builder.AddComponentParameter(1, "class", "cool-beans");
            builder.AddComponentParameter(2, "id", "some_text");
            builder.AddComponentParameter(3, nameof(MyStrongComponent.Text), "hi there.");
            builder.CloseComponent();
        });

        var rootComponentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();

        var childComponentId = renderer.Batches.Single()
            .ReferenceFrames
            .Single(frame => frame.FrameType == RenderTreeFrameType.Component)
            .ComponentId;

        // Act: Second render
        component.TriggerRender();

        // Assert
        Assert.False(renderer.Batches[1].DiffsByComponentId.ContainsKey(childComponentId));
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
                builder.AddComponentParameter(101, nameof(ConditionalParentComponent<FakeComponent>.IncludeChild), true);
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
        var renderHandle = ((FakeComponent)childComponent3).RenderHandle;
        renderHandle.Dispatcher.InvokeAsync(() => renderHandle.Render(builder
            => throw new NotImplementedException("Should not be invoked")));
        Assert.Equal(2, renderer.Batches.Count);
    }

    [Fact]
    public void RenderBatch_HandlesExceptionsFromAllDisposedComponents()
    {
        // Arrange
        var renderer = new TestRenderer { ShouldHandleExceptions = true };
        var exception1 = new Exception();
        var exception2 = new Exception();

        var firstRender = true;
        var component = new TestComponent(builder =>
        {
            if (firstRender)
            {
                builder.AddContent(0, "Hello");
                builder.OpenComponent<DisposableComponent>(1);
                builder.AddComponentParameter(1, nameof(DisposableComponent.DisposeAction), (Action)(() => throw exception1));
                builder.CloseComponent();

                builder.OpenComponent<DisposableComponent>(2);
                builder.AddComponentParameter(1, nameof(DisposableComponent.DisposeAction), (Action)(() => throw exception2));
                builder.CloseComponent();
            }
        });
        var componentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();

        // Act: Second render
        firstRender = false;
        component.TriggerRender();

        // Assert: Applicable children are included in disposal list
        Assert.Equal(2, renderer.Batches.Count);
        Assert.Equal(new[] { 1, 2 }, renderer.Batches[1].DisposedComponentIDs);

        // Outer component is still alive and not disposed.
        Assert.False(component.Disposed);
        var aex = Assert.IsType<AggregateException>(Assert.Single(renderer.HandledExceptions));
        Assert.Contains(exception1, aex.InnerExceptions);
        Assert.Contains(exception2, aex.InnerExceptions);
    }

    [Fact]
    public void RenderBatch_HandlesSynchronousExceptionsInAsyncDisposableComponents()
    {
        // Arrange
        var renderer = new TestRenderer { ShouldHandleExceptions = true };
        var exception1 = new InvalidOperationException();

        var firstRender = true;
        var component = new TestComponent(builder =>
        {
            if (firstRender)
            {
                builder.AddContent(0, "Hello");
                builder.OpenComponent<AsyncDisposableComponent>(1);
                builder.AddComponentParameter(1, nameof(AsyncDisposableComponent.AsyncDisposeAction), (Func<ValueTask>)(() => throw exception1));
                builder.CloseComponent();
            }
        });
        var componentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();

        // Act: Second render
        firstRender = false;
        component.TriggerRender();

        // Assert: Applicable children are included in disposal list
        Assert.Equal(2, renderer.Batches.Count);
        Assert.Equal(new[] { 1, }, renderer.Batches[1].DisposedComponentIDs);

        // Outer component is still alive and not disposed.
        Assert.False(component.Disposed);
        var aex = Assert.Single(renderer.HandledExceptions);
        Assert.Same(exception1, aex);
    }

    [Fact]
    public void RenderBatch_CanDisposeSynchronousAsyncDisposableImplementations()
    {
        // Arrange
        var renderer = new TestRenderer { ShouldHandleExceptions = true };

        var firstRender = true;
        var component = new TestComponent(builder =>
        {
            if (firstRender)
            {
                builder.AddContent(0, "Hello");
                builder.OpenComponent<AsyncDisposableComponent>(1);
                builder.AddComponentParameter(1, nameof(AsyncDisposableComponent.AsyncDisposeAction), (Func<ValueTask>)(() => default));
                builder.CloseComponent();
            }
        });
        var componentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();

        // Act: Second render
        firstRender = false;
        component.TriggerRender();

        // Assert: Applicable children are included in disposal list
        Assert.Equal(2, renderer.Batches.Count);
        Assert.Equal(new[] { 1, }, renderer.Batches[1].DisposedComponentIDs);

        // Outer component is still alive and not disposed.
        Assert.False(component.Disposed);
        Assert.Empty(renderer.HandledExceptions);
    }

    [Fact]
    public void RenderBatch_CanDisposeAsynchronousAsyncDisposables()
    {
        // Arrange
        var semaphore = new Semaphore(0, 1);
        var renderer = new TestRenderer
        {
            ShouldHandleExceptions = true,
            OnExceptionHandled = () => semaphore.Release()
        };
        var exception1 = new InvalidOperationException();
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var firstRender = true;
        var component = new TestComponent(builder =>
        {
            if (firstRender)
            {
                builder.AddContent(0, "Hello");
                builder.OpenComponent<AsyncDisposableComponent>(1);
                builder.AddComponentParameter(1, nameof(AsyncDisposableComponent.AsyncDisposeAction), (Func<ValueTask>)(async () => { await tcs.Task; }));
                builder.CloseComponent();
            }
        });

        var componentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();
        // Act: Second render
        firstRender = false;
        component.TriggerRender();

        // Assert: Applicable children are included in disposal list
        Assert.Equal(2, renderer.Batches.Count);
        Assert.Equal(new[] { 1, }, renderer.Batches[1].DisposedComponentIDs);

        // Outer component is still alive and not disposed.
        Assert.False(component.Disposed);
        Assert.Empty(renderer.HandledExceptions);

        // Continue execution
        tcs.SetResult();
        Assert.False(semaphore.WaitOne(10));
        Assert.Empty(renderer.HandledExceptions);
    }

    [Fact]
    public void RenderBatch_HandlesAsynchronousExceptionsInAsyncDisposableComponents()
    {
        // Arrange
        var semaphore = new Semaphore(0, 1);
        var renderer = new TestRenderer
        {
            ShouldHandleExceptions = true,
            OnExceptionHandled = () => semaphore.Release()
        };
        var exception1 = new InvalidOperationException();
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var firstRender = true;
        var component = new TestComponent(builder =>
        {
            if (firstRender)
            {
                builder.AddContent(0, "Hello");
                builder.OpenComponent<AsyncDisposableComponent>(1);
                builder.AddComponentParameter(1, nameof(AsyncDisposableComponent.AsyncDisposeAction), (Func<ValueTask>)(async () => { await tcs.Task; throw exception1; }));
                builder.CloseComponent();
            }
        });
        var componentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();
        // Act: Second render
        firstRender = false;
        component.TriggerRender();

        // Assert: Applicable children are included in disposal list
        Assert.Equal(2, renderer.Batches.Count);
        Assert.Equal(new[] { 1, }, renderer.Batches[1].DisposedComponentIDs);

        // Outer component is still alive and not disposed.
        Assert.False(component.Disposed);
        Assert.Empty(renderer.HandledExceptions);

        // Continue execution
        tcs.SetResult();
        semaphore.WaitOne();
        var aex = Assert.IsType<InvalidOperationException>(Assert.Single(renderer.HandledExceptions));
        Assert.Same(exception1, aex);
    }

    [Fact]
    public void RenderBatch_ReportsSynchronousCancelationsAsErrors()
    {
        // Arrange
        var renderer = new TestRenderer { ShouldHandleExceptions = true };
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var firstRender = true;
        var component = new TestComponent(builder =>
        {
            if (firstRender)
            {
                builder.AddContent(0, "Hello");
                builder.OpenComponent<AsyncDisposableComponent>(1);
                builder.AddComponentParameter(1, nameof(AsyncDisposableComponent.AsyncDisposeAction), (Func<ValueTask>)(() => throw new TaskCanceledException()));
                builder.CloseComponent();
            }
        });
        var componentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();

        // Act: Second render
        firstRender = false;
        component.TriggerRender();

        // Assert: Applicable children are included in disposal list
        Assert.Equal(2, renderer.Batches.Count);
        Assert.Equal(new[] { 1, }, renderer.Batches[1].DisposedComponentIDs);

        // Outer component is still alive and not disposed.
        Assert.False(component.Disposed);
        Assert.IsType<TaskCanceledException>(Assert.Single(renderer.HandledExceptions));
    }

    [Fact]
    public void RenderBatch_ReportsAsynchronousCancelationsAsErrors()
    {
        // Arrange
        var semaphore = new Semaphore(0, 1);
        var renderer = new TestRenderer { ShouldHandleExceptions = true };
        renderer.OnExceptionHandled += () => semaphore.Release();
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstRender = true;
        var component = new TestComponent(builder =>
        {
            if (firstRender)
            {
                builder.AddContent(0, "Hello");
                builder.OpenComponent<AsyncDisposableComponent>(1);
                builder.AddComponentParameter(
                    1,
                    nameof(AsyncDisposableComponent.AsyncDisposeAction),
                    (Func<ValueTask>)(() => new ValueTask(tcs.Task)));
                builder.CloseComponent();
            }
        });
        var componentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();

        // Act: Second render
        firstRender = false;
        component.TriggerRender();

        // Assert: Applicable children are included in disposal list
        Assert.Equal(2, renderer.Batches.Count);
        Assert.Equal(new[] { 1, }, renderer.Batches[1].DisposedComponentIDs);

        // Outer component is still alive and not disposed.
        Assert.False(component.Disposed);
        Assert.Empty(renderer.HandledExceptions);

        // Cancel execution
        tcs.SetCanceled();

        semaphore.WaitOne();
        var aex = Assert.IsType<TaskCanceledException>(Assert.Single(renderer.HandledExceptions));
    }

    [Fact]
    public void RenderBatch_DoesNotDisposeComponentMultipleTimes()
    {
        // Arrange
        var renderer = new TestRenderer { ShouldHandleExceptions = true };
        var exception1 = new Exception();
        var exception2 = new Exception();

        var count1 = 0;
        var count2 = 0;
        var count3 = 0;
        var count4 = 0;
        var count5 = 0;

        var firstRender = true;
        var component = new TestComponent(builder =>
        {
            if (firstRender)
            {
                builder.AddContent(0, "Hello");
                builder.OpenComponent<DisposableComponent>(1);
                builder.AddComponentParameter(1, nameof(DisposableComponent.DisposeAction), (Action)(() => { count1++; }));
                builder.CloseComponent();

                builder.OpenComponent<DisposableComponent>(2);
                builder.AddComponentParameter(1, nameof(DisposableComponent.DisposeAction), (Action)(() => { count2++; throw exception1; }));
                builder.CloseComponent();

                builder.OpenComponent<DisposableComponent>(3);
                builder.AddComponentParameter(1, nameof(DisposableComponent.DisposeAction), (Action)(() => { count3++; }));
                builder.CloseComponent();
            }

            builder.OpenComponent<DisposableComponent>(4);
            builder.AddComponentParameter(1, nameof(DisposableComponent.DisposeAction), (Action)(() => { count4++; throw exception2; }));
            builder.CloseComponent();

            builder.OpenComponent<DisposableComponent>(5);
            builder.AddComponentParameter(1, nameof(DisposableComponent.DisposeAction), (Action)(() => { count5++; }));
            builder.CloseComponent();
        });
        var componentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();

        // Act: Second render
        firstRender = false;
        component.TriggerRender();

        // Assert: Applicable children are included in disposal list
        Assert.Equal(2, renderer.Batches.Count);
        Assert.Equal(new[] { 1, 2, 3 }, renderer.Batches[1].DisposedComponentIDs);

        // Components "disposed" in the batch were all disposed, components that are still live were not disposed
        Assert.Equal(1, count1);
        Assert.Equal(1, count2);
        Assert.Equal(1, count3);
        Assert.Equal(0, count4);
        Assert.Equal(0, count5);

        // Outer component is still alive and not disposed.
        Assert.False(component.Disposed);
        var ex = Assert.IsType<Exception>(Assert.Single(renderer.HandledExceptions));
        Assert.Same(exception1, ex);

        // Act: Dispose renderer
        renderer.Dispose();

        Assert.Equal(2, renderer.HandledExceptions.Count);
        ex = renderer.HandledExceptions[1];
        Assert.Same(exception2, ex);

        // Assert: Everything was disposed once.
        Assert.Equal(1, count1);
        Assert.Equal(1, count2);
        Assert.Equal(1, count3);
        Assert.Equal(1, count4);
        Assert.Equal(1, count5);
        Assert.True(component.Disposed);
    }

    [Fact]
    public async Task DoesNotDispatchEventsAfterOwnerComponentIsDisposed()
    {
        // Arrange
        var renderer = new TestRenderer();
        var eventCount = 0;
        Action<EventArgs> origEventHandler = args => { eventCount++; };
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
        var renderTask = renderer.DispatchEventAsync(eventHandlerId, args: null);
        Assert.True(renderTask.IsCompletedSuccessfully);
        Assert.Equal(1, eventCount);
        await renderTask;

        // Now remove the EventComponent, but without ever acknowledging the renderbatch, so the event handler doesn't get disposed
        var disposalBatchAcknowledgementTcs = new TaskCompletionSource();
        component.IncludeChild = false;
        renderer.NextRenderResultTask = disposalBatchAcknowledgementTcs.Task;
        component.TriggerRender();

        // Act/Assert 2: Can no longer fire the original event. It's not an error but the delegate was not invoked.
        await renderer.DispatchEventAsync(eventHandlerId, args: null);
        Assert.Equal(1, eventCount);
    }

    [Fact]
    public async Task DisposesEventHandlersWhenAttributeValueChanged()
    {
        // Arrange
        var renderer = new TestRenderer();
        var eventCount = 0;
        Action<EventArgs> origEventHandler = args => { eventCount++; };
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
        var renderTask = renderer.DispatchEventAsync(origEventHandlerId, args: null);
        Assert.True(renderTask.IsCompletedSuccessfully);
        Assert.Equal(1, eventCount);
        await renderTask;

        // Now change the attribute value
        var newEventCount = 0;
        component.OnTest = args => { newEventCount++; };
        component.TriggerRender();

        // Act/Assert 2: Can no longer fire the original event, but can fire the new event
        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            return renderer.DispatchEventAsync(origEventHandlerId, args: null);
        });

        Assert.Equal(1, eventCount);
        Assert.Equal(0, newEventCount);
        renderTask = renderer.DispatchEventAsync(origEventHandlerId + 1, args: null);
        Assert.True(renderTask.IsCompletedSuccessfully);
        Assert.Equal(1, newEventCount);
        await renderTask;
    }

    [Fact]
    public async Task DisposesEventHandlersWhenAttributeRemoved()
    {
        // Arrange
        var renderer = new TestRenderer();
        var eventCount = 0;
        Action<EventArgs> origEventHandler = args => { eventCount++; };
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
        var renderTask = renderer.DispatchEventAsync(origEventHandlerId, args: null);
        Assert.True(renderTask.IsCompletedSuccessfully);
        Assert.Equal(1, eventCount);
        await renderTask;

        // Now remove the event attribute
        component.OnTest = null;
        component.TriggerRender();

        // Act/Assert 2: Can no longer fire the original event
        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            return renderer.DispatchEventAsync(origEventHandlerId, args: null);
        });
        Assert.Equal(1, eventCount);
    }

    [Fact]
    public async Task DisposesEventHandlersWhenOwnerComponentRemoved()
    {
        // Arrange
        var renderer = new TestRenderer();
        var eventCount = 0;
        Action<EventArgs> origEventHandler = args => { eventCount++; };
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
        var renderTask = renderer.DispatchEventAsync(eventHandlerId, args: null);
        Assert.True(renderTask.IsCompletedSuccessfully);
        Assert.Equal(1, eventCount);
        await renderTask;

        // Now remove the EventComponent
        component.IncludeChild = false;
        component.TriggerRender();

        // Act/Assert 2: Can no longer fire the original event
        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            return renderer.DispatchEventAsync(eventHandlerId, args: null);
        });
        Assert.Equal(1, eventCount);
    }

    [Fact]
    public async Task DisposesEventHandlersWhenAncestorElementRemoved()
    {
        // Arrange
        var renderer = new TestRenderer();
        var eventCount = 0;
        Action<EventArgs> origEventHandler = args => { eventCount++; };
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
        var renderTask = renderer.DispatchEventAsync(origEventHandlerId, args: null);
        Assert.True(renderTask.IsCompletedSuccessfully);
        Assert.Equal(1, eventCount);
        await renderTask;

        // Now remove the ancestor element
        component.SkipElement = true;
        component.TriggerRender();

        // Act/Assert 2: Can no longer fire the original event
        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            return renderer.DispatchEventAsync(origEventHandlerId, args: null);
        });
        Assert.Equal(1, eventCount);
    }

    [Fact]
    public async Task AllRendersTriggeredSynchronouslyDuringEventHandlerAreHandledAsSingleBatch()
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
            builder.AddComponentParameter(2, nameof(EventComponent.OnTest), new Action<EventArgs>(args =>
            {
                eventCount++;
                rootComponent.TriggerRender();
                childComponent.TriggerRender();
            }));
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
        var renderTask = renderer.DispatchEventAsync(origEventHandlerId, args: null);

        // Assert
        Assert.True(renderTask.IsCompletedSuccessfully);
        await renderTask;

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
        var ex = Assert.Throws<InvalidOperationException>(component.TriggerRender);
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
            builder.AddComponentParameter(1, nameof(ReRendersParentComponent.Parent), parent);
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
                builder.AddComponentParameter(2, "onclick", (Action<object>)((object obj) =>
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
            .Where(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "onmycustomevent")
            .Single(f => f.AttributeEventHandlerId != 0)
            .AttributeEventHandlerId;

        // Act
        // The fact that there's no error here is the main thing we're testing
        var renderTask = renderer.DispatchEventAsync(origEventHandlerId, args: null);

        // Assert: correct render result
        Assert.True(renderTask.IsCompletedSuccessfully);
        var newBatch = renderer.Batches.Skip(1).Single();
        Assert.Single(newBatch.DisposedComponentIDs);
        Assert.Single(newBatch.DiffsByComponentId);
        Assert.Collection(newBatch.DiffsByComponentId[componentId].Single().Edits,
            edit =>
            {
                Assert.Equal(RenderTreeEditType.RemoveFrame, edit.Type);
                Assert.Equal(1, edit.SiblingIndex);
            });
    }

    [Fact]
    public async Task CanCombineBindAndConditionalAttribute()
    {
        // This test represents https://github.com/dotnet/blazor/issues/624

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
        var eventArgs = new ChangeEventArgs { Value = true };
        var renderTask = renderer.DispatchEventAsync(checkboxChangeEventHandlerId, eventArgs);

        Assert.True(renderTask.IsCompletedSuccessfully);
        var latestBatch = renderer.Batches.Last();
        var latestDiff = latestBatch.DiffsInOrder.Single();
        var referenceFrames = latestBatch.ReferenceFrames;

        // Assert: Textbox's "disabled" attribute was removed
        Assert.Equal(2, renderer.Batches.Count);
        Assert.Equal(componentId, latestDiff.ComponentId);
        Assert.Contains(latestDiff.Edits, edit =>
            edit.SiblingIndex == 1
            && edit.RemovedAttributeName == "disabled");

        await renderTask;
    }

    [Fact]
    public async Task BindWithSynchronousSetter_Lambda()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new OuterEventComponent();
        var value = "value";
        component.RenderFragment = (builder) =>
        {
            builder.OpenElement(0, "input");
            builder.AddAttribute(1, "onchange", EventCallback.Factory.CreateBinder(
                component,
                RuntimeHelpers.CreateInferredBindSetter(__value => value = __value, value),
                value));
            builder.CloseElement();
        };
        var componentId = renderer.AssignRootComponentId(component);
        await component.TriggerRenderAsync();
        var checkboxChangeEventHandlerId = renderer.Batches.Single()
            .ReferenceFrames
            .First(frame => frame.FrameType == RenderTreeFrameType.Attribute && frame.AttributeEventHandlerId != 0)
            .AttributeEventHandlerId;

        // Act: Trigger change event
        var eventArgs = new ChangeEventArgs { Value = "hello" };
        var renderTask = renderer.DispatchEventAsync(checkboxChangeEventHandlerId, eventArgs);
        await renderTask;

        // Assert
        Assert.Equal("hello", value);
    }

    [Fact]
    public async Task BindWithAsynchronousSetter_MethodGroupToDelegate()
    {
        // This test represents https://github.com/dotnet/blazor/issues/624

        // Arrange: Rendered with textbox enabled
        var renderer = new TestRenderer();
        var component = new OuterEventComponent();
        var value = "value";
        async Task SetValue(string __value)
        {
            value = __value;
            await Task.CompletedTask;
        }
        component.RenderFragment = (builder) =>
        {
            builder.OpenElement(0, "input");
            builder.AddAttribute(1, "onchange", EventCallback.Factory.CreateBinder(
            component,
            RuntimeHelpers.CreateInferredBindSetter(SetValue, value),
            value));
            builder.CloseElement();
        };
        var componentId = renderer.AssignRootComponentId(component);
        await component.TriggerRenderAsync();
        var checkboxChangeEventHandlerId = renderer.Batches.Single()
            .ReferenceFrames
            .First(frame => frame.FrameType == RenderTreeFrameType.Attribute && frame.AttributeEventHandlerId != 0)
            .AttributeEventHandlerId;

        // Act: Trigger change event
        var eventArgs = new ChangeEventArgs { Value = "hello" };
        var renderTask = renderer.DispatchEventAsync(checkboxChangeEventHandlerId, eventArgs);
        await renderTask;

        // Assert
        Assert.Equal("hello", value);
    }

    [Fact]
    public async Task BindWithAfter()
    {
        // This test represents https://github.com/dotnet/blazor/issues/624

        // Arrange: Rendered with textbox enabled
        var renderer = new TestRenderer();
        var component = new OuterEventComponent();
        string value = "value";
        component.RenderFragment = (builder) =>
        {
            builder.OpenElement(0, "input");
            builder.AddAttribute(1, "onchange", EventCallback.Factory.CreateBinder(
            component,
            RuntimeHelpers.CreateInferredBindSetter(
                __value =>
                {
                    value = __value;
                    return RuntimeHelpers.InvokeAsynchronousDelegate(() => Task.CompletedTask);
                },
                value),
            value));
            builder.CloseElement();
        };
        var componentId = renderer.AssignRootComponentId(component);
        await component.TriggerRenderAsync();
        var checkboxChangeEventHandlerId = renderer.Batches.Single()
            .ReferenceFrames
            .First(frame => frame.FrameType == RenderTreeFrameType.Attribute && frame.AttributeEventHandlerId != 0)
            .AttributeEventHandlerId;

        // Act: Trigger change event
        var eventArgs = new ChangeEventArgs { Value = "hello" };
        var renderTask = renderer.DispatchEventAsync(checkboxChangeEventHandlerId, eventArgs);
        await renderTask;

        // Assert
        Assert.Equal("hello", value);
    }

    [Fact]
    public async Task BindWithAfter_Action()
    {
        // This test represents https://github.com/dotnet/blazor/issues/624

        // Arrange: Rendered with textbox enabled
        var renderer = new TestRenderer();
        var component = new OuterEventComponent();
        string value = "value";
        component.RenderFragment = (builder) =>
        {
            builder.OpenElement(0, "input");
            builder.AddAttribute(1, "onchange", EventCallback.Factory.CreateBinder(
            component,
            RuntimeHelpers.CreateInferredBindSetter(
                __value =>
                {
                    value = __value;
                    return RuntimeHelpers.InvokeAsynchronousDelegate(() => { });
                },
                value),
            value));
            builder.CloseElement();
        };
        var componentId = renderer.AssignRootComponentId(component);
        await component.TriggerRenderAsync();
        var checkboxChangeEventHandlerId = renderer.Batches.Single()
            .ReferenceFrames
            .First(frame => frame.FrameType == RenderTreeFrameType.Attribute && frame.AttributeEventHandlerId != 0)
            .AttributeEventHandlerId;

        // Act: Trigger change event
        var eventArgs = new ChangeEventArgs { Value = "hello" };
        var renderTask = renderer.DispatchEventAsync(checkboxChangeEventHandlerId, eventArgs);
        await renderTask;

        // Assert
        Assert.Equal("hello", value);
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
    public void CallsAfterRenderAfterTheUIHasFinishedUpdatingAsynchronously()
    {
        // Arrange
        var @event = new ManualResetEventSlim();
        var tcs = new TaskCompletionSource();
        var afterRenderTcs = new TaskCompletionSource();
        var onAfterRenderCallCountLog = new List<int>();
        var component = new AsyncAfterRenderComponent(afterRenderTcs.Task)
        {
            OnAfterRenderComplete = @event.Set,
        };
        var renderer = new AsyncUpdateTestRenderer()
        {
            OnUpdateDisplayAsync = _ => tcs.Task,
        };
        renderer.AssignRootComponentId(component);

        // Act
        component.TriggerRender();
        tcs.SetResult();
        afterRenderTcs.SetResult();

        // We need to wait here because the completions from SetResult will be scheduled.
        @event.Wait(Timeout);

        // Assert
        Assert.True(component.Called);
    }

    [Fact]
    public void CallsAfterRenderAfterTheUIHasFinishedUpdatingSynchronously()
    {
        // Arrange
        var @event = new ManualResetEventSlim();
        var afterRenderTcs = new TaskCompletionSource();
        var onAfterRenderCallCountLog = new List<int>();
        var component = new AsyncAfterRenderComponent(afterRenderTcs.Task)
        {
            OnAfterRenderComplete = @event.Set,
        };
        var renderer = new AsyncUpdateTestRenderer()
        {
            OnUpdateDisplayAsync = _ => Task.CompletedTask
        };
        renderer.AssignRootComponentId(component);

        // Act
        component.TriggerRender();
        afterRenderTcs.SetResult();

        // We need to wait here because the completions from SetResult will be scheduled.
        @event.Wait(Timeout);

        // Assert
        Assert.True(component.Called);
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
            builder.AddComponentParameter(1, "some param", showComponent3);
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
        Assert.Single(batch2.DisposedComponentIDs); // Third child
        Assert.Equal(2, childComponents[0].OnAfterRenderCallCount); // Retained and re-rendered
        Assert.Equal(1, childComponents[1].OnAfterRenderCallCount); // Retained and not re-rendered
        Assert.Equal(1, childComponents[2].OnAfterRenderCallCount); // Disposed
    }

    [Fact]
    public void CanTriggerRenderingSynchronouslyFromInsideAfterRenderCallback()
    {
        // Arrange
        AfterRenderCaptureComponent component = null;
        component = new AfterRenderCaptureComponent
        {
            OnAfterRenderLogic = () =>
            {
                if (component.OnAfterRenderCallCount < 10)
                {
                    component.TriggerRender();
                }
            }
        };
        var renderer = new TestRenderer();
        renderer.AssignRootComponentId(component);

        // Act
        component.TriggerRender();

        // Assert
        Assert.Equal(10, component.OnAfterRenderCallCount);
    }

    [Fact]
    public async Task CanTriggerEventHandlerDisposedInEarlierPendingBatchAsync()
    {
        // This represents the scenario where the same event handler is being triggered
        // rapidly, such as an input event while typing. It only applies to asynchronous
        // batch updates, i.e., server-side Components.
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
        Action<EventArgs> eventHandler = null;

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
        var render1TCS = new TaskCompletionSource();
        renderer.NextUpdateDisplayReturnTask = render1TCS.Task;
        await renderer.DispatchEventAsync(eventHandlerId, new EventArgs());
        Assert.Equal(1, numEventsFired);

        // Act/Assert 2: *Same* event handler ID can be reused prior to completion of
        // preceding UI update
        var render2TCS = new TaskCompletionSource();
        renderer.NextUpdateDisplayReturnTask = render2TCS.Task;
        await renderer.DispatchEventAsync(eventHandlerId, new EventArgs());
        Assert.Equal(2, numEventsFired);

        // Act/Assert 3: After we complete the first UI update in which a given
        // event handler ID is disposed, we can no longer reuse that event handler ID

        // From here we can't see when the async disposal is completed. Just give it plenty of time (Task.Yield isn't enough).
        // There is a small chance in which the continuations from TaskCompletionSource run asynchronously.
        // In that case we might not be able to see the results from RemoveEventHandlerIds as they might run asynchronously.
        // For that case, we are going to queue a continuation on render1TCS.Task, include a 1s delay and await the resulting
        // task to offer the best chance that we get to see the error in all cases.
        var awaitableTask = render1TCS.Task.ContinueWith(_ => Task.Delay(1000)).Unwrap();
        render1TCS.SetResult();
        await awaitableTask;
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            return renderer.DispatchEventAsync(eventHandlerId, new EventArgs());
        });
        Assert.Contains($"There is no event handler associated with this event. EventId: '{eventHandlerId}'.", ex.Message);
        Assert.Equal(2, numEventsFired);
    }

    [Fact]
    public void ExceptionsThrownSynchronouslyCanBeHandledSynchronously()
    {
        // Arrange
        var renderer = new TestRenderer { ShouldHandleExceptions = true };
        var component = new NestedAsyncComponent();
        var exception = new InvalidTimeZoneException();

        // Act/Assert
        var componentId = renderer.AssignRootComponentId(component);
        var task = renderer.RenderRootComponentAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(NestedAsyncComponent.EventActions)] = new Dictionary<int, IList<NestedAsyncComponent.ExecutionAction>>
            {
                [0] = new[]
                {
                        new NestedAsyncComponent.ExecutionAction
                        {
                            Event = NestedAsyncComponent.EventType.OnInitAsyncAsync,
                            EventAction = () => throw exception,
                        },
                    }
            },
            [nameof(NestedAsyncComponent.WhatToRender)] = new Dictionary<int, Func<NestedAsyncComponent, RenderFragment>>
            {
                [0] = CreateRenderFactory(Array.Empty<int>()),
            },
        }));

        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(new[] { exception }, renderer.HandledExceptions);
    }

    [Fact]
    public void ExceptionsThrownSynchronouslyCanBeHandled()
    {
        // Arrange
        var renderer = new TestRenderer { ShouldHandleExceptions = true };
        var component = new NestedAsyncComponent();
        var exception = new InvalidTimeZoneException();

        // Act/Assert
        var componentId = renderer.AssignRootComponentId(component);
        var renderTask = renderer.RenderRootComponentAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(NestedAsyncComponent.EventActions)] = new Dictionary<int, IList<NestedAsyncComponent.ExecutionAction>>
            {
                [0] = new[]
                {
                        new NestedAsyncComponent.ExecutionAction
                        {
                            Event = NestedAsyncComponent.EventType.OnInitAsyncAsync,
                            EventAction = () => throw exception,
                        },
                    }
            },
            [nameof(NestedAsyncComponent.WhatToRender)] = new Dictionary<int, Func<NestedAsyncComponent, RenderFragment>>
            {
                [0] = CreateRenderFactory(Array.Empty<int>()),
            },
        }));

        Assert.True(renderTask.IsCompletedSuccessfully);
        Assert.Equal(new[] { exception }, renderer.HandledExceptions);
    }

    [Fact]
    public void ExceptionsReturnedUsingTaskFromExceptionCanBeHandled()
    {
        // Arrange
        var renderer = new TestRenderer { ShouldHandleExceptions = true };
        var component = new NestedAsyncComponent();
        var exception = new InvalidTimeZoneException();

        // Act/Assert
        var componentId = renderer.AssignRootComponentId(component);
        var renderTask = renderer.RenderRootComponentAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(NestedAsyncComponent.EventActions)] = new Dictionary<int, IList<NestedAsyncComponent.ExecutionAction>>
            {
                [0] = new[]
                {
                        new NestedAsyncComponent.ExecutionAction
                        {
                            Event = NestedAsyncComponent.EventType.OnInitAsyncAsync,
                            EventAction = () => Task.FromException<(int, NestedAsyncComponent.EventType)>(exception),
                        },
                    }
            },
            [nameof(NestedAsyncComponent.WhatToRender)] = new Dictionary<int, Func<NestedAsyncComponent, RenderFragment>>
            {
                [0] = CreateRenderFactory(Array.Empty<int>()),
            },
        }));

        Assert.True(renderTask.IsCompletedSuccessfully);
        Assert.Equal(new[] { exception }, renderer.HandledExceptions);
    }

    [Fact]
    public async Task ExceptionsThrownAsynchronouslyDuringFirstRenderCanBeHandled()
    {
        // Arrange
        var renderer = new TestRenderer { ShouldHandleExceptions = true };
        var component = new NestedAsyncComponent();
        var tcs = new TaskCompletionSource();
        var exception = new InvalidTimeZoneException();

        // Act/Assert
        var componentId = renderer.AssignRootComponentId(component);
        var renderTask = renderer.RenderRootComponentAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(NestedAsyncComponent.EventActions)] = new Dictionary<int, IList<NestedAsyncComponent.ExecutionAction>>
            {
                [0] = new[]
                {
                        new NestedAsyncComponent.ExecutionAction
                        {
                            Event = NestedAsyncComponent.EventType.OnInitAsyncAsync,
                            EventAction = async () =>
                            {
                                await tcs.Task;
                                throw exception;
                            }
                        },
                    }
            },
            [nameof(NestedAsyncComponent.WhatToRender)] = new Dictionary<int, Func<NestedAsyncComponent, RenderFragment>>
            {
                [0] = CreateRenderFactory(Array.Empty<int>()),
            },
        }));

        Assert.False(renderTask.IsCompleted);
        tcs.SetResult();
        await renderTask;
        Assert.Same(exception, Assert.Single(renderer.HandledExceptions).GetBaseException());
    }

    [Fact]
    public async Task ExceptionsDispatchedOffSyncContextCanBeHandledAsync()
    {
        // Arrange
        var renderer = new TestRenderer { ShouldHandleExceptions = true };
        var component = new NestedAsyncComponent();
        var exception = new InvalidTimeZoneException("Error from outside the sync context.");

        // Act
        renderer.AssignRootComponentId(component);
        await component.ExternalExceptionDispatch(exception);

        // Assert
        Assert.Same(exception, Assert.Single(renderer.HandledExceptions).GetBaseException());
    }

    [Fact]
    public async Task ExceptionsThrownAsynchronouslyAfterFirstRenderCanBeHandled()
    {
        // This differs from the "during first render" case, because some aspects of the rendering
        // code paths are special cased for the first render because of prerendering.

        // Arrange
        var @event = new ManualResetEventSlim();
        var renderer = new TestRenderer()
        {
            ShouldHandleExceptions = true,
            OnExceptionHandled = @event.Set,
        };
        var taskToAwait = Task.CompletedTask;
        var component = new TestComponent(builder =>
        {
            builder.OpenComponent<ComponentThatAwaitsTask>(0);
            builder.AddComponentParameter(1, nameof(ComponentThatAwaitsTask.TaskToAwait), taskToAwait);
            builder.CloseComponent();
        });
        var componentId = renderer.AssignRootComponentId(component);
        await renderer.RenderRootComponentAsync(componentId); // Not throwing on first render

        var asyncExceptionTcs = new TaskCompletionSource();
        taskToAwait = asyncExceptionTcs.Task;
        await renderer.Dispatcher.InvokeAsync(component.TriggerRender);

        // Act
        var exception = new InvalidOperationException();

        @event.Reset();
        asyncExceptionTcs.SetException(exception);

        // We need to wait here because the continuations of SetException will be scheduled to run asynchronously.
        @event.Wait(Timeout);

        // Assert
        Assert.Same(exception, Assert.Single(renderer.HandledExceptions).GetBaseException());
    }

    [Fact]
    public async Task ExceptionsThrownAsynchronouslyFromMultipleComponentsCanBeHandled()
    {
        // Arrange
        var renderer = new TestRenderer { ShouldHandleExceptions = true };
        var component = new NestedAsyncComponent();
        var exception1 = new InvalidTimeZoneException();
        var exception2 = new UriFormatException();
        var tcs = new TaskCompletionSource();

        // Act/Assert
        var componentId = renderer.AssignRootComponentId(component);
        var renderTask = renderer.RenderRootComponentAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(NestedAsyncComponent.EventActions)] = new Dictionary<int, IList<NestedAsyncComponent.ExecutionAction>>
            {
                [0] = Array.Empty<NestedAsyncComponent.ExecutionAction>(),
                [1] = new List<NestedAsyncComponent.ExecutionAction>
                    {
                        new NestedAsyncComponent.ExecutionAction
                        {
                            Event = NestedAsyncComponent.EventType.OnInitAsyncAsync,
                            EventAction = async () =>
                            {
                                await tcs.Task;
                                throw exception1;
                            }
                        },
                    },
                [2] = new List<NestedAsyncComponent.ExecutionAction>
                    {
                        new NestedAsyncComponent.ExecutionAction
                        {
                            Event = NestedAsyncComponent.EventType.OnInitAsyncAsync,
                            EventAction = async () =>
                            {
                                await tcs.Task;
                                throw exception2;
                            }
                        },
                    },
            },
            [nameof(NestedAsyncComponent.WhatToRender)] = new Dictionary<int, Func<NestedAsyncComponent, RenderFragment>>
            {
                [0] = CreateRenderFactory(new[] { 1, 2, }),
                [1] = CreateRenderFactory(Array.Empty<int>()),
                [2] = CreateRenderFactory(Array.Empty<int>()),
            },
        }));

        Assert.False(renderTask.IsCompleted);
        tcs.SetResult();

        await renderTask;
        Assert.Equal(2, renderer.HandledExceptions.Count);
        Assert.Contains(exception1, renderer.HandledExceptions);
        Assert.Contains(exception2, renderer.HandledExceptions);
    }

    [Fact]
    public void ExceptionsThrownSynchronouslyFromMultipleComponentsCanBeHandled()
    {
        // Arrange
        var renderer = new TestRenderer { ShouldHandleExceptions = true };
        var component = new NestedAsyncComponent();
        var exception1 = new InvalidTimeZoneException();
        var exception2 = new UriFormatException();

        // Act/Assert
        var componentId = renderer.AssignRootComponentId(component);
        var renderTask = renderer.RenderRootComponentAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(NestedAsyncComponent.EventActions)] = new Dictionary<int, IList<NestedAsyncComponent.ExecutionAction>>
            {
                [0] = Array.Empty<NestedAsyncComponent.ExecutionAction>(),
                [1] = new List<NestedAsyncComponent.ExecutionAction>
                    {
                        new NestedAsyncComponent.ExecutionAction
                        {
                            Event = NestedAsyncComponent.EventType.OnInitAsyncAsync,
                            EventAction = () =>
                            {
                                throw exception1;
                            }
                        },
                    },
                [2] = new List<NestedAsyncComponent.ExecutionAction>
                    {
                        new NestedAsyncComponent.ExecutionAction
                        {
                            Event = NestedAsyncComponent.EventType.OnInitAsyncAsync,
                            EventAction = () =>
                            {
                                throw exception2;
                            }
                        },
                    },
            },
            [nameof(NestedAsyncComponent.WhatToRender)] = new Dictionary<int, Func<NestedAsyncComponent, RenderFragment>>
            {
                [0] = CreateRenderFactory(new[] { 1, 2, }),
                [1] = CreateRenderFactory(Array.Empty<int>()),
                [2] = CreateRenderFactory(Array.Empty<int>()),
            },
        }));

        Assert.True(renderTask.IsCompletedSuccessfully);

        Assert.Equal(2, renderer.HandledExceptions.Count);
        Assert.Contains(exception1, renderer.HandledExceptions);
        Assert.Contains(exception2, renderer.HandledExceptions);
    }

    [Fact]
    public async Task ExceptionsThrownFromHandleAfterRender_Sync_AreHandled()
    {
        // Arrange
        var renderer = new TestRenderer { ShouldHandleExceptions = true };
        var component = new NestedAsyncComponent();
        var exception = new InvalidTimeZoneException();

        var taskCompletionSource = new TaskCompletionSource();

        // Act/Assert
        var componentId = renderer.AssignRootComponentId(component);
        var renderTask = renderer.RenderRootComponentAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(NestedAsyncComponent.EventActions)] = new Dictionary<int, IList<NestedAsyncComponent.ExecutionAction>>
            {
                [0] = new[]
                {
                        new NestedAsyncComponent.ExecutionAction
                        {
                            Event = NestedAsyncComponent.EventType.OnAfterRenderAsyncSync,
                            EventAction = () =>
                            {
                                throw exception;
                            },
                        }
                    },
                [1] = new[]
                {
                        new NestedAsyncComponent.ExecutionAction
                        {
                            Event = NestedAsyncComponent.EventType.OnAfterRenderAsyncSync,
                            EventAction = () =>
                            {
                                taskCompletionSource.TrySetResult();
                                return Task.FromResult((1, NestedAsyncComponent.EventType.OnAfterRenderAsyncSync));
                            },
                        }
                    }
            },
            [nameof(NestedAsyncComponent.WhatToRender)] = new Dictionary<int, Func<NestedAsyncComponent, RenderFragment>>
            {
                [0] = CreateRenderFactory(new[] { 1 }),
                [1] = CreateRenderFactory(Array.Empty<int>()),
            },
        }));

        Assert.True(renderTask.IsCompletedSuccessfully);

        // OnAfterRenderAsync happens in the background. Make it more predictable, by gating it until we're ready to capture exceptions.
        await taskCompletionSource.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
        Assert.Same(exception, Assert.Single(renderer.HandledExceptions).GetBaseException());
    }

    [Fact]
    public async Task ExceptionsThrownFromHandleAfterRender_Async_AreHandled()
    {
        // Arrange
        var renderer = new TestRenderer { ShouldHandleExceptions = true };
        var component = new NestedAsyncComponent();
        var exception = new InvalidTimeZoneException();

        var taskCompletionSource = new TaskCompletionSource();

        // Act/Assert
        var componentId = renderer.AssignRootComponentId(component);
        var renderTask = renderer.RenderRootComponentAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(NestedAsyncComponent.EventActions)] = new Dictionary<int, IList<NestedAsyncComponent.ExecutionAction>>
            {
                [0] = new[]
                {
                        new NestedAsyncComponent.ExecutionAction
                        {
                            Event = NestedAsyncComponent.EventType.OnAfterRenderAsyncAsync,
                            EventAction = async () =>
                            {
                                await Task.Yield();
                                throw exception;
                            },
                        }
                    },
                [1] = new[]
                {
                        new NestedAsyncComponent.ExecutionAction
                        {
                            Event = NestedAsyncComponent.EventType.OnAfterRenderAsyncAsync,
                            EventAction = async () =>
                            {
                                await Task.Yield();
                                taskCompletionSource.TrySetResult();
                                return (1, NestedAsyncComponent.EventType.OnAfterRenderAsyncAsync);
                            },
                        }
                    }
            },
            [nameof(NestedAsyncComponent.WhatToRender)] = new Dictionary<int, Func<NestedAsyncComponent, RenderFragment>>
            {
                [0] = CreateRenderFactory(new[] { 1 }),
                [1] = CreateRenderFactory(Array.Empty<int>()),
            },
        }));

        Assert.True(renderTask.IsCompletedSuccessfully);

        // OnAfterRenderAsync happens in the background. Make it more predictable, by gating it until we're ready to capture exceptions.
        await taskCompletionSource.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
        Assert.Same(exception, Assert.Single(renderer.HandledExceptions).GetBaseException());
    }

    [Fact]
    public async Task ExceptionThrownFromConstructor()
    {
        // Arrange
        var renderer = new TestRenderer { ShouldHandleExceptions = true };
        var component = new TestComponent(builder =>
        {
            builder.OpenComponent<ConstructorThrowingComponent>(0);
            builder.CloseComponent();
        });

        // Act/Assert
        var componentId = renderer.AssignRootComponentId(component);
        var renderTask = renderer.RenderRootComponentAsync(componentId);

        await renderTask;
        Assert.True(renderTask.IsCompletedSuccessfully);
        Assert.Same(ConstructorThrowingComponent.Exception, Assert.Single(renderer.HandledExceptions).GetBaseException());
    }

    private class ConstructorThrowingComponent : IComponent
    {
        public static readonly Exception Exception = new InvalidTimeZoneException();

        public ConstructorThrowingComponent()
        {
            throw Exception;
        }

        public void Attach(RenderHandle renderHandle)
        {
            throw new NotImplementedException();
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public async Task ExceptionThrownFromAttach()
    {
        // Arrange
        var renderer = new TestRenderer { ShouldHandleExceptions = true };
        var component = new TestComponent(builder =>
        {
            builder.OpenComponent<AttachThrowingComponent>(0);
            builder.CloseComponent();
        });

        // Act/Assert
        var componentId = renderer.AssignRootComponentId(component);
        var renderTask = renderer.RenderRootComponentAsync(componentId);

        await renderTask;
        Assert.True(renderTask.IsCompletedSuccessfully);
        Assert.Same(AttachThrowingComponent.Exception, Assert.Single(renderer.HandledExceptions).GetBaseException());
    }

    private class AttachThrowingComponent : IComponent
    {
        public static readonly Exception Exception = new InvalidTimeZoneException();

        public void Attach(RenderHandle renderHandle)
        {
            throw Exception;
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public void SynchronousCancelledTasks_HandleAfterRender_Works()
    {
        // Arrange
        var renderer = new TestRenderer { ShouldHandleExceptions = true };
        var component = new NestedAsyncComponent();
        var tcs = new TaskCompletionSource<(int, NestedAsyncComponent.EventType)>();
        tcs.TrySetCanceled();

        // Act/Assert
        var componentId = renderer.AssignRootComponentId(component);
        var renderTask = renderer.RenderRootComponentAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(NestedAsyncComponent.EventActions)] = new Dictionary<int, IList<NestedAsyncComponent.ExecutionAction>>
            {
                [0] = new[]
                {
                        new NestedAsyncComponent.ExecutionAction
                        {
                            Event = NestedAsyncComponent.EventType.OnAfterRenderAsyncAsync,
                            EventAction = () => tcs.Task,
                        }
                    },
            },
            [nameof(NestedAsyncComponent.WhatToRender)] = new Dictionary<int, Func<NestedAsyncComponent, RenderFragment>>
            {
                [0] = CreateRenderFactory(Array.Empty<int>()),
            },
        }));

        // Rendering should finish synchronously
        Assert.True(renderTask.IsCompletedSuccessfully);
        Assert.Empty(renderer.HandledExceptions);
    }

    [Fact]
    public void AsynchronousCancelledTasks_HandleAfterRender_Works()
    {
        // Arrange
        var renderer = new TestRenderer { ShouldHandleExceptions = true };
        var component = new NestedAsyncComponent();
        var tcs = new TaskCompletionSource<(int, NestedAsyncComponent.EventType)>();

        // Act/Assert
        var componentId = renderer.AssignRootComponentId(component);
        var renderTask = renderer.RenderRootComponentAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(NestedAsyncComponent.EventActions)] = new Dictionary<int, IList<NestedAsyncComponent.ExecutionAction>>
            {
                [0] = new[]
                {
                        new NestedAsyncComponent.ExecutionAction
                        {
                            Event = NestedAsyncComponent.EventType.OnAfterRenderAsyncAsync,
                            EventAction = () => tcs.Task,
                        }
                    },
            },
            [nameof(NestedAsyncComponent.WhatToRender)] = new Dictionary<int, Func<NestedAsyncComponent, RenderFragment>>
            {
                [0] = CreateRenderFactory(Array.Empty<int>()),
            },
        }));

        // Rendering should be complete.
        Assert.True(renderTask.IsCompletedSuccessfully);
        tcs.TrySetCanceled();
        Assert.Empty(renderer.HandledExceptions);
    }

    [Fact]
    public async Task CanceledTasksInHandleAfterRender_AreIgnored()
    {
        // Arrange
        var renderer = new TestRenderer { ShouldHandleExceptions = true };
        var component = new NestedAsyncComponent();
        var taskCompletionSource = new TaskCompletionSource();
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act/Assert
        var componentId = renderer.AssignRootComponentId(component);
        await renderer.RenderRootComponentAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(NestedAsyncComponent.EventActions)] = new Dictionary<int, IList<NestedAsyncComponent.ExecutionAction>>
            {
                [0] = new[]
                {
                        new NestedAsyncComponent.ExecutionAction
                        {
                            Event = NestedAsyncComponent.EventType.OnAfterRenderAsyncSync,
                            EventAction = () =>
                            {
                                taskCompletionSource.TrySetResult();
                                cancellationTokenSource.Token.ThrowIfCancellationRequested();
                                return default;
                            },
                        }
                    },
            },
            [nameof(NestedAsyncComponent.WhatToRender)] = new Dictionary<int, Func<NestedAsyncComponent, RenderFragment>>
            {
                [0] = CreateRenderFactory(Array.Empty<int>()),
            },
        }));

        await taskCompletionSource.Task.TimeoutAfter(TimeSpan.FromSeconds(10));

        Assert.Empty(renderer.HandledExceptions);
    }

    [Fact]
    public void DisposingRenderer_DisposesTopLevelComponents()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new DisposableComponent();
        renderer.AssignRootComponentId(component);

        // Act
        renderer.Dispose();

        // Assert
        Assert.True(component.Disposed);
    }

    [Fact]
    public void DisposingRenderer_DisregardsAttemptsToStartMoreRenderBatches()
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
        renderer.AssignRootComponentId(component);
        renderer.Dispose();
        component.TriggerRender();

        // Assert
        Assert.Empty(renderer.Batches);
    }

    [Fact]
    public void WhenRendererIsDisposed_ComponentRenderRequestsAreSkipped()
    {
        // The important point of this is that user code in components may continue to call
        // StateHasChanged (e.g., after an async task completion), and we don't want that to
        // show up as an error. In general, components should skip rendering after disposal.
        // This test shows that we don't add any new entries to the render queue after disposal.
        // There's a different test showing that if the render queue entry was already added
        // before a component got individually disposed, that render queue entry gets skipped.

        // Arrange
        var renderer = new TestRenderer();
        var component = new DisposableComponent();
        renderer.AssignRootComponentId(component);

        // Act
        renderer.Dispose();
        component.TriggerRender();

        // Assert: no exception, no batch produced
        Assert.Empty(renderer.Batches);
    }

    [Fact]
    public void DisposingRenderer_DisposesNestedComponents()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponent(builder =>
        {
            builder.AddContent(0, "Hello");
            builder.OpenComponent<DisposableComponent>(1);
            builder.CloseComponent();
        });
        var componentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();
        var batch = renderer.Batches.Single();
        var componentFrame = batch.ReferenceFrames
            .Single(frame => frame.FrameType == RenderTreeFrameType.Component);
        var nestedComponent = Assert.IsType<DisposableComponent>(componentFrame.Component);

        // Act
        renderer.Dispose();

        // Assert
        Assert.True(component.Disposed);
        Assert.True(nestedComponent.Disposed);
    }

    [Fact]
    public void DisposingRenderer_CapturesExceptionsFromAllRegisteredComponents()
    {
        // Arrange
        var renderer = new TestRenderer { ShouldHandleExceptions = true };
        var exception1 = new Exception();
        var exception2 = new Exception();
        var component = new TestComponent(builder =>
        {
            builder.AddContent(0, "Hello");
            builder.OpenComponent<DisposableComponent>(1);
            builder.AddComponentParameter(1, nameof(DisposableComponent.DisposeAction), (Action)(() => throw exception1));
            builder.CloseComponent();

            builder.OpenComponent<DisposableComponent>(2);
            builder.AddComponentParameter(1, nameof(DisposableComponent.DisposeAction), (Action)(() => throw exception2));
            builder.CloseComponent();
        });
        var componentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();

        // Act &A Assert
        renderer.Dispose();

        // All components must be disposed even if some throw as part of being disposed.
        Assert.True(component.Disposed);
        var aex = Assert.IsType<AggregateException>(Assert.Single(renderer.HandledExceptions));
        Assert.Contains(exception1, aex.InnerExceptions);
        Assert.Contains(exception2, aex.InnerExceptions);
    }

    [Fact]
    public async Task DisposingRenderer_CapturesSyncExceptionsFromAllRegisteredAsyncDisposableComponents()
    {
        // Arrange
        var renderer = new TestRenderer { ShouldHandleExceptions = true };
        var exception1 = new InvalidOperationException();
        var disposed = false;

        var component = new TestComponent(builder =>
        {
            builder.AddContent(0, "Hello");
            builder.OpenComponent<AsyncDisposableComponent>(1);
            builder.AddComponentParameter(1, nameof(AsyncDisposableComponent.AsyncDisposeAction), (Func<ValueTask>)(() => { disposed = true; throw exception1; }));
            builder.CloseComponent();
        });
        var componentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();

        // Act
        await renderer.DisposeAsync();

        // Assert
        Assert.True(disposed);
        var handledException = Assert.Single(renderer.HandledExceptions);
        Assert.Same(exception1, handledException);
    }

    [Fact]
    public async Task DisposingRenderer_CapturesAsyncExceptionsFromAllRegisteredAsyncDisposableComponents()
    {
        // Arrange
        var renderer = new TestRenderer { ShouldHandleExceptions = true };
        var exception1 = new InvalidOperationException();
        var disposed = false;
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var component = new TestComponent(builder =>
        {
            builder.AddContent(0, "Hello");
            builder.OpenComponent<AsyncDisposableComponent>(1);
            builder.AddComponentParameter(1, nameof(AsyncDisposableComponent.AsyncDisposeAction), (Func<ValueTask>)(async () => { await tcs.Task; disposed = true; throw exception1; }));
            builder.CloseComponent();
        });
        var componentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();

        // Act
        var disposal = renderer.DisposeAsync();
        Assert.False(disposed);
        Assert.False(disposal.IsCompleted);

        tcs.TrySetResult();
        await disposal;

        // Assert
        Assert.True(disposed);
        var handledException = Assert.Single(renderer.HandledExceptions);
        Assert.Same(exception1, handledException);
    }

    [Theory]
    [InlineData(null)] // No existing attribute to update
    [InlineData("old property value")] // Has existing attribute to update
    public void EventFieldInfoCanPatchTreeSoDiffDoesNotUpdateAttribute(string oldValue)
    {
        // Arrange: Render a component with an event handler
        var renderer = new TestRenderer();
        var component = new BoundPropertyComponent { BoundString = oldValue };
        var componentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();

        var eventHandlerId = renderer.Batches.Single()
            .ReferenceFrames
            .First(frame => frame.FrameType == RenderTreeFrameType.Attribute && frame.AttributeEventHandlerId > 0)
            .AttributeEventHandlerId;

        // Act: Fire event and re-render
        var eventFieldInfo = new EventFieldInfo
        {
            FieldValue = "new property value",
            ComponentId = componentId
        };
        var dispatchEventTask = renderer.DispatchEventAsync(eventHandlerId, eventFieldInfo, new ChangeEventArgs
        {
            Value = "new property value"
        });
        Assert.True(dispatchEventTask.IsCompletedSuccessfully);

        // Assert: Property was updated, but the diff doesn't include changing the
        // element attribute, since we told it the element attribute was already updated
        Assert.Equal("new property value", component.BoundString);
        Assert.Equal(2, renderer.Batches.Count);
        var batch2 = renderer.Batches[1];
        Assert.Collection(batch2.DiffsInOrder.Single().Edits.ToArray(), edit =>
        {
            // The only edit is updating the event handler ID, since the test component
            // deliberately uses a capturing lambda. The whole point of this test is to
            // show that the diff does *not* update the BoundString value attribute.
            Assert.Equal(RenderTreeEditType.SetAttribute, edit.Type);
            var attributeFrame = batch2.ReferenceFrames[edit.ReferenceFrameIndex];
            AssertFrame.Attribute(attributeFrame, "ontestevent", typeof(Action<ChangeEventArgs>));
            Assert.NotEqual(default, attributeFrame.AttributeEventHandlerId);
            Assert.NotEqual(eventHandlerId, attributeFrame.AttributeEventHandlerId);
        });
    }

    [Fact]
    public void EventFieldInfoWorksWhenEventHandlerIdWasSuperseded()
    {
        // Arrange: Render a component with an event handler
        // We want the renderer to think none of the "UpdateDisplay" calls ever complete, because we
        // want to keep reusing the same eventHandlerId and not let it get disposed
        var renderCompletedTcs = new TaskCompletionSource();
        var renderer = new TestRenderer { NextRenderResultTask = renderCompletedTcs.Task };
        var component = new BoundPropertyComponent { BoundString = "old property value" };
        var componentId = renderer.AssignRootComponentId(component);

        component.TriggerRender();

        var eventHandlerId = renderer.Batches.Single()
            .ReferenceFrames
            .First(frame => frame.FrameType == RenderTreeFrameType.Attribute && frame.AttributeEventHandlerId > 0)
            .AttributeEventHandlerId;

        // Act: Fire event and re-render *repeatedly*, without changing to use a newer event handler ID,
        // even though we know the event handler ID is getting updated in successive diffs
        for (var i = 0; i < 10; i++)
        {
            var newPropertyValue = $"new property value {i}";
            var fieldInfo = new EventFieldInfo
            {
                ComponentId = componentId,
                FieldValue = newPropertyValue,
            };
            var dispatchEventTask = renderer.DispatchEventAsync(eventHandlerId, fieldInfo, new ChangeEventArgs
            {
                Value = newPropertyValue
            });
            Assert.True(dispatchEventTask.IsCompletedSuccessfully);

            // Assert: Property was updated, but the diff doesn't include changing the
            // element attribute, since we told it the element attribute was already updated
            Assert.Equal(newPropertyValue, component.BoundString);
            Assert.Equal(i + 2, renderer.Batches.Count);
            var latestBatch = renderer.Batches.Last();
            Assert.Collection(latestBatch.DiffsInOrder.Single().Edits.ToArray(), edit =>
            {
                // The only edit is updating the event handler ID, since the test component
                // deliberately uses a capturing lambda. The whole point of this test is to
                // show that the diff does *not* update the BoundString value attribute.
                Assert.Equal(RenderTreeEditType.SetAttribute, edit.Type);
                var attributeFrame = latestBatch.ReferenceFrames[edit.ReferenceFrameIndex];
                AssertFrame.Attribute(attributeFrame, "ontestevent", typeof(Action<ChangeEventArgs>));
                Assert.NotEqual(default, attributeFrame.AttributeEventHandlerId);
                Assert.NotEqual(eventHandlerId, attributeFrame.AttributeEventHandlerId);
            });
        }
    }

    [Fact]
    public void CannotStartOverlappingBatches()
    {
        // Arrange
        var renderer = new InvalidRecursiveRenderer();
        var component = new CallbackOnRenderComponent(renderer.ProcessPendingRender);
        var componentId = renderer.AssignRootComponentId(component);

        // Act/Assert
        var ex = Assert.Throws<InvalidOperationException>(
            () => renderer.RenderRootComponent(componentId));
        Assert.Contains("Cannot start a batch when one is already in progress.", ex.Message);
    }

    [Fact]
    public void CannotAccessParameterViewAfterSynchronousReturn()
    {
        // Arrange
        var renderer = new TestRenderer();
        var rootComponent = new TestComponent(builder =>
        {
            builder.OpenComponent<ParameterViewIllegalCapturingComponent>(0);
            builder.AddComponentParameter(1, nameof(ParameterViewIllegalCapturingComponent.SomeParam), 0);
            builder.CloseComponent();
        });
        var rootComponentId = renderer.AssignRootComponentId(rootComponent);

        // Note that we're not waiting for the async render to complete, since we want to assert
        // about the situation immediately after the component yields the thread
        renderer.RenderRootComponentAsync(rootComponentId);

        // Act/Assert
        var capturingComponent = (ParameterViewIllegalCapturingComponent)renderer.GetCurrentRenderTreeFrames(rootComponentId).Array[0].Component;
        var parameterView = capturingComponent.CapturedParameterView;

        // All public APIs on capturingComponent should be electrified now
        // Internal APIs don't have to be, because we won't call them at the wrong time
        Assert.Throws<InvalidOperationException>(() => parameterView.GetEnumerator());
        Assert.Throws<InvalidOperationException>(() => parameterView.GetValueOrDefault<object>("anything"));
        Assert.Throws<InvalidOperationException>(() => parameterView.SetParameterProperties(new object()));
        Assert.Throws<InvalidOperationException>(parameterView.ToDictionary);
        var ex = Assert.Throws<InvalidOperationException>(() => parameterView.TryGetValue<object>("anything", out _));

        // It's enough to assert about one of the messages
        Assert.Equal($"The {nameof(ParameterView)} instance can no longer be read because it has expired. {nameof(ParameterView)} can only be read synchronously and must not be stored for later use.", ex.Message);
    }

    [Fact]
    public async Task CanSetComponentParameter_WhenParameterTypeHasImplicitConversionToString()
    {
        // Arrange
        var renderer = new TestRenderer();
        var parameterValue = new ImplicitlyConvertsToString("Hello");
        var rootComponent = new TestComponent(builder =>
        {
            builder.OpenComponent<ImplicitConversionComponent>(0);
            builder.AddComponentParameter(1, nameof(ImplicitConversionComponent.SomeParam), parameterValue);
            builder.CloseComponent();
        });

        // Act
        var rootComponentId = renderer.AssignRootComponentId(rootComponent);
        await renderer.RenderRootComponentAsync(rootComponentId);
        var capturingComponent = (ImplicitConversionComponent)renderer.GetCurrentRenderTreeFrames(rootComponentId).Array[0].Component;

        // Assert
        Assert.Same(parameterValue, capturingComponent.SomeParam);
    }

    [Fact]
    public void CanUseCustomComponentActivatorFromConstructorParameter()
    {
        // Arrange
        var serviceProvider = new TestServiceProvider();
        var componentActivator = new TestComponentActivator<MessageComponent>();
        var renderer = new TestRenderer(serviceProvider, componentActivator);

        // Act: Ask for TestComponent
        var suppliedComponent = renderer.InstantiateComponent<TestComponent>();

        // Assert: We actually receive MessageComponent
        Assert.IsType<MessageComponent>(suppliedComponent);
        Assert.Collection(componentActivator.RequestedComponentTypes,
            requestedType => Assert.Equal(typeof(TestComponent), requestedType));
    }

    [Fact]
    public void CanUseCustomComponentActivatorFromServiceProvider()
    {
        // Arrange
        var serviceProvider = new TestServiceProvider();
        var componentActivator = new TestComponentActivator<MessageComponent>();
        serviceProvider.AddService<IComponentActivator>(componentActivator);
        var renderer = new TestRenderer(serviceProvider);

        // Act: Ask for TestComponent
        var suppliedComponent = renderer.InstantiateComponent<TestComponent>();

        // Assert: We actually receive MessageComponent
        Assert.IsType<MessageComponent>(suppliedComponent);
        Assert.Collection(componentActivator.RequestedComponentTypes,
            requestedType => Assert.Equal(typeof(TestComponent), requestedType));
    }

    [Fact]
    public async Task ThrowsIfComponentProducesInvalidRenderTree()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponent(builder =>
        {
            builder.OpenElement(0, "myElem");
        });
        var rootComponentId = renderer.AssignRootComponentId(component);

        // Act/Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => renderer.RenderRootComponentAsync(rootComponentId));
        Assert.StartsWith($"Render output is invalid for component of type '{typeof(TestComponent).FullName}'. A frame of type 'Element' was left unclosed.", ex.Message);
    }

    [Fact]
    public void RenderingExceptionsCanBeHandledByClosestErrorBoundary()
    {
        // Arrange
        var renderer = new TestRenderer();
        var exception = new InvalidTimeZoneException("Error during render");
        var rootComponentId = renderer.AssignRootComponentId(new TestComponent(builder =>
        {
            TestErrorBoundary.RenderNestedErrorBoundaries(builder, builder =>
            {
                builder.OpenComponent<ErrorThrowingComponent>(0);
                builder.AddComponentParameter(1, nameof(ErrorThrowingComponent.ThrowDuringRender), exception);
                builder.CloseComponent();
            });
        }));

        // Act
        renderer.RenderRootComponent(rootComponentId);

        // Assert
        var batch = renderer.Batches.Single();
        var errorThrowingComponentId = batch.GetComponentFrames<ErrorThrowingComponent>().Single().ComponentId;
        var componentFrames = batch.GetComponentFrames<TestErrorBoundary>();
        Assert.Collection(componentFrames.Select(f => (TestErrorBoundary)f.Component),
            component => Assert.Null(component.ReceivedException),
            component => Assert.Same(exception, component.ReceivedException));

        // The failed subtree is disposed
        Assert.Equal(errorThrowingComponentId, batch.DisposedComponentIDs.Single());
    }

    [Fact]
    public void SetParametersAsyncExceptionsCanBeHandledByClosestErrorBoundary_Sync()
    {
        // Arrange
        var renderer = new TestRenderer();
        Exception exception = null;
        var rootComponent = new TestComponent(builder =>
        {
            TestErrorBoundary.RenderNestedErrorBoundaries(builder, builder =>
            {
                builder.OpenComponent<ErrorThrowingComponent>(0);
                builder.AddComponentParameter(1, nameof(ErrorThrowingComponent.ThrowDuringParameterSettingSync), exception);
                builder.CloseComponent();
            });
        });
        var rootComponentId = renderer.AssignRootComponentId(rootComponent);
        renderer.RenderRootComponent(rootComponentId);
        var errorBoundaries = renderer.Batches.Single().GetComponentFrames<TestErrorBoundary>()
            .Select(f => (TestErrorBoundary)f.Component);
        var errorThrowingComponentId = renderer.Batches.Single()
            .GetComponentFrames<ErrorThrowingComponent>().Single().ComponentId;

        // Act
        exception = new InvalidTimeZoneException("Error during SetParametersAsync");
        rootComponent.TriggerRender();

        // Assert
        Assert.Equal(2, renderer.Batches.Count);
        Assert.Collection(errorBoundaries,
            component => Assert.Null(component.ReceivedException),
            component => Assert.Same(exception, component.ReceivedException));

        // The failed subtree is disposed
        Assert.Equal(errorThrowingComponentId, renderer.Batches[1].DisposedComponentIDs.Single());
    }

    [Fact]
    public async Task SetParametersAsyncExceptionsCanBeHandledByClosestErrorBoundary_Async()
    {
        // Arrange
        var renderer = new TestRenderer();
        var exception = new InvalidTimeZoneException("Error during SetParametersAsync");
        TaskCompletionSource exceptionTcs = null;
        var rootComponent = new TestComponent(builder =>
        {
            TestErrorBoundary.RenderNestedErrorBoundaries(builder, builder =>
            {
                builder.OpenComponent<ErrorThrowingComponent>(0);
                builder.AddComponentParameter(1, nameof(ErrorThrowingComponent.ThrowDuringParameterSettingAsync), exceptionTcs?.Task);
                builder.CloseComponent();
            });
        });
        var rootComponentId = renderer.AssignRootComponentId(rootComponent);
        renderer.RenderRootComponent(rootComponentId);
        var errorBoundaries = renderer.Batches.Single().GetComponentFrames<TestErrorBoundary>()
            .Select(f => (TestErrorBoundary)f.Component).ToArray();
        var errorThrowingComponentId = renderer.Batches.Single()
            .GetComponentFrames<ErrorThrowingComponent>().Single().ComponentId;

        // Act/Assert 1: No synchronous errors
        exceptionTcs = new TaskCompletionSource();
        rootComponent.TriggerRender();
        Assert.Equal(2, renderer.Batches.Count);

        // Act/Assert 2: Asynchronous error
        exceptionTcs.SetException(exception);
        await errorBoundaries[1].ReceivedErrorTask;
        Assert.Equal(3, renderer.Batches.Count);
        Assert.Collection(errorBoundaries,
            component => Assert.Null(component.ReceivedException),
            component => Assert.Same(exception, component.ReceivedException));

        // The failed subtree is disposed
        Assert.Equal(errorThrowingComponentId, renderer.Batches[2].DisposedComponentIDs.Single());
    }

    [Fact]
    public void EventDispatchExceptionsCanBeHandledByClosestErrorBoundary_Sync()
    {
        // Arrange
        var renderer = new TestRenderer();
        var exception = new InvalidTimeZoneException("Error during event");
        var rootComponentId = renderer.AssignRootComponentId(new TestComponent(builder =>
        {
            TestErrorBoundary.RenderNestedErrorBoundaries(builder, builder =>
            {
                builder.OpenComponent<ErrorThrowingComponent>(0);
                builder.AddComponentParameter(1, nameof(ErrorThrowingComponent.ThrowDuringEventSync), exception);
                builder.CloseComponent();
            });
        }));
        renderer.RenderRootComponent(rootComponentId);
        var errorBoundaries = renderer.Batches.Single().GetComponentFrames<TestErrorBoundary>()
            .Select(f => (TestErrorBoundary)f.Component);
        var errorThrowingComponentId = renderer.Batches.Single()
            .GetComponentFrames<ErrorThrowingComponent>().Single().ComponentId;
        var eventHandlerId = renderer.Batches.Single().ReferenceFrames
            .Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "onmakeerror")
            .AttributeEventHandlerId;

        // Act
        var task = renderer.DispatchEventAsync(eventHandlerId, new EventArgs());

        // Assert
        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(2, renderer.Batches.Count);
        Assert.Collection(errorBoundaries,
            component => Assert.Null(component.ReceivedException),
            component => Assert.Same(exception, component.ReceivedException));

        // The failed subtree is disposed
        Assert.Equal(errorThrowingComponentId, renderer.Batches[1].DisposedComponentIDs.Single());
    }

    [Fact]
    public async Task EventDispatchExceptionsCanBeHandledByClosestErrorBoundary_Async()
    {
        // Arrange
        var renderer = new TestRenderer();
        var exception = new InvalidTimeZoneException("Error during event");
        var exceptionTcs = new TaskCompletionSource();
        var rootComponentId = renderer.AssignRootComponentId(new TestComponent(builder =>
        {
            TestErrorBoundary.RenderNestedErrorBoundaries(builder, builder =>
            {
                builder.OpenComponent<ErrorThrowingComponent>(0);
                builder.AddComponentParameter(1, nameof(ErrorThrowingComponent.ThrowDuringEventAsync), exceptionTcs.Task);
                builder.CloseComponent();
            });
        }));
        renderer.RenderRootComponent(rootComponentId);
        var errorBoundaries = renderer.Batches.Single().GetComponentFrames<TestErrorBoundary>()
            .Select(f => (TestErrorBoundary)f.Component);
        var errorThrowingComponentId = renderer.Batches.Single()
            .GetComponentFrames<ErrorThrowingComponent>().Single().ComponentId;
        var eventHandlerId = renderer.Batches.Single().ReferenceFrames
            .Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "onmakeerror")
            .AttributeEventHandlerId;

        // Act/Assert 1: No error synchronously
        var dispatchEventTask = renderer.DispatchEventAsync(eventHandlerId, new EventArgs());
        Assert.Single(renderer.Batches);
        Assert.Collection(errorBoundaries,
            component => Assert.Null(component.ReceivedException),
            component => Assert.Null(component.ReceivedException));

        // Act/Assert 2: Error is handled asynchronously
        exceptionTcs.SetException(exception);
        await dispatchEventTask;
        Assert.Equal(2, renderer.Batches.Count);
        Assert.Collection(errorBoundaries,
            component => Assert.Null(component.ReceivedException),
            component => Assert.Same(exception, component.ReceivedException));

        // The failed subtree is disposed
        Assert.Equal(errorThrowingComponentId, renderer.Batches[1].DisposedComponentIDs.Single());
    }

    [Fact]
    public async Task EventDispatchExceptionsCanBeHandledByClosestErrorBoundary_AfterDisposal()
    {
        // Arrange
        var renderer = new TestRenderer();
        var disposeChildren = false;
        var exception = new InvalidTimeZoneException("Error during event");
        var exceptionTcs = new TaskCompletionSource();
        var rootComponent = new TestComponent(builder =>
        {
            if (!disposeChildren)
            {
                TestErrorBoundary.RenderNestedErrorBoundaries(builder, builder =>
                {
                    builder.OpenComponent<ErrorThrowingComponent>(0);
                    builder.AddComponentParameter(1, nameof(ErrorThrowingComponent.ThrowDuringEventAsync), exceptionTcs.Task);
                    builder.CloseComponent();
                });
            }
        });
        var rootComponentId = renderer.AssignRootComponentId(rootComponent);
        renderer.RenderRootComponent(rootComponentId);
        var errorBoundaries = renderer.Batches.Single().GetComponentFrames<TestErrorBoundary>()
            .Select(f => (TestErrorBoundary)f.Component);
        var errorThrowingComponentId = renderer.Batches.Single()
            .GetComponentFrames<ErrorThrowingComponent>().Single().ComponentId;
        var eventHandlerId = renderer.Batches.Single().ReferenceFrames
            .Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "onmakeerror")
            .AttributeEventHandlerId;

        // Act/Assert 1: No error synchronously
        var dispatchEventTask = renderer.DispatchEventAsync(eventHandlerId, new EventArgs());
        Assert.Single(renderer.Batches);
        Assert.Collection(errorBoundaries,
            component => Assert.Null(component.ReceivedException),
            component => Assert.Null(component.ReceivedException));

        // Act 2: Before the async error occurs, dispose the hierarchy containing the error boundary and erroring component
        disposeChildren = true;
        rootComponent.TriggerRender();
        Assert.Equal(2, renderer.Batches.Count);
        Assert.Contains(errorThrowingComponentId, renderer.Batches.Last().DisposedComponentIDs);

        // Assert 2: Error is still handled
        exceptionTcs.SetException(exception);
        await dispatchEventTask;
        Assert.Equal(2, renderer.Batches.Count); // Didn't re-render as the error boundary was already gone
        Assert.Collection(errorBoundaries,
            component => Assert.Null(component.ReceivedException),
            component => Assert.Same(exception, component.ReceivedException));
    }

    [Fact]
    public async Task CanRemoveRootComponents()
    {
        // Arrange
        var renderer = new TestRenderer();
        var rootComponent = new TestComponent(builder =>
        {
            builder.OpenComponent<DisposableComponent>(0);
            builder.CloseComponent();

            builder.OpenComponent<AsyncDisposableComponent>(1);
            builder.CloseComponent();
        });
        var unrelatedComponent = new DisposableComponent();
        var rootComponentId = renderer.AssignRootComponentId(rootComponent);
        var unrelatedRootComponentId = renderer.AssignRootComponentId(unrelatedComponent);
        rootComponent.TriggerRender();
        unrelatedComponent.TriggerRender();
        Assert.Equal(2, renderer.Batches.Count);

        var nestedDisposableComponentFrame = renderer.Batches[0]
            .GetComponentFrames<DisposableComponent>().Single();
        var nestedAsyncDisposableComponentFrame = renderer.Batches[0]
            .GetComponentFrames<AsyncDisposableComponent>().Single();

        // Act
        _ = renderer.Dispatcher.InvokeAsync(() => renderer.RemoveRootComponent(rootComponentId));

        // Assert: we disposed the specified root component and its descendants, but not
        // the other root component
        Assert.Equal(3, renderer.Batches.Count);
        var batch = renderer.Batches.Last();
        Assert.Equal(new[]
        {
                rootComponentId,
                nestedDisposableComponentFrame.ComponentId,
                nestedAsyncDisposableComponentFrame.ComponentId,
            }, batch.DisposedComponentIDs);

        // Assert: component instances were disposed properly
        Assert.True(((DisposableComponent)nestedDisposableComponentFrame.Component).Disposed);
        Assert.True(((AsyncDisposableComponent)nestedAsyncDisposableComponentFrame.Component).Disposed);

        // Assert: it's no longer known as a component
        await renderer.Dispatcher.InvokeAsync(() =>
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                renderer.RemoveRootComponent(rootComponentId));
            Assert.Equal($"The renderer does not have a component with ID {rootComponentId}.", ex.Message);
        });
    }

    [Fact]
    public async Task CannotRemoveSameRootComponentMultipleTimesSynchronously()
    {
        // Arrange
        var renderer = new TestRenderer();
        var rootComponent = new AsyncDisposableComponent
        {
            // Show that, even if the component tries to delay its disposal by returning
            // a task that never completes, it still gets removed from the renderer synchronously
            AsyncDisposeAction = () => new ValueTask(new TaskCompletionSource().Task)
        };
        var rootComponentId = renderer.AssignRootComponentId(rootComponent);

        // Act/Assert
        var didRunTestLogic = false; // Don't just trust the dispatcher here - verify it runs our callback
        await renderer.Dispatcher.InvokeAsync(() =>
        {
            renderer.RemoveRootComponent(rootComponentId);

            // Even though we didn't await anything, it's synchronously unavailable for re-removal
            var ex = Assert.Throws<ArgumentException>(() =>
            renderer.RemoveRootComponent(rootComponentId));
            Assert.Equal($"The renderer does not have a component with ID {rootComponentId}.", ex.Message);
            didRunTestLogic = true;
        });

        Assert.True(didRunTestLogic);
    }

    [Fact]
    public async Task CannotRemoveNonRootComponentsDirectly()
    {
        // Arrange
        var renderer = new TestRenderer();
        var rootComponent = new TestComponent(builder =>
        {
            builder.OpenComponent<DisposableComponent>(0);
            builder.CloseComponent();
        });
        var rootComponentId = renderer.AssignRootComponentId(rootComponent);
        rootComponent.TriggerRender();

        var nestedComponentFrame = renderer.Batches[0]
            .GetComponentFrames<DisposableComponent>().Single();
        var nestedComponent = (DisposableComponent)nestedComponentFrame.Component;

        // Act/Assert
        await renderer.Dispatcher.InvokeAsync(() =>
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                renderer.RemoveRootComponent(nestedComponentFrame.ComponentId));
            Assert.Equal("The specified component is not a root component", ex.Message);
        });

        Assert.False(nestedComponent.Disposed);
    }

    [Fact]
    public void RemoveRootComponentHandlesDisposalExceptions()
    {
        // Arrange
        var autoResetEvent = new AutoResetEvent(false);
        var renderer = new TestRenderer
        {
            ShouldHandleExceptions = true,
            OnExceptionHandled = () => autoResetEvent.Set()
        };
        var exception1 = new InvalidTimeZoneException();
        var exception2Tcs = new TaskCompletionSource();
        var rootComponent = new TestComponent(builder =>
        {
            builder.AddContent(0, "Hello");
            builder.OpenComponent<DisposableComponent>(1);
            builder.AddComponentParameter(1, nameof(DisposableComponent.DisposeAction), (Action)(() => throw exception1));
            builder.CloseComponent();

            builder.OpenComponent<AsyncDisposableComponent>(2);
            builder.AddComponentParameter(1, nameof(AsyncDisposableComponent.AsyncDisposeAction), (Func<ValueTask>)(async () => await exception2Tcs.Task));
            builder.CloseComponent();
        });
        var rootComponentId = renderer.AssignRootComponentId(rootComponent);
        rootComponent.TriggerRender();
        Assert.Single(renderer.Batches);

        var nestedDisposableComponentFrame = renderer.Batches[0]
            .GetComponentFrames<DisposableComponent>().Single();
        var nestedAsyncDisposableComponentFrame = renderer.Batches[0]
            .GetComponentFrames<AsyncDisposableComponent>().Single();

        // Act
        renderer.Dispatcher.InvokeAsync(() => renderer.RemoveRootComponent(rootComponentId));

        // Assert: we get the synchronous exception synchronously
        Assert.Same(exception1, Assert.Single(renderer.HandledExceptions));

        // Assert: we get the asynchronous exception asynchronously
        var exception2 = new InvalidTimeZoneException();
        autoResetEvent.Reset();
        exception2Tcs.SetException(exception2);
        autoResetEvent.WaitOne();
        Assert.Equal(2, renderer.HandledExceptions.Count);
        Assert.Same(exception2, renderer.HandledExceptions[1]);
    }

    [Fact]
    public void DisposeCallsComponentDisposeOnSyncContext()
    {
        // Arrange
        var renderer = new TestRenderer();
        var wasOnSyncContext = false;
        var component = new DisposableComponent
        {
            DisposeAction = () =>
            {
                wasOnSyncContext = renderer.Dispatcher.CheckAccess();
            }
        };

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        renderer.Dispose();

        // Assert
        Assert.True(wasOnSyncContext);
    }

    [Fact]
    public async Task DisposeAsyncCallsComponentDisposeAsyncOnSyncContext()
    {
        // Arrange
        var renderer = new TestRenderer();
        var wasOnSyncContext = false;
        var component = new AsyncDisposableComponent
        {
            AsyncDisposeAction = () =>
            {
                wasOnSyncContext = renderer.Dispatcher.CheckAccess();
                return ValueTask.CompletedTask;
            }
        };

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        await renderer.DisposeAsync();

        // Assert
        Assert.True(wasOnSyncContext);
    }

    [Fact]
    public async Task NoHotReloadListenersAreRegistered_WhenMetadataUpdatesAreNotSupported()
    {
        // Arrange
        await using var renderer = new TestRenderer();
        var hotReloadManager = new HotReloadManager { MetadataUpdateSupported = false };
        renderer.HotReloadManager = hotReloadManager;
        var component = new TestComponent(builder =>
        {
            builder.OpenElement(0, "h2");
            builder.AddContent(1, "some text");
            builder.CloseElement();
        });

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();
        Assert.False(hotReloadManager.IsSubscribedTo);

        await renderer.DisposeAsync();
    }

    [Fact]
    public async Task DisposingRenderer_UnsubsribesFromHotReloadManager()
    {
        // Arrange
        var renderer = new TestRenderer();
        var hotReloadManager = new HotReloadManager { MetadataUpdateSupported = true };
        renderer.HotReloadManager = hotReloadManager;
        var component = new TestComponent(builder =>
        {
            builder.OpenElement(0, "h2");
            builder.AddContent(1, "some text");
            builder.CloseElement();
        });

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();
        Assert.True(hotReloadManager.IsSubscribedTo);

        await renderer.DisposeAsync();

        // Assert
        Assert.False(hotReloadManager.IsSubscribedTo);
    }

    [Fact]
    public void ThrowsForUnknownRenderMode_OnComponentType()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponent(builder =>
        {
            builder.OpenComponent<ComponentWithUnknownRenderMode>(0);
            builder.CloseComponent();
        });

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        var ex = Assert.Throws<NotSupportedException>(() => component.TriggerRender());
        Assert.Contains($"Cannot supply a component of type '{typeof(ComponentWithUnknownRenderMode)}' because the current platform does not support the render mode '{typeof(ComponentWithUnknownRenderMode.UnknownRenderMode)}'.", ex.Message);
    }

    [Fact]
    public void ThrowsForUnknownRenderMode_AtCallSite()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponent(builder =>
        {
            builder.OpenComponent<TestComponent>(0);
            builder.AddComponentRenderMode(new ComponentWithUnknownRenderMode.UnknownRenderMode());
            builder.CloseComponent();
        });

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        var ex = Assert.Throws<NotSupportedException>(component.TriggerRender);
        Assert.Contains($"Cannot supply a component of type '{typeof(TestComponent)}' because the current platform does not support the render mode '{typeof(ComponentWithUnknownRenderMode.UnknownRenderMode)}'.", ex.Message);
    }

    [Fact]
    public void RenderModeResolverCanSupplyComponent_WithComponentTypeRenderMode()
    {
        // Arrange
        var renderer = new RendererWithRenderModeResolver();

        var component = new TestComponent(builder =>
        {
            builder.OpenComponent<ComponentWithRenderMode>(0);
            builder.AddComponentParameter(1, nameof(MessageComponent.Message), "Some message");
            builder.CloseComponent();
        });

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();

        // Assert
        var batch = renderer.Batches.Single();
        var componentFrames = batch.GetComponentFrames<MessageComponent>();
        var resolvedComponent = (MessageComponent)componentFrames.Single().Component;
        Assert.Equal("Some message", resolvedComponent.Message);
    }

    [Fact]
    public void RenderModeResolverCanSupplyComponent_CallSiteRenderMode()
    {
        // Arrange
        var renderer = new RendererWithRenderModeResolver();

        var component = new TestComponent(builder =>
        {
            builder.OpenComponent<TestComponent>(0);
            builder.AddComponentParameter(1, nameof(MessageComponent.Message), "Some message");
            builder.AddComponentRenderMode(new SubstituteComponentRenderMode());
            builder.CloseComponent();
        });

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();

        // Assert
        var batch = renderer.Batches.Single();
        var componentFrames = batch.GetComponentFrames<MessageComponent>();
        var resolvedComponent = (MessageComponent)componentFrames.Single().Component;
        Assert.Equal("Some message", resolvedComponent.Message);
    }

    [HasSubstituteComponentRenderMode]
    private class ComponentWithRenderMode : IComponent
    {
        public void Attach(RenderHandle renderHandle) => throw new NotImplementedException();
        public Task SetParametersAsync(ParameterView parameters) => throw new NotImplementedException();

        public class HasSubstituteComponentRenderMode : RenderModeAttribute
        {
            public override IComponentRenderMode Mode => new SubstituteComponentRenderMode();
        }
    }

    [HasUnknownRenderMode]
    private class ComponentWithUnknownRenderMode : IComponent
    {
        public void Attach(RenderHandle renderHandle) => throw new NotImplementedException();
        public Task SetParametersAsync(ParameterView parameters) => throw new NotImplementedException();

        public class HasUnknownRenderMode : RenderModeAttribute
        {
            public override IComponentRenderMode Mode => new UnknownRenderMode();
        }

        public class UnknownRenderMode : IComponentRenderMode { }
    }

    private class RendererWithRenderModeResolver : TestRenderer
    {
        protected internal override IComponent ResolveComponentForRenderMode(Type componentType, int? parentComponentId, IComponentActivator componentActivator, IComponentRenderMode renderMode)
        {
            return renderMode switch
            {
                SubstituteComponentRenderMode => componentActivator.CreateInstance(typeof(MessageComponent)),
                var other => throw new NotSupportedException($"{nameof(RendererWithRenderModeResolver)} should not have received rendermode {other}"),
            };
        }
    }

    private class SubstituteComponentRenderMode : IComponentRenderMode { }

    private class TestComponentActivator<TResult> : IComponentActivator where TResult : IComponent, new()
    {
        public List<Type> RequestedComponentTypes { get; } = new List<Type>();

        public IComponent CreateInstance(Type componentType)
        {
            RequestedComponentTypes.Add(componentType);
            return new TResult();
        }
    }

    private class NoOpRenderer : Renderer
    {
        public NoOpRenderer() : base(new TestServiceProvider(), NullLoggerFactory.Instance)
        {
        }

        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        public new int AssignRootComponentId(IComponent component)
            => base.AssignRootComponentId(component);

        protected override void HandleException(Exception exception)
            => throw new NotImplementedException();

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
            => Task.CompletedTask;
    }

    private class TestComponent : IComponent, IDisposable
    {
        private RenderHandle _renderHandle;
        private readonly RenderFragment _renderFragment;

        public TestComponent(RenderFragment renderFragment)
        {
            _renderFragment = renderFragment;
        }

        public void Attach(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            TriggerRender();
            return Task.CompletedTask;
        }

        public void TriggerRender()
        {
            var t = _renderHandle.Dispatcher.InvokeAsync(() => _renderHandle.Render(_renderFragment));
            // This should always be run synchronously
            Assert.True(t.IsCompleted);
            if (t.IsFaulted)
            {
                var exception = t.Exception.Flatten().InnerException;
                while (exception is AggregateException e)
                {
                    exception = e.InnerException;
                }
                ExceptionDispatchInfo.Capture(exception).Throw();
            }
        }

        public bool Disposed { get; private set; }

        void IDisposable.Dispose() => Disposed = true;
    }

    private class MessageComponent : AutoRenderComponent
    {
        [Parameter]
        public string Message { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, Message);
        }
    }

    private class MyStrongComponent : AutoRenderComponent
    {
        [Parameter(CaptureUnmatchedValues = true)] public IDictionary<string, object> Attributes { get; set; }

        [Parameter] public string Text { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "strong");
            builder.AddMultipleAttributes(1, Attributes);
            builder.AddContent(2, Text);
            builder.CloseElement();
        }
    }

    private class FakeComponent : IComponent
    {
        [Parameter]
        public int IntProperty { get; set; }

        [Parameter]
        public string StringProperty { get; set; }

        [Parameter]
        public object ObjectProperty { get; set; }

        public RenderHandle RenderHandle { get; private set; }

        public void Attach(RenderHandle renderHandle)
            => RenderHandle = renderHandle;

        public Task SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);
            return Task.CompletedTask;
        }
    }

    private class EventComponent : AutoRenderComponent, IComponent, IHandleEvent
    {
        [Parameter]
        public Action<EventArgs> OnTest { get; set; }

        [Parameter]
        public Func<EventArgs, Task> OnTestAsync { get; set; }

        [Parameter]
        public Action<DerivedEventArgs> OnClick { get; set; }

        [Parameter]
        public Func<DerivedEventArgs, Task> OnClickAsync { get; set; }

        [Parameter]
        public Action OnClickAction { get; set; }

        [Parameter]
        public Func<Task> OnClickAsyncAction { get; set; }

        [Parameter]
        public EventCallback OnClickEventCallback { get; set; }

        [Parameter]
        public EventCallback<DerivedEventArgs> OnClickEventCallbackOfT { get; set; }

        [Parameter]
        public Delegate OnArbitraryDelegateEvent { get; set; }

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
                else if (OnTestAsync != null)
                {
                    builder.AddAttribute(3, "ontest", OnTestAsync);
                }

                if (OnClick != null)
                {
                    builder.AddAttribute(4, "onclick", OnClick);
                }
                else if (OnClickAsync != null)
                {
                    builder.AddAttribute(4, "onclick", OnClickAsync);
                }
                else if (OnClickEventCallback.HasDelegate)
                {
                    builder.AddAttribute(4, "onclick", OnClickEventCallback);
                }
                else if (OnClickEventCallbackOfT.HasDelegate)
                {
                    builder.AddAttribute(4, "onclick", OnClickEventCallbackOfT);
                }

                if (OnClickAction != null)
                {
                    builder.AddAttribute(5, "onclickaction", OnClickAction);
                }
                else if (OnClickAsyncAction != null)
                {
                    builder.AddAttribute(5, "onclickaction", OnClickAsyncAction);
                }

                if (OnArbitraryDelegateEvent != null)
                {
                    builder.AddAttribute(6, "onarbitrarydelegateevent", OnArbitraryDelegateEvent);
                }

                builder.CloseElement();
                builder.CloseElement();
            }
            builder.CloseElement();
            builder.AddContent(6, $"Render count: {++renderCount}");
        }

        public Task HandleEventAsync(EventCallbackWorkItem callback, object arg)
        {
            // Notice, we don't re-render.
            return callback.InvokeAsync(arg);
        }
    }

    private class ConditionalParentComponent<T> : AutoRenderComponent where T : IComponent
    {
        [Parameter]
        public bool IncludeChild { get; set; }

        [Parameter]
        public IDictionary<string, object> ChildParameters { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, "Parent here");

            if (IncludeChild)
            {
                builder.OpenComponent<T>(1);
                if (ChildParameters != null)
                {
                    foreach (var kvp in ChildParameters)
                    {
                        builder.AddComponentParameter(2, kvp.Key, kvp.Value);
                    }
                }
                builder.CloseComponent();
            }
        }
    }

    private class ReRendersParentComponent : AutoRenderComponent
    {
        [Parameter]
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
        [Parameter]
        public Action<object> OnClick { get; set; }

        private RenderHandle _renderHandle;

        public void Attach(RenderHandle renderHandle)
            => _renderHandle = renderHandle;

        public Task SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);
            Render();
            return Task.CompletedTask;
        }

        public Task HandleEventAsync(EventCallbackWorkItem callback, object arg)
        {
            var task = callback.InvokeAsync(arg);
            Render();
            return task;
        }

        private void Render()
            => _renderHandle.Render(builder =>
            {
                builder.OpenElement(0, "my button");
                builder.AddAttribute(1, "onmycustomevent", EventCallback.Factory.Create(this, eventArgs => OnClick(eventArgs)));
                builder.CloseElement();
            });
    }

    private class MultiRendererComponent : IComponent
    {
        private readonly List<RenderHandle> _renderHandles
            = new List<RenderHandle>();

        public void Attach(RenderHandle renderHandle)
            => _renderHandles.Add(renderHandle);

        public Task SetParametersAsync(ParameterView parameters)
        {
            return Task.CompletedTask;
        }

        public void TriggerRender()
        {
            foreach (var renderHandle in _renderHandles)
            {
                renderHandle.Dispatcher.InvokeAsync(() => renderHandle.Render(builder =>
                {
                    builder.AddContent(0, $"Hello from {nameof(MultiRendererComponent)}");
                }));
            }
        }
    }

    private class BindPlusConditionalAttributeComponent : AutoRenderComponent, IHandleEvent
    {
        public bool CheckboxEnabled;
        public string SomeStringProperty;

        public Task HandleEventAsync(EventCallbackWorkItem callback, object arg)
        {
            var task = callback.InvokeAsync(arg);
            TriggerRender();
            return task;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "input");
            builder.AddAttribute(1, "type", "checkbox");
            builder.AddAttribute(2, "value", BindConverter.FormatValue(CheckboxEnabled));
            builder.AddAttribute(3, "onchange", EventCallback.Factory.CreateBinder<bool>(this, __value => CheckboxEnabled = __value, CheckboxEnabled));
            builder.CloseElement();
            builder.OpenElement(4, "input");
            builder.AddAttribute(5, "value", BindConverter.FormatValue(SomeStringProperty));
            builder.AddAttribute(6, "onchange", EventCallback.Factory.CreateBinder<string>(this, __value => SomeStringProperty = __value, SomeStringProperty));
            builder.AddAttribute(7, "disabled", !CheckboxEnabled);
            builder.CloseElement();
        }
    }

    private class AfterRenderCaptureComponent : AutoRenderComponent, IComponent, IHandleAfterRender
    {
        public Action OnAfterRenderLogic { get; set; }

        public int OnAfterRenderCallCount { get; private set; }

        public Task OnAfterRenderAsync()
        {
            OnAfterRenderCallCount++;
            OnAfterRenderLogic?.Invoke();
            return Task.CompletedTask;
        }

        Task IComponent.SetParametersAsync(ParameterView parameters)
        {
            TriggerRender();
            return Task.CompletedTask;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
        }
    }

    private class DisposableComponent : AutoRenderComponent, IDisposable
    {
        public bool Disposed { get; private set; }

        [Parameter]
        public Action DisposeAction { get; set; }

        public void Dispose()
        {
            Disposed = true;
            DisposeAction?.Invoke();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
        }
    }

    private class AsyncDisposableComponent : AutoRenderComponent, IAsyncDisposable
    {
        public bool Disposed { get; private set; }

        [Parameter]
        public Func<ValueTask> AsyncDisposeAction { get; set; }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return AsyncDisposeAction == null ? default : AsyncDisposeAction.Invoke();
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

    private class AsyncComponent : IComponent
    {
        private RenderHandle _renderHandler;

        public AsyncComponent(Task taskToAwait, int number)
        {
            _taskToAwait = taskToAwait;
            Number = number;
        }

        private readonly Task _taskToAwait;

        public int Number { get; set; }

        public void Attach(RenderHandle renderHandle)
        {
            _renderHandler = renderHandle;
        }

        public async Task SetParametersAsync(ParameterView parameters)
        {
            int n;
            while (Number > 0)
            {
                n = Number;
                _renderHandler.Render(CreateFragment);
                Number--;
                await _taskToAwait;
            };

            // Cheap closure
            void CreateFragment(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, "p");
                builder.AddContent(1, n);
                builder.CloseElement();
            }
        }
    }

    private class OuterEventComponent : IComponent, IHandleEvent
    {
        private RenderHandle _renderHandle;

        public RenderFragment RenderFragment { get; set; }

        public Action OnEvent { get; set; }

        public int SomeMethodCallCount { get; set; }

        public void SomeMethod()
        {
            SomeMethodCallCount++;
        }

        public void Attach(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        public Task HandleEventAsync(EventCallbackWorkItem callback, object arg)
        {
            var task = callback.InvokeAsync(arg);
            OnEvent?.Invoke();
            return task;
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            return TriggerRenderAsync();
        }

        public Task TriggerRenderAsync() => _renderHandle.Dispatcher.InvokeAsync(() => _renderHandle.Render(RenderFragment));
    }

    private void AssertStream(int expectedId, (int id, NestedAsyncComponent.EventType @event)[] logStream)
    {
        // OnInit runs first
        Assert.Equal((expectedId, NestedAsyncComponent.EventType.OnInit), logStream[0]);

        // OnInit async completes
        Assert.Single(logStream.Skip(1),
            e => e == (expectedId, NestedAsyncComponent.EventType.OnInitAsyncAsync) || e == (expectedId, NestedAsyncComponent.EventType.OnInitAsyncSync));

        var parametersSetEvent = logStream.Where(le => le == (expectedId, NestedAsyncComponent.EventType.OnParametersSet)).ToArray();
        // OnParametersSet gets called at least once
        Assert.NotEmpty(parametersSetEvent);

        var parametersSetAsyncEvent = logStream
            .Where(le => le == (expectedId, NestedAsyncComponent.EventType.OnParametersSetAsyncAsync) ||
                   le == (expectedId, NestedAsyncComponent.EventType.OnParametersSetAsyncSync))
            .ToArray();
        // OnParametersSetAsync async gets called at least once
        Assert.NotEmpty(parametersSetAsyncEvent);

        // The same number of OnParametersSet and OnParametersSetAsync get produced
        Assert.Equal(parametersSetEvent.Length, parametersSetAsyncEvent.Length);

        // The log ends with an OnParametersSetAsync event
        Assert.True(logStream.Last() == (expectedId, NestedAsyncComponent.EventType.OnParametersSetAsyncSync) ||
            logStream.Last() == (expectedId, NestedAsyncComponent.EventType.OnParametersSetAsyncAsync));
    }

    private Func<NestedAsyncComponent, RenderFragment> CreateRenderFactory(int[] childrenToRender)
    {
        // For some reason nameof doesn't work inside a nested lambda, so capturing the value here.
        var eventActionsName = nameof(NestedAsyncComponent.EventActions);
        var whatToRenderName = nameof(NestedAsyncComponent.WhatToRender);
        var testIdName = nameof(NestedAsyncComponent.TestId);
        var logName = nameof(NestedAsyncComponent.Log);

        return component => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddContent(1, $"Id: {component.TestId} BuildRenderTree, {Guid.NewGuid()}");
            foreach (var child in childrenToRender)
            {
                builder.OpenComponent<NestedAsyncComponent>(2);
                builder.AddComponentParameter(3, eventActionsName, component.EventActions);
                builder.AddComponentParameter(4, whatToRenderName, component.WhatToRender);
                builder.AddComponentParameter(5, testIdName, child);
                builder.AddComponentParameter(6, logName, component.Log);
                builder.CloseComponent();
            }

            builder.CloseElement();
        };
    }

    private class NestedAsyncComponent : ComponentBase
    {
        [Parameter] public IDictionary<int, IList<ExecutionAction>> EventActions { get; set; }

        [Parameter] public IDictionary<int, Func<NestedAsyncComponent, RenderFragment>> WhatToRender { get; set; }

        [Parameter] public int TestId { get; set; }

        [Parameter] public ConcurrentQueue<(int testId, EventType @event)> Log { get; set; }

        protected override void OnInitialized()
        {
            if (TryGetEntry(EventType.OnInit, out var entry))
            {
                var result = entry.EventAction();
                Assert.True(result.IsCompleted, "Task must complete synchronously.");
                LogResult(result.Result);
            }
        }

        protected override async Task OnInitializedAsync()
        {
            if (TryGetEntry(EventType.OnInitAsyncSync, out var entrySync))
            {
                var result = entrySync.EventAction();
                Assert.True(result.IsCompleted, "Task must complete synchronously.");
                LogResult(result.Result);
            }
            else if (TryGetEntry(EventType.OnInitAsyncAsync, out var entryAsync))
            {
                var result = await entryAsync.EventAction();
                LogResult(result);
            }
        }

        protected override void OnParametersSet()
        {
            if (TryGetEntry(EventType.OnParametersSet, out var entry))
            {
                var result = entry.EventAction();
                Assert.True(result.IsCompleted, "Task must complete synchronously.");
                LogResult(result.Result);
            }
            base.OnParametersSet();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (TryGetEntry(EventType.OnParametersSetAsyncSync, out var entrySync))
            {
                var result = entrySync.EventAction();
                Assert.True(result.IsCompleted, "Task must complete synchronously.");
                LogResult(result.Result);
            }
            else if (TryGetEntry(EventType.OnParametersSetAsyncAsync, out var entryAsync))
            {
                var result = await entryAsync.EventAction();
                LogResult(result);
            }
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var renderFactory = WhatToRender[TestId];
            renderFactory(this)(builder);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (TryGetEntry(EventType.OnAfterRenderAsyncSync, out var entrySync))
            {
                var result = entrySync.EventAction();
                Assert.True(result.IsCompleted, "Task must complete synchronously.");
                LogResult(result.Result);
            }
            if (TryGetEntry(EventType.OnAfterRenderAsyncAsync, out var entryAsync))
            {
                var result = await entryAsync.EventAction();
                LogResult(result);
            }
        }

        private bool TryGetEntry(EventType eventType, out ExecutionAction entry)
        {
            var entries = EventActions[TestId];
            if (entries == null)
            {
                throw new InvalidOperationException("Failed to find entries for component with Id: " + TestId);
            }
            entry = entries.FirstOrDefault(e => e.Event == eventType);
            return entry != null;
        }

        private void LogResult((int, EventType) entry)
        {
            Log?.Enqueue(entry);
        }

        public class ExecutionAction
        {
            public EventType Event { get; set; }
            public Func<Task<(int id, EventType @event)>> EventAction { get; set; }

            public static ExecutionAction On(int id, EventType @event, bool async = false)
            {
                if (!async)
                {
                    return new ExecutionAction
                    {
                        Event = @event,
                        EventAction = () => Task.FromResult((id, @event))
                    };
                }
                else
                {
                    return new ExecutionAction
                    {
                        Event = @event,
                        EventAction = async () =>
                        {
                            await Task.Yield();
                            return (id, @event);
                        }
                    };
                }
            }
        }

        public enum EventType
        {
            OnInit,
            OnInitAsyncSync,
            OnInitAsyncAsync,
            OnParametersSet,
            OnParametersSetAsyncSync,
            OnParametersSetAsyncAsync,
            OnAfterRenderAsyncSync,
            OnAfterRenderAsyncAsync,
        }

        public Task ExternalExceptionDispatch(Exception exception)
        {
            var tcs = new TaskCompletionSource();
            Task.Run(async () =>
            {
                // Inside Task.Run, we're outside the call stack or task chain of the lifecycle method, so
                // DispatchExceptionAsync is needed to get an exception back into the component
                await DispatchExceptionAsync(exception);
                tcs.SetResult();
            });

            return tcs.Task;
        }
    }

    private class ComponentThatAwaitsTask : ComponentBase
    {
        [Parameter] public Task TaskToAwait { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            await TaskToAwait;
        }
    }

    private class AsyncUpdateTestRenderer : TestRenderer
    {
        public Func<RenderBatch, Task> OnUpdateDisplayAsync { get; set; }

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
        {
            return OnUpdateDisplayAsync(renderBatch);
        }
    }

    private class AsyncAfterRenderComponent : AutoRenderComponent, IHandleAfterRender
    {
        private readonly Task _task;

        public AsyncAfterRenderComponent(Task task)
        {
            _task = task;
        }

        public bool Called { get; private set; }

        public Action OnAfterRenderComplete { get; set; }

        public async Task OnAfterRenderAsync()
        {
            await _task;
            Called = true;

            OnAfterRenderComplete?.Invoke();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "p");
            builder.CloseElement();
        }
    }

    class BoundPropertyComponent : AutoRenderComponent
    {
        public string BoundString { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var unrelatedThingToMakeTheLambdaCapture = new object();

            builder.OpenElement(0, "element with event");
            builder.AddAttribute(1, nameof(BoundString), BoundString);
            builder.AddAttribute(2, "ontestevent", new Action<ChangeEventArgs>((ChangeEventArgs eventArgs) =>
            {
                BoundString = (string)eventArgs.Value;
                TriggerRender();
                GC.KeepAlive(unrelatedThingToMakeTheLambdaCapture);
            }));
            builder.SetUpdatesAttributeName(nameof(BoundString));
            builder.CloseElement();
        }
    }

    private class DerivedEventArgs : EventArgs
    {
    }

    class CallbackOnRenderComponent : AutoRenderComponent
    {
        private readonly Action _callback;

        public CallbackOnRenderComponent(Action callback)
        {
            _callback = callback;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
            => _callback();
    }

    class InvalidRecursiveRenderer : TestRenderer
    {
        public new void ProcessPendingRender()
            => base.ProcessPendingRender();
    }

    class ParameterViewIllegalCapturingComponent : IComponent
    {
        public ParameterView CapturedParameterView { get; private set; }

        [Parameter] public int SomeParam { get; set; }

        public void Attach(RenderHandle renderHandle)
        {
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            CapturedParameterView = parameters;

            // Return a task that never completes to show that access is forbidden
            // after the synchronous return, not just after the returned task completes
            return new TaskCompletionSource().Task;
        }
    }

    private class TestErrorBoundary : AutoRenderComponent, IErrorBoundary
    {
        private readonly TaskCompletionSource receivedErrorTaskCompletionSource = new();

        public Exception ReceivedException { get; private set; }
        public Task ReceivedErrorTask => receivedErrorTaskCompletionSource.Task;

        [Parameter] public RenderFragment ChildContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
            => ChildContent(builder);

        public void HandleException(Exception error)
        {
            ReceivedException = error;
            receivedErrorTaskCompletionSource.SetResult();
        }

        public static void RenderNestedErrorBoundaries(RenderTreeBuilder builder, RenderFragment innerContent)
        {
            // Create an error boundary
            builder.OpenComponent<TestErrorBoundary>(0);
            builder.AddComponentParameter(1, nameof(TestErrorBoundary.ChildContent), (RenderFragment)(builder =>
            {
                // ... containing another error boundary, containing the content
                builder.OpenComponent<TestErrorBoundary>(0);
                builder.AddComponentParameter(1, nameof(TestErrorBoundary.ChildContent), innerContent);
                builder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    private class ErrorThrowingComponent : AutoRenderComponent, IHandleEvent
    {
        [Parameter] public Exception ThrowDuringRender { get; set; }
        [Parameter] public Exception ThrowDuringEventSync { get; set; }
        [Parameter] public Task ThrowDuringEventAsync { get; set; }
        [Parameter] public Exception ThrowDuringParameterSettingSync { get; set; }
        [Parameter] public Task ThrowDuringParameterSettingAsync { get; set; }

        public override async Task SetParametersAsync(ParameterView parameters)
        {
            _ = base.SetParametersAsync(parameters);

            if (ThrowDuringParameterSettingSync is not null)
            {
                throw ThrowDuringParameterSettingSync;
            }

            if (ThrowDuringParameterSettingAsync is not null)
            {
                await ThrowDuringParameterSettingAsync;
            }
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (ThrowDuringRender is not null)
            {
                throw ThrowDuringRender;
            }

            builder.OpenElement(0, "someelem");
            builder.AddAttribute(1, "onmakeerror", EventCallback.Factory.Create(this, () => { }));
            builder.AddContent(1, "Hello");
            builder.CloseElement();
        }

        public async Task HandleEventAsync(EventCallbackWorkItem item, object arg)
        {
            if (ThrowDuringEventSync is not null)
            {
                throw ThrowDuringEventSync;
            }

            if (ThrowDuringEventAsync is not null)
            {
                await ThrowDuringEventAsync;
            }
        }
    }

    private class CallbackDuringSetParametersAsyncComponent : AutoRenderComponent
    {
        public int RenderCount { get; private set; }
        public Func<Task> Callback { get; set; }

        public override async Task SetParametersAsync(ParameterView parameters)
        {
            await Callback();
            await base.SetParametersAsync(parameters);
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            RenderCount++;
        }
    }

    private sealed class ImplicitConversionComponent : IComponent
    {
        [Parameter]
        public ImplicitlyConvertsToString SomeParam { get; set; }

        public void Attach(RenderHandle renderHandle)
        {
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            foreach (var parameter in parameters)
            {
                if (parameter.Name.Equals(nameof(SomeParam), StringComparison.OrdinalIgnoreCase))
                {
                    // 'SomeParam' will be assigned to null if an implicit conversion changed the
                    // parameter type.
                    SomeParam = parameter.Value as ImplicitlyConvertsToString;
                }
            }

            return Task.CompletedTask;
        }
    }

    private sealed class ImplicitlyConvertsToString
    {
        private readonly string _value;

        public ImplicitlyConvertsToString(string value)
        {
            _value = value;
        }

        public static implicit operator string(ImplicitlyConvertsToString value) => value._value;
    }
}
