// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI.Tests.TestFramework;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Components;

public class AgentFormBoundaryTests
{
    private static TestRenderer CreateRenderer()
    {
        var serviceProvider = new TestServiceProvider();
        serviceProvider.AddService<AntiforgeryStateProvider>(new NullAntiforgeryStateProvider());
        serviceProvider.AddService<IServiceProvider>(serviceProvider);
        return new TestRenderer(serviceProvider);
    }

    [Fact]
    public void RendersFormElement()
    {
        var client = new DelegatingStreamingChatClient();
        var agent = new UIAgent(client);

        var renderer = CreateRenderer();
        var cut = renderer.RenderComponent<AgentFormBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(b =>
            {
                b.OpenElement(0, "span");
                b.AddContent(1, "child");
                b.CloseElement();
            });
        });

        var html = cut.GetHtml();
        Assert.Contains("<form", html);
        Assert.Contains("method=\"post\"", html);
        Assert.Contains("data-enhance", html);
        Assert.Contains("child", html);
    }

    [Fact]
    public void RendersThreadIdHiddenField_WhenThreadConfigured()
    {
        var client = new DelegatingStreamingChatClient();
        var thread = new InMemoryConversationThread("my-thread-123");
        var agent = new UIAgent(client, options => { options.Thread = thread; });

        var renderer = CreateRenderer();
        var cut = renderer.RenderComponent<AgentFormBoundary>(p =>
        {
            p["Agent"] = agent;
        });

        var html = cut.GetHtml();
        Assert.Contains("name=\"ThreadId\"", html);
        Assert.Contains("value=\"my-thread-123\"", html);
    }

    [Fact]
    public void DoesNotRenderThreadIdHiddenField_WhenNoThread()
    {
        var client = new DelegatingStreamingChatClient();
        var agent = new UIAgent(client);

        var renderer = CreateRenderer();
        var cut = renderer.RenderComponent<AgentFormBoundary>(p =>
        {
            p["Agent"] = agent;
        });

        var html = cut.GetHtml();
        Assert.DoesNotContain("name=\"ThreadId\"", html);
    }

    [Fact]
    public void CascadesAgentContext()
    {
        var client = new DelegatingStreamingChatClient();
        var agent = new UIAgent(client);

        var renderer = CreateRenderer();
        var cut = renderer.RenderComponent<AgentFormBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(b =>
            {
                b.OpenComponent<ContextCapture>(0);
                b.CloseComponent();
            });
        });

        var captureComponent = cut.FindComponent<ContextCapture>();
        Assert.NotNull(captureComponent.Instance.CapturedContext);
    }

    [Fact]
    public async Task RestoresConversation_WhenThreadHasUpdates()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Hello!", ct));

        // Build a thread with existing updates
        var thread = new InMemoryConversationThread("restore-thread");
        var previousAgent = new UIAgent(client, options => { options.Thread = thread; });
        var previousContext = new AgentContext(previousAgent);
        await previousContext.SendMessageAsync("Hi");
        previousContext.Dispose();
        previousAgent.Dispose();

        // Now render AgentFormBoundary with the same thread (simulating page reload)
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Second response", ct));
        var agent = new UIAgent(client, options => { options.Thread = thread; });

        var renderer = CreateRenderer();
        var cut = renderer.RenderComponent<AgentFormBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(b =>
            {
                b.OpenComponent<MessageList>(0);
                b.CloseComponent();
            });
        });

        var html = cut.GetHtml();
        // Restored conversation should show the previous turn
        Assert.Contains("Hi", html);
        Assert.Contains("Hello!", html);
    }

    [Fact]
    public void Dispose_DisposesContext()
    {
        var client = new DelegatingStreamingChatClient();
        var agent = new UIAgent(client);

        var renderer = CreateRenderer();
        var cut = renderer.RenderComponent<AgentFormBoundary>(p =>
        {
            p["Agent"] = agent;
        });

        var context = GetAgentContext(cut);
        var boundary = cut.Instance;
        ((IDisposable)boundary).Dispose();

        // Dispose should be idempotent — calling it again does not throw
        ((IDisposable)boundary).Dispose();
    }

    private static AgentContext GetAgentContext(RenderedComponent<AgentFormBoundary> cut)
    {
        return (AgentContext)typeof(AgentFormBoundary)
            .GetField("_context",
                System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance)!
            .GetValue(cut.Instance)!;
    }

    internal class ContextCapture : IComponent
    {
        private RenderHandle _renderHandle;

        [CascadingParameter]
        public AgentContext? CapturedContext { get; set; }

        void IComponent.Attach(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        Task IComponent.SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);
            _renderHandle.Render(_ => { });
            return Task.CompletedTask;
        }
    }
}
