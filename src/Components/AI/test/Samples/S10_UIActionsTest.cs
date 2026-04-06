// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Samples;

public class S10_UIActionsTest
{
    // AG-UI "frontend actions" map to UIActionBlock in components-ai.
    // The LLM emits a FunctionCallContent for a registered UIAction.
    // The engine parks at AwaitingInput until the action is invoked.

    [Fact]
    public async Task UIAction_ParksAtAwaitingInput_ThenResumes()
    {
        var action = AIFunctionFactory.Create(
            () => "user-confirmed",
            "ConfirmOrder", "Asks the user to confirm the order");

        var client = new DelegatingStreamingChatClient();
        var agent = new UIAgent(client, options =>
        {
            options.RegisterUIAction(action);
        });

        var callCount = 0;
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return EmitUIActionCall("call-1", "ConfirmOrder", ct);
            }
            return ResponseEmitters.EmitTextResponse("Order confirmed! Processing now.", ct);
        });

        var context = new AgentContext(agent);
        var statuses = new List<ConversationStatus>();
        context.RegisterOnStatusChanged(s =>
        {
            statuses.Add(s);
            if (s == ConversationStatus.AwaitingInput)
            {
                var turn = context.Turns[^1];
                var block = turn.ResponseBlocks.OfType<UIActionBlock>().Single();
                block.InvokeAsync().GetAwaiter().GetResult();
            }
        });

        await context.SendMessageAsync("Place my order");

        Assert.Contains(ConversationStatus.AwaitingInput, statuses);
        Assert.Equal(ConversationStatus.Idle, context.Status);

        var turn = context.Turns[0];
        var uiBlock = turn.ResponseBlocks.OfType<UIActionBlock>().Single();
        Assert.Equal("ConfirmOrder", uiBlock.ToolName);
        Assert.True(uiBlock.IsComplete);
        Assert.True(uiBlock.HasResult);

        var textBlock = turn.ResponseBlocks.OfType<RichContentBlock>().Single();
        Assert.Equal("Order confirmed! Processing now.", textBlock.RawText);
    }

    [Fact]
    public async Task UIAction_WithArguments_ArgumentsAvailableOnBlock()
    {
        var action = AIFunctionFactory.Create(
            (string product, int quantity) => $"Added {quantity}x {product}",
            "AddToCart", "Adds item to cart");

        var client = new DelegatingStreamingChatClient();
        var agent = new UIAgent(client, options =>
        {
            options.RegisterUIAction(action);
        });

        var callCount = 0;
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return EmitUIActionCall("call-1", "AddToCart", ct,
                    new Dictionary<string, object?> { ["product"] = "Widget", ["quantity"] = 3 });
            }
            return ResponseEmitters.EmitTextResponse("Added to cart.", ct);
        });

        var context = new AgentContext(agent);
        UIActionBlock? captured = null;
        context.RegisterOnStatusChanged(s =>
        {
            if (s == ConversationStatus.AwaitingInput)
            {
                var turn = context.Turns[^1];
                captured = turn.ResponseBlocks.OfType<UIActionBlock>().Single();
                captured.InvokeAsync().GetAwaiter().GetResult();
            }
        });

        await context.SendMessageAsync("Add widgets");

        Assert.NotNull(captured);
        Assert.Equal("AddToCart", captured!.ToolName);
        Assert.Equal("call-1", captured.Id);
    }

    [Fact]
    public async Task UIAction_ResultSentBackToLLM()
    {
        var action = AIFunctionFactory.Create(
            () => "Seattle, WA",
            "GetLocation", "Gets user location");

        var client = new DelegatingStreamingChatClient();
        var agent = new UIAgent(client, options =>
        {
            options.RegisterUIAction(action);
        });

        var callCount = 0;
        IEnumerable<ChatMessage>? resumeMessages = null;
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return EmitUIActionCall("loc-1", "GetLocation", ct);
            }
            resumeMessages = msgs;
            return ResponseEmitters.EmitTextResponse("You are in Seattle, WA.", ct);
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

        await context.SendMessageAsync("Where am I?");

        Assert.NotNull(resumeMessages);
        var toolMsg = resumeMessages!.Last(m => m.Role == ChatRole.Tool);
        var result = toolMsg.Contents.OfType<FunctionResultContent>().Single();
        Assert.Equal("loc-1", result.CallId);
    }

    [Fact]
    public async Task UIAction_StatusFlow_StreamingAwaitingStreamingIdle()
    {
        var action = AIFunctionFactory.Create(() => "ok", "Confirm", "Confirms");
        var client = new DelegatingStreamingChatClient();
        var agent = new UIAgent(client, options =>
        {
            options.RegisterUIAction(action);
        });

        var callCount = 0;
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return EmitUIActionCall("c1", "Confirm", ct);
            }
            return ResponseEmitters.EmitTextResponse("Done.", ct);
        });

        var context = new AgentContext(agent);
        var statuses = new List<ConversationStatus>();
        context.RegisterOnStatusChanged(s =>
        {
            statuses.Add(s);
            if (s == ConversationStatus.AwaitingInput)
            {
                context.Turns[^1].ResponseBlocks.OfType<UIActionBlock>().Single()
                    .InvokeAsync().GetAwaiter().GetResult();
            }
        });

        await context.SendMessageAsync("Go");

        Assert.Equal(new[]
        {
            ConversationStatus.Streaming,
            ConversationStatus.AwaitingInput,
            ConversationStatus.Streaming,
            ConversationStatus.Idle,
        }, statuses);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> EmitUIActionCall(
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
