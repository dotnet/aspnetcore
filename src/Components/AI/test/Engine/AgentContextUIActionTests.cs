// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Engine;

public class AgentContextUIActionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UIAction_MultipleCallsContinueAcrossUpdateBoundaries(bool splitAcrossUpdates)
    {
        var firstActionInvocations = 0;
        var secondActionInvocations = 0;
        var clientCallCount = 0;
        List<ChatMessage>? continuationMessages = null;
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((messages, options, cancellationToken) =>
        {
            clientCallCount++;
            if (clientCallCount == 1)
            {
                return EmitTwoUIActionCalls(splitAcrossUpdates, cancellationToken);
            }

            continuationMessages = messages.ToList();
            return ResponseEmitters.EmitTextResponse("Both values received.", cancellationToken);
        });

        using var agent = new UIAgent(client, options =>
        {
            options.RegisterUIAction(AIFunctionFactory.Create(
                () =>
                {
                    firstActionInvocations++;
                    return "client-value-1";
                },
                "get_client_value_1",
                "Gets the first value from the client."));
            options.RegisterUIAction(AIFunctionFactory.Create(
                () =>
                {
                    secondActionInvocations++;
                    return "client-value-2";
                },
                "get_client_value_2",
                "Gets the second value from the client."));
        });
        using var context = new AgentContext(agent);
        using var subscription = context.RegisterOnStatusChanged(status =>
        {
            if (status == ConversationStatus.AwaitingInput)
            {
                foreach (var action in context.Turns[^1].ResponseBlocks.OfType<UIActionBlock>())
                {
                    _ = action.InvokeAsync();
                }
            }
        });

        await context.SendMessageAsync("Get both values");

        Assert.Equal(1, firstActionInvocations);
        Assert.Equal(1, secondActionInvocations);
        Assert.Equal(2, clientCallCount);

        var toolMessage = Assert.IsType<ChatMessage>(
            continuationMessages?.LastOrDefault(message => message.Role == ChatRole.Tool));
        var results = toolMessage.Contents.OfType<FunctionResultContent>().ToArray();
        Assert.Collection(
            results,
            result =>
            {
                Assert.Equal("call-1", result.CallId);
                Assert.Equal("client-value-1", result.Result?.ToString());
            },
            result =>
            {
                Assert.Equal("call-2", result.CallId);
                Assert.Equal("client-value-2", result.Result?.ToString());
            });

        var turn = Assert.Single(context.Turns);
        Assert.Collection(
            turn.ResponseBlocks.OfType<UIActionBlock>(),
            block => Assert.Equal("call-1", block.Call.CallId),
            block => Assert.Equal("call-2", block.Call.CallId));
        Assert.Equal(
            "Both values received.",
            Assert.Single(turn.ResponseBlocks.OfType<RichContentBlock>()).RawText);
    }

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
    public async Task UIAction_SendMessageAsyncWhileAwaitingInputThrows()
    {
        var clientCallCount = 0;
        var awaitingInputReached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        UIActionBlock? pendingAction = null;
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((messages, options, cancellationToken) =>
        {
            clientCallCount++;
            return clientCallCount == 1
                ? EmitUIActionCall("call-1", "get_client_value", cancellationToken)
                : ResponseEmitters.EmitTextResponse("Done", cancellationToken);
        });

        using var agent = new UIAgent(client, options =>
        {
            options.RegisterUIAction(AIFunctionFactory.Create(
                () => "client-value",
                "get_client_value",
                "Gets a value from the client."));
        });
        using var context = new AgentContext(agent);
        using var subscription = context.RegisterOnStatusChanged(status =>
        {
            if (status == ConversationStatus.AwaitingInput)
            {
                pendingAction = context.Turns[^1].ResponseBlocks.OfType<UIActionBlock>().Single();
                awaitingInputReached.TrySetResult();
            }
        });

        var firstSendTask = context.SendMessageAsync("First");
        await awaitingInputReached.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SendMessageAsync("Second"));

        await pendingAction!.InvokeAsync();
        await firstSendTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(2, clientCallCount);
        Assert.Single(context.Turns);
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
    public async Task UIAction_InformationalCallIsIgnored()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((messages, options, cancellationToken) =>
            EmitUIActionCall(
                new FunctionCallContent("call-1", "change_background")
                {
                    InformationalOnly = true
                },
                cancellationToken));

        using var agent = new UIAgent(client, options =>
        {
            options.RegisterUIAction(AIFunctionFactory.Create(
                () => "changed",
                "change_background",
                "Changes the background."));
        });
        using var context = new AgentContext(agent);

        await context.SendMessageAsync("Change it");

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

    private static IAsyncEnumerable<ChatResponseUpdate> EmitUIActionCall(
        string callId,
        string name,
        CancellationToken cancellationToken)
        => EmitUIActionCall(new FunctionCallContent(callId, name), cancellationToken);

    private static async IAsyncEnumerable<ChatResponseUpdate> EmitUIActionCall(
        FunctionCallContent call,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = Guid.NewGuid().ToString("N"),
            Contents = [call],
            FinishReason = ChatFinishReason.ToolCalls,
        };
        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> EmitTwoUIActionCalls(
        bool splitAcrossUpdates,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var messageId = Guid.NewGuid().ToString("N");
        if (splitAcrossUpdates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                MessageId = messageId,
                Contents = [new FunctionCallContent("call-1", "get_client_value_1")],
            };

            cancellationToken.ThrowIfCancellationRequested();
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                MessageId = messageId,
                Contents = [new FunctionCallContent("call-2", "get_client_value_2")],
                FinishReason = ChatFinishReason.ToolCalls,
            };
            await Task.CompletedTask;
            yield break;
        }

        cancellationToken.ThrowIfCancellationRequested();
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = messageId,
            Contents =
            [
                new FunctionCallContent("call-1", "get_client_value_1"),
                new FunctionCallContent("call-2", "get_client_value_2"),
            ],
            FinishReason = ChatFinishReason.ToolCalls,
        };
        await Task.CompletedTask;
    }
}
