// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Samples;

public class S03_FrontendToolsTest
{
    // AG-UI "frontend tools" map to UIActions in components-ai.
    // The LLM emits a FunctionCallContent for a client-side tool;
    // the engine parks at AwaitingInput and the UI invokes the action.
    // This differs from backend tools which auto-execute.

    [Fact]
    public async Task FrontendTool_SurfacesAsUIActionBlock()
    {
        var showChart = AIFunctionFactory.Create(
            (string data) => $"chart-rendered:{data}",
            "ShowChart", "Renders a chart on the client");

        var client = new DelegatingStreamingChatClient();
        var agent = new UIAgent(client, options =>
        {
            options.RegisterUIAction(showChart);
        });

        var callCount = 0;
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return EmitFrontendToolCall("call-1", "ShowChart", ct,
                    new Dictionary<string, object?> { ["data"] = "sales-q4" });
            }
            return ResponseEmitters.EmitTextResponse("Chart displayed.", ct);
        });

        var context = new AgentContext(agent);
        context.RegisterOnStatusChanged(s =>
        {
            if (s == ConversationStatus.AwaitingInput)
            {
                context.Turns[^1].ResponseBlocks.OfType<UIActionBlock>().Single()
                    .InvokeAsync().GetAwaiter().GetResult();
            }
        });

        await context.SendMessageAsync("Show me Q4 sales chart");

        var turn = context.Turns[0];
        var uiBlock = turn.ResponseBlocks.OfType<UIActionBlock>().Single();
        Assert.Equal("ShowChart", uiBlock.ToolName);
        Assert.True(uiBlock.IsComplete);
    }

    [Fact]
    public async Task FrontendTool_ArgumentsPreserved()
    {
        var navigateTo = AIFunctionFactory.Create(
            (string url, bool newTab) => $"navigated to {url}",
            "NavigateTo", "Navigates client browser");

        var client = new DelegatingStreamingChatClient();
        var agent = new UIAgent(client, options =>
        {
            options.RegisterUIAction(navigateTo);
        });

        var callCount = 0;
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return EmitFrontendToolCall("call-2", "NavigateTo", ct,
                    new Dictionary<string, object?>
                    {
                        ["url"] = "/dashboard",
                        ["newTab"] = true
                    });
            }
            return ResponseEmitters.EmitTextResponse("Navigated.", ct);
        });

        var context = new AgentContext(agent);
        UIActionBlock? captured = null;
        context.RegisterOnStatusChanged(s =>
        {
            if (s == ConversationStatus.AwaitingInput)
            {
                captured = context.Turns[^1].ResponseBlocks.OfType<UIActionBlock>().Single();
                captured.InvokeAsync().GetAwaiter().GetResult();
            }
        });

        await context.SendMessageAsync("Go to dashboard");

        Assert.NotNull(captured);
        Assert.Equal("NavigateTo", captured!.ToolName);
        Assert.Equal("call-2", captured.Id);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> EmitFrontendToolCall(
        string callId,
        string name,
        [EnumeratorCancellation] CancellationToken ct,
        IDictionary<string, object?>? arguments = null)
    {
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = Guid.NewGuid().ToString("N"),
            Contents = [new FunctionCallContent(callId, name, arguments)],
            FinishReason = ChatFinishReason.ToolCalls
        };
        await Task.CompletedTask;
    }
}
