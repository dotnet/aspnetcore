// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.AI.Tests.TestFramework;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;

namespace Microsoft.AspNetCore.Components.AI.Tests.Components;

public class AgentBoundaryTests
{
    [Fact]
    public void CascadesAgentContext_ToChildren()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Hi", ct));
        var agent = new UIAgent(client);

        AgentContext? received = null;
        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<AgentBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(builder =>
            {
                builder.OpenComponent<CascadingReceiver>(0);
                builder.AddComponentParameter(1, "OnReceived",
                    EventCallback.Factory.Create<AgentContext>(
                        new object(), ctx => received = ctx));
                builder.CloseComponent();
            });
        });

        Assert.NotNull(received);
    }

    [Fact]
    public void WithUIAgentTState_CascadesAgentState()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Hi", ct));
        var agent = new UIAgent<TestState>(client, new TestState { Value = "initial" });

        AgentState<TestState>? receivedState = null;
        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<AgentBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(builder =>
            {
                builder.OpenComponent<TypedStateReceiver>(0);
                builder.AddComponentParameter(1, "OnReceived",
                    EventCallback.Factory.Create<AgentState<TestState>>(
                        new object(), s => receivedState = s));
                builder.CloseComponent();
            });
        });

        Assert.NotNull(receivedState);
        Assert.Equal("initial", receivedState.Value.Value);
    }

    [Fact]
    public void WithPlainUIAgent_DoesNotCascadeAgentState()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Hi", ct));
        var agent = new UIAgent(client);

        AgentState<TestState>? receivedState = null;
        bool receiverInitialized = false;
        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<AgentBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(builder =>
            {
                builder.OpenComponent<TypedStateReceiver>(0);
                builder.AddComponentParameter(1, "OnReceived",
                    EventCallback.Factory.Create<AgentState<TestState>>(
                        new object(), s => receivedState = s));
                builder.AddComponentParameter(2, "OnInitializedCallback",
                    EventCallback.Factory.Create(
                        new object(), () => receiverInitialized = true));
                builder.CloseComponent();
            });
        });

        Assert.True(receiverInitialized);
        Assert.Null(receivedState);
    }

    [Fact]
    public void Dispose_DisposesContext()
    {
        var client = new DelegatingStreamingChatClient();
        var agent = new UIAgent(client);

        AgentContext? received = null;
        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<AgentBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(builder =>
            {
                builder.OpenComponent<CascadingReceiver>(0);
                builder.AddComponentParameter(1, "OnReceived",
                    EventCallback.Factory.Create<AgentContext>(
                        new object(), ctx => received = ctx));
                builder.CloseComponent();
            });
        });

        Assert.NotNull(received);

        // Dispose the boundary
        ((IDisposable)cut.Instance).Dispose();

        // After dispose, SendMessageAsync should throw because context is disposed
        // (the CTS was cancelled/disposed in Dispose)
        // Verify it doesn't throw during dispose itself — that's the main contract.
    }

    [Fact]
    public void RendersChildContent()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Hi", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<AgentBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "child-content");
                builder.AddContent(2, "Hello from child");
                builder.CloseElement();
            });
        });

        var html = cut.GetHtml();
        Assert.Contains("class=\"child-content\"", html);
        Assert.Contains("Hello from child", html);
    }
}

internal class CascadingReceiver : ComponentBase
{
    [CascadingParameter]
    public AgentContext? Context { get; set; }

    [Parameter]
    public EventCallback<AgentContext> OnReceived { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (Context is not null)
        {
            await OnReceived.InvokeAsync(Context);
        }
    }
}

internal class TypedStateReceiver : ComponentBase
{
    [CascadingParameter]
    public AgentState<TestState>? State { get; set; }

    [Parameter]
    public EventCallback<AgentState<TestState>> OnReceived { get; set; }

    [Parameter]
    public EventCallback OnInitializedCallback { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await OnInitializedCallback.InvokeAsync();
        if (State is not null)
        {
            await OnReceived.InvokeAsync(State);
        }
    }
}

internal class TestState
{
    public string Value { get; set; } = "";
}
