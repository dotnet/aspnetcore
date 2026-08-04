// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI.Tests.Engine;

public class AgentStateTests
{
    private class TestState
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }

    [Fact]
    public void Value_DefaultsToNewT()
    {
        var state = new AgentState<TestState>();
        Assert.NotNull(state.Value);
        Assert.Equal("", state.Value.Name);
        Assert.Equal(0, state.Value.Count);
    }

    [Fact]
    public void Value_UsesInitialValueWhenProvided()
    {
        var initial = new TestState { Name = "initial", Count = 5 };
        var state = new AgentState<TestState>(initial);
        Assert.Same(initial, state.Value);
    }

    [Fact]
    public void Value_Setter_InvokesOnChangedCallbacks()
    {
        var state = new AgentState<TestState>();
        var fired = false;
        state.OnChanged(() => fired = true);

        state.Value = new TestState { Name = "updated" };

        Assert.True(fired);
    }

    [Fact]
    public void OnChanged_MultipleCallbacksAllFire()
    {
        var state = new AgentState<TestState>();
        var count = 0;
        state.OnChanged(() => count++);
        state.OnChanged(() => count++);

        state.Value = new TestState();

        Assert.Equal(2, count);
    }

    [Fact]
    public void OnChanged_DisposingStopsCallbacks()
    {
        var state = new AgentState<TestState>();
        var count = 0;
        var reg = state.OnChanged(() => count++);

        state.Value = new TestState();
        Assert.Equal(1, count);

        reg.Dispose();
        state.Value = new TestState();
        Assert.Equal(1, count);
    }
}
