// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Components;

public class EventCallbackTest
{
    [Fact]
    public async Task EventCallback_Default()
    {
        // Arrange
        var callback = default(EventCallback);

        // Act & Assert (Does not throw)
        await callback.InvokeAsync();
    }

    [Fact]
    public async Task EventCallbackOfT_Default()
    {
        // Arrange
        var callback = default(EventCallback<EventArgs>);

        // Act & Assert (Does not throw)
        await callback.InvokeAsync();
    }

    [Fact]
    public async Task EventCallback_NullReceiver()
    {
        // Arrange
        int runCount = 0;
        var callback = new EventCallback(null, (Action)(() => runCount++));

        // Act
        await callback.InvokeAsync();

        // Assert
        Assert.Equal(1, runCount);
    }

    [Fact]
    public async Task EventCallbackOfT_NullReceiver()
    {
        // Arrange
        int runCount = 0;
        var callback = new EventCallback<EventArgs>(null, (Action)(() => runCount++));

        // Act
        await callback.InvokeAsync();

        // Assert
        Assert.Equal(1, runCount);
    }

    [Fact]
    public async Task EventCallback_Action_Null()
    {
        // Arrange
        var component = new EventCountingComponent();

        int runCount = 0;
        var callback = new EventCallback(component, (Action)(() => runCount++));

        // Act
        await callback.InvokeAsync();

        // Assert
        Assert.Equal(1, runCount);
        Assert.Equal(1, component.Count);
    }

    [Fact]
    public async Task EventCallback_Action_IgnoresArg()
    {
        // Arrange
        var component = new EventCountingComponent();

        int runCount = 0;
        var callback = new EventCallback(component, (Action)(() => runCount++));

        // Act
        await callback.InvokeAsync(new EventArgs());

        // Assert
        Assert.Equal(1, runCount);
        Assert.Equal(1, component.Count);
    }

    [Fact]
    public async Task EventCallback_ActionT_Null()
    {
        // Arrange
        var component = new EventCountingComponent();

        int runCount = 0;
        EventArgs arg = null;
        var callback = new EventCallback(component, (Action<EventArgs>)((e) => { arg = e; runCount++; }));

        // Act
        await callback.InvokeAsync();

        // Assert
        Assert.Null(arg);
        Assert.Equal(1, runCount);
        Assert.Equal(1, component.Count);
    }

    [Fact]
    public async Task EventCallback_ActionT_Arg()
    {
        // Arrange
        var component = new EventCountingComponent();

        int runCount = 0;
        EventArgs arg = null;
        var callback = new EventCallback(component, (Action<EventArgs>)((e) => { arg = e; runCount++; }));

        // Act
        await callback.InvokeAsync(new EventArgs());

        // Assert
        Assert.NotNull(arg);
        Assert.Equal(1, runCount);
        Assert.Equal(1, component.Count);
    }

    [Fact]
    public async Task EventCallback_ActionT_Arg_ValueType()
    {
        // Arrange
        var component = new EventCountingComponent();

        int runCount = 0;
        int arg = -1;
        var callback = new EventCallback(component, (Action<int>)((e) => { arg = e; runCount++; }));

        // Act
        await callback.InvokeAsync(17);

        // Assert
        Assert.Equal(17, arg);
        Assert.Equal(1, runCount);
        Assert.Equal(1, component.Count);
    }

    [Fact]
    public async Task EventCallback_ActionT_ArgMismatch()
    {
        // Arrange
        var component = new EventCountingComponent();

        int runCount = 0;
        EventArgs arg = null;
        var callback = new EventCallback(component, (Action<EventArgs>)((e) => { arg = e; runCount++; }));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            return callback.InvokeAsync(new StringBuilder());
        });
    }

    [Fact]
    public async Task EventCallback_FuncTask_Null()
    {
        // Arrange
        var component = new EventCountingComponent();

        int runCount = 0;
        var callback = new EventCallback(component, (Func<Task>)(() => { runCount++; return Task.CompletedTask; }));

        // Act
        await callback.InvokeAsync();

        // Assert
        Assert.Equal(1, runCount);
        Assert.Equal(1, component.Count);
    }

    [Fact]
    public async Task EventCallback_FuncTask_IgnoresArg()
    {
        // Arrange
        var component = new EventCountingComponent();

        int runCount = 0;
        var callback = new EventCallback(component, (Func<Task>)(() => { runCount++; return Task.CompletedTask; }));

        // Act
        await callback.InvokeAsync(new EventArgs());

        // Assert
        Assert.Equal(1, runCount);
        Assert.Equal(1, component.Count);
    }

    [Fact]
    public async Task EventCallback_FuncTTask_Null()
    {
        // Arrange
        var component = new EventCountingComponent();

        int runCount = 0;
        EventArgs arg = null;
        var callback = new EventCallback(component, (Func<EventArgs, Task>)((e) => { arg = e; runCount++; return Task.CompletedTask; }));

        // Act
        await callback.InvokeAsync();

        // Assert
        Assert.Null(arg);
        Assert.Equal(1, runCount);
        Assert.Equal(1, component.Count);
    }

    [Fact]
    public async Task EventCallback_FuncTTask_Arg()
    {
        // Arrange
        var component = new EventCountingComponent();

        int runCount = 0;
        EventArgs arg = null;
        var callback = new EventCallback(component, (Func<EventArgs, Task>)((e) => { arg = e; runCount++; return Task.CompletedTask; }));

        // Act
        await callback.InvokeAsync(new EventArgs());

        // Assert
        Assert.NotNull(arg);
        Assert.Equal(1, runCount);
        Assert.Equal(1, component.Count);
    }

    [Fact]
    public async Task EventCallback_FuncTTask_Arg_ValueType()
    {
        // Arrange
        var component = new EventCountingComponent();

        int runCount = 0;
        int arg = -1;
        var callback = new EventCallback(component, (Func<int, Task>)((e) => { arg = e; runCount++; return Task.CompletedTask; }));

        // Act
        await callback.InvokeAsync(17);

        // Assert
        Assert.Equal(17, arg);
        Assert.Equal(1, runCount);
        Assert.Equal(1, component.Count);
    }

    [Fact]
    public async Task EventCallback_FuncTTask_ArgMismatch()
    {
        // Arrange
        var component = new EventCountingComponent();

        int runCount = 0;
        EventArgs arg = null;
        var callback = new EventCallback(component, (Func<EventArgs, Task>)((e) => { arg = e; runCount++; return Task.CompletedTask; }));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            return callback.InvokeAsync(new StringBuilder());
        });
    }

    [Fact]
    public void EventCallbackOf_Equals_WhenANewDelegateIsCreated()
    {
        // Arrange
        var component = new EventCountingComponent();

        var delegate_1 = (EventArgs _) => { };
        var delegate_2 = (MulticastDelegate)MulticastDelegate.CreateDelegate(typeof(Action<EventArgs>), delegate_1.Target, delegate_1.Method);
        var eventcallback_1 = new EventCallback(component, delegate_1);
        var eventcallback_2 = new EventCallback(component, delegate_2);

        // Act
        var result = eventcallback_1.Equals(eventcallback_2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task EventCallbackOfT_Action_Null()
    {
        // Arrange
        var component = new EventCountingComponent();

        int runCount = 0;
        var callback = new EventCallback<EventArgs>(component, (Action)(() => runCount++));

        // Act
        await callback.InvokeAsync();

        // Assert
        Assert.Equal(1, runCount);
        Assert.Equal(1, component.Count);
    }

    [Fact]
    public async Task EventCallbackOfT_Action_IgnoresArg()
    {
        // Arrange
        var component = new EventCountingComponent();

        int runCount = 0;
        var callback = new EventCallback<EventArgs>(component, (Action)(() => runCount++));

        // Act
        await callback.InvokeAsync(new EventArgs());

        // Assert
        Assert.Equal(1, runCount);
        Assert.Equal(1, component.Count);
    }

    [Fact]
    public async Task EventCallbackOfT_ActionT_Null()
    {
        // Arrange
        var component = new EventCountingComponent();

        int runCount = 0;
        EventArgs arg = null;
        var callback = new EventCallback<EventArgs>(component, (Action<EventArgs>)((e) => { arg = e; runCount++; }));

        // Act
        await callback.InvokeAsync();

        // Assert
        Assert.Null(arg);
        Assert.Equal(1, runCount);
        Assert.Equal(1, component.Count);
    }

    [Fact]
    public async Task EventCallbackOfT_ActionT_Arg()
    {
        // Arrange
        var component = new EventCountingComponent();

        int runCount = 0;
        EventArgs arg = null;
        var callback = new EventCallback<EventArgs>(component, (Action<EventArgs>)((e) => { arg = e; runCount++; }));

        // Act
        await callback.InvokeAsync(new EventArgs());

        // Assert
        Assert.NotNull(arg);
        Assert.Equal(1, runCount);
        Assert.Equal(1, component.Count);
    }

    [Fact]
    public async Task EventCallbackOfT_FuncTask_Null()
    {
        // Arrange
        var component = new EventCountingComponent();

        int runCount = 0;
        var callback = new EventCallback<EventArgs>(component, (Func<Task>)(() => { runCount++; return Task.CompletedTask; }));

        // Act
        await callback.InvokeAsync();

        // Assert
        Assert.Equal(1, runCount);
        Assert.Equal(1, component.Count);
    }

    [Fact]
    public async Task EventCallbackOfT_FuncTask_IgnoresArg()
    {
        // Arrange
        var component = new EventCountingComponent();

        int runCount = 0;
        var callback = new EventCallback<EventArgs>(component, (Func<Task>)(() => { runCount++; return Task.CompletedTask; }));

        // Act
        await callback.InvokeAsync(new EventArgs());

        // Assert
        Assert.Equal(1, runCount);
        Assert.Equal(1, component.Count);
    }

    [Fact]
    public async Task EventCallbackOfT_FuncTTask_Null()
    {
        // Arrange
        var component = new EventCountingComponent();

        int runCount = 0;
        EventArgs arg = null;
        var callback = new EventCallback<EventArgs>(component, (Func<EventArgs, Task>)((e) => { arg = e; runCount++; return Task.CompletedTask; }));

        // Act
        await callback.InvokeAsync();

        // Assert
        Assert.Null(arg);
        Assert.Equal(1, runCount);
        Assert.Equal(1, component.Count);
    }

    [Fact]
    public async Task EventCallbackOfT_FuncTTask_Arg()
    {
        // Arrange
        var component = new EventCountingComponent();

        int runCount = 0;
        EventArgs arg = null;
        var callback = new EventCallback<EventArgs>(component, (Func<EventArgs, Task>)((e) => { arg = e; runCount++; return Task.CompletedTask; }));

        // Act
        await callback.InvokeAsync(new EventArgs());

        // Assert
        Assert.NotNull(arg);
        Assert.Equal(1, runCount);
        Assert.Equal(1, component.Count);
    }

    [Fact]
    public void EventCallbackOfT_Equals_WhenANewDelegateIsCreated()
    {
        // Arrange
        var component = new EventCountingComponent();

        var delegate_1 = (EventArgs _) => { };
        var delegate_2 = (MulticastDelegate)MulticastDelegate.CreateDelegate(typeof(Action<EventArgs>), delegate_1.Target, delegate_1.Method);
        var eventcallback_1 = new EventCallback<EventArgs>(component, delegate_1);
        var eventcallback_2 = new EventCallback<EventArgs>(component, delegate_2);

        // Act
        var result = eventcallback_1.Equals(eventcallback_2);

        // Assert
        Assert.True(result);
    }

    private class EventCountingComponent : IComponent, IHandleEvent
    {
        public int Count;

        public Task HandleEventAsync(EventCallbackWorkItem item, object arg)
        {
            Count++;
            return item.InvokeAsync(arg);
        }

        public void Attach(RenderHandle renderHandle) => throw new NotImplementedException();

        public Task SetParametersAsync(ParameterView parameters) => throw new NotImplementedException();
    }
}
