// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Engine;

public class AgentContextUIActionTests
{
    [Fact]
    public async Task UIAction_ContinuesWithOneToolResult()
    {
        var invocationCount = 0;
        var clientCallCount = 0;
        List<ChatMessage>? continuationMessages = null;
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((messages, options, cancellationToken) =>
        {
            clientCallCount++;
            if (clientCallCount == 1)
            {
                return EmitUIActionCall("call-1", "get_client_value", cancellationToken);
            }

            continuationMessages = messages.ToList();
            return ResponseEmitters.EmitTextResponse("Client value received.", cancellationToken);
        });

        using var agent = new UIAgent(client, options =>
        {
            options.RegisterUIAction(AIFunctionFactory.Create(
                () =>
                {
                    invocationCount++;
                    return "client-value";
                },
                "get_client_value",
                "Gets a value from the client."));
        });
        using var context = new AgentContext(agent);
        var statuses = new List<ConversationStatus>();
        using var subscription = context.RegisterOnStatusChanged(status =>
        {
            statuses.Add(status);
            if (status == ConversationStatus.AwaitingInput)
            {
                _ = context.Turns[^1].ResponseBlocks
                    .OfType<UIActionBlock>()
                    .Single()
                    .InvokeAsync();
            }
        });

        await context.SendMessageAsync("Get the value");

        Assert.Equal(1, invocationCount);
        Assert.Equal(2, clientCallCount);
        Assert.Equal(
            [
                ConversationStatus.Streaming,
                ConversationStatus.AwaitingInput,
                ConversationStatus.Streaming,
                ConversationStatus.Idle
            ],
            statuses);

        var toolMessage = Assert.IsType<ChatMessage>(
            continuationMessages?.LastOrDefault(message => message.Role == ChatRole.Tool));
        var result = Assert.IsType<FunctionResultContent>(Assert.Single(toolMessage.Contents));
        Assert.Equal("call-1", result.CallId);
        Assert.Equal("client-value", result.Result?.ToString());

        var turn = Assert.Single(context.Turns);
        Assert.Single(turn.ResponseBlocks.OfType<UIActionBlock>());
        Assert.Equal(
            "Client value received.",
            Assert.Single(turn.ResponseBlocks.OfType<RichContentBlock>()).RawText);
    }

    [Fact]
    public async Task UIAction_IsAdvertisedAsDeclarationOnlyAndPreservesConfiguredTools()
    {
        ChatOptions? capturedOptions = null;
        var serverTool = AIFunctionFactory.Create(() => "server", "server_tool", "Server tool.");
        var clientTool = AIFunctionFactory.Create(() => "client", "client_tool", "Client tool.");
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((messages, options, cancellationToken) =>
        {
            capturedOptions = options;
            return ResponseEmitters.EmitTextResponse("Done", cancellationToken);
        });

        using var agent = new UIAgent(client, options =>
        {
            options.ChatOptions = new ChatOptions { Tools = [serverTool] };
            options.RegisterUIAction(clientTool);
        });
        using var context = new AgentContext(agent);

        await context.SendMessageAsync("Go");

        Assert.NotNull(capturedOptions);
        Assert.Collection(
            capturedOptions.Tools!,
            tool => Assert.Same(serverTool, tool),
            tool =>
            {
                Assert.IsAssignableFrom<AIFunctionDeclaration>(tool);
                Assert.IsNotAssignableFrom<AIFunction>(tool);
                Assert.Equal("client_tool", ((AIFunctionDeclaration)tool).Name);
            });
    }

    [Fact]
    public async Task UIAction_NameMatchingIsOrdinal()
    {
        var invoked = false;
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((messages, options, cancellationToken) =>
            EmitUIActionCall("call-1", "CHANGE_BACKGROUND", cancellationToken));

        using var agent = new UIAgent(client, options =>
        {
            options.RegisterUIAction(AIFunctionFactory.Create(
                () =>
                {
                    invoked = true;
                    return "changed";
                },
                "change_background",
                "Changes the background."));
        });
        using var context = new AgentContext(agent);

        await context.SendMessageAsync("Change it");

        Assert.False(invoked);
        Assert.Empty(Assert.Single(context.Turns).ResponseBlocks.OfType<UIActionBlock>());
    }

    [Fact]
    public async Task UIAction_InvokeAsyncExecutesOnlyOnce()
    {
        var invocationCount = 0;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var function = AIFunctionFactory.Create(
            async () =>
            {
                invocationCount++;
                await gate.Task;
                return "done";
            },
            "run_once",
            "Runs once.");
        var block = new UIActionBlock(
            function,
            new FunctionCallContent("call-1", "run_once"))
        {
            Id = "call-1"
        };

        var first = block.InvokeAsync();
        var second = block.InvokeAsync();
        gate.SetResult();
        await Task.WhenAll(first, second);

        Assert.Same(first, second);
        Assert.Equal(1, invocationCount);
        Assert.True(block.IsComplete);
        Assert.Equal("call-1", block.Result?.CallId);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> EmitUIActionCall(
        string callId,
        string name,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = Guid.NewGuid().ToString("N"),
            Contents = [new FunctionCallContent(callId, name)],
            FinishReason = ChatFinishReason.ToolCalls,
        };
        await Task.CompletedTask;
    }
}
