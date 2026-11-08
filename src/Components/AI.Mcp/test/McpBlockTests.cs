// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using ModelContextProtocol.Client;
using Moq;

namespace Microsoft.AspNetCore.Components.AI.Mcp.Tests;

public class McpBlockTests
{
    [Fact]
    public async Task StaticRenderingDoesNotInstantiateHostOrImportModule()
    {
        var server = new McpClientFixture("server");
        await using var client = await McpClient.CreateAsync(server);
        var browser = new Mock<IJSRuntime>(MockBehavior.Strict);
        var state = new HostState();
        await using var services = new ServiceCollection().AddLogging()
            .AddSingleton(browser.Object).AddSingleton(state).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var parameters = Parameters(client, (await client.ListToolsAsync()).ToArray());

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
            (await renderer.RenderComponentAsync<McpBlock<Host>>(parameters)).ToHtmlString());

        Assert.Equal("", html);
        Assert.Equal(0, state.Renders);
        browser.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ImportCompletesBeforeHostRendersAndDisposalCannotResurrectIt(bool disposeDuringImport)
    {
        var server = new McpClientFixture("server");
        await using var client = await McpClient.CreateAsync(server);
        var import = new TaskCompletionSource<IJSObjectReference>(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var browser = new Mock<IJSRuntime>();
        browser.Setup(runtime => runtime.InvokeAsync<IJSObjectReference>(
            "import", It.IsAny<CancellationToken>(), It.Is<object?[]>(args => (string)args[0]! == McpContentHost.ModulePath)))
            .Returns(() =>
            {
                started.TrySetResult();
                return new(import.Task);
            });
        var module = new Mock<IJSObjectReference>();
        module.Setup(reference => reference.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var state = new HostState();
        await using var services = new ServiceCollection().AddLogging()
            .AddSingleton(browser.Object).AddSingleton(state).BuildServiceProvider();
        await using var renderer = new InteractiveRenderer(services);
        var parameters = Parameters(client, (await client.ListToolsAsync()).ToArray());
        var block = await renderer.Dispatcher.InvokeAsync(() => renderer.AddBlock());
        await renderer.Dispatcher.InvokeAsync(() => renderer.RenderAsync(parameters));
        await started.Task;
        Assert.Equal(0, state.Renders);
        Task? closing = disposeDuringImport
            ? renderer.Dispatcher.InvokeAsync(() => block.DisposeAsync().AsTask()) : null;

        import.SetResult(module.Object);
        if (closing is not null)
        {
            await closing;
            Assert.Equal(0, state.Renders);
        }
        else
        {
            await state.Rendered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(1, state.Renders);
            await renderer.Dispatcher.InvokeAsync(() => block.DisposeAsync().AsTask());
        }

        module.Verify(reference => reference.DisposeAsync(), Times.Once);
        browser.Verify(runtime => runtime.InvokeAsync<IJSObjectReference>(
            "import", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()), Times.Once);
    }

    private static ParameterView Parameters(McpClient client, IReadOnlyList<McpClientTool> tools) =>
        ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(McpBlock<Host>.Client)] = client,
            [nameof(McpBlock<Host>.Tools)] = tools,
            [nameof(McpBlock<Host>.Block)] = new FunctionInvocationContentBlock
            {
                Call = new("call", "open", new Dictionary<string, object?>())
            }
        });

    private sealed class HostState
    {
        public int Renders;
        public TaskCompletionSource Rendered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class Host : McpContentHost
    {
        [Inject] private HostState State { get; set; } = default!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            State.Renders++;
            builder.OpenElement(0, "iframe");
            builder.CloseElement();
            State.Rendered.TrySetResult();
        }

        protected override Task OnAfterRenderAsync(bool firstRender) => Task.CompletedTask;
    }

    private sealed class InteractiveRenderer(IServiceProvider services)
        : Renderer(services, services.GetRequiredService<ILoggerFactory>())
    {
        private int _root;
        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        public McpBlock<Host> AddBlock()
        {
            var block = (McpBlock<Host>)InstantiateComponent(typeof(McpBlock<Host>));
            _root = AssignRootComponentId(block);
            return block;
        }

        public Task RenderAsync(ParameterView parameters) => RenderRootComponentAsync(_root, parameters);
        protected override void HandleException(Exception exception) => throw new InvalidOperationException("Rendering failed.", exception);
        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
    }
}
