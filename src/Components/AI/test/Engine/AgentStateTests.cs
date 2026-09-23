// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI.Tests.Engine;

public class AgentStateTests
{
    [Fact]
    public void Value_DefaultsToNewState()
    {
        var state = new AgentState<TestState>();

        Assert.NotNull(state.Value);
        Assert.Equal("", state.Value.Name);
        Assert.Equal(0, state.Value.Count);
    }

    [Fact]
    public void Value_UsesInitialState()
    {
        var initial = new TestState { Name = "initial", Count = 5 };
        var state = new AgentState<TestState>(initial);

        Assert.Same(initial, state.Value);
    }

    [Fact]
    public void Value_NotifiesEverySubscriber()
    {
        var state = new AgentState<TestState>();
        var callbackCount = 0;
        state.OnChanged(() => callbackCount++);
        state.OnChanged(() => callbackCount++);

        state.Value = new TestState { Name = "updated" };

        Assert.Equal(2, callbackCount);
    }

    [Fact]
    public void OnChanged_DisposedRegistrationStopsNotifications()
    {
        var state = new AgentState<TestState>();
        var callbackCount = 0;
        var registration = state.OnChanged(() => callbackCount++);

        state.Value = new TestState();
        registration.Dispose();
        state.Value = new TestState();

        Assert.Equal(1, callbackCount);
    }

    [Fact]
    public void OnChanged_CanDisposeRegistrationDuringNotification()
    {
        var state = new AgentState<TestState>();
        IDisposable? registration = null;
        var callbackCount = 0;
        registration = state.OnChanged(() =>
        {
            callbackCount++;
            registration!.Dispose();
        });

        state.Value = new TestState();
        state.Value = new TestState();

        Assert.Equal(1, callbackCount);
    }

    private sealed class TestState
    {
        public string Name { get; set; } = "";

        public int Count { get; set; }
    }
}
