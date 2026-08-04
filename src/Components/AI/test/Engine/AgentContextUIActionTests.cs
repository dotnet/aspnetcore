// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Engine;

public class AgentContextUIActionTests
{
    private static (UIAgent agent, DelegatingStreamingChatClient client) CreateAgentWithUIAction(
        AIFunction uiAction)
    {
        var chatClient = new DelegatingStreamingChatClient();
        var agent = new UIAgent(chatClient, options =>
        {
            options.RegisterUIAction(uiAction);
        });
        return (agent, chatClient);
    }

    [Fact]
    public async Task UIAction_SetsStatusToAwaitingInput()
    {
        var action = AIFunctionFactory.Create(() => "result", "GetClientData", "Gets client data");
        var (agent, client) = CreateAgentWithUIAction(action);
        client.SetHandler((msgs, opts, ct) =>
            EmitUIActionCall("call-1", "GetClientData", ct));

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

        await context.SendMessageAsync("Get data");

        Assert.Contains(ConversationStatus.AwaitingInput, statuses);
    }

    [Fact]
    public async Task UIAction_ParksUntilInvoked()
    {
        var action = AIFunctionFactory.Create(() => "value", "GetValue", "Gets a value");
        var (agent, client) = CreateAgentWithUIAction(action);
        var callCount = 0;
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return EmitUIActionCall("call-1", "GetValue", ct);
            }
            return ResponseEmitters.EmitTextResponse("Done");
        });

        var context = new AgentContext(agent);
        var statuses = new List<ConversationStatus>();
        context.RegisterOnStatusChanged(s =>
        {
            statuses.Add(s);
            if (s == ConversationStatus.AwaitingInput)
            {
                var turn = context.Turns[^1];
                turn.ResponseBlocks.OfType<UIActionBlock>().Single()
                    .InvokeAsync().GetAwaiter().GetResult();
            }
        });

        await context.SendMessageAsync("Go");

        // Streaming → AwaitingInput → Streaming → Idle
        Assert.Equal(new[]
        {
            ConversationStatus.Streaming,
            ConversationStatus.AwaitingInput,
            ConversationStatus.Streaming,
            ConversationStatus.Idle,
        }, statuses);
    }

    [Fact]
    public async Task UIAction_ResultSentBackToLLM()
    {
        var action = AIFunctionFactory.Create(() => "42", "GetAnswer", "Gets the answer");
        var (agent, client) = CreateAgentWithUIAction(action);
        var callCount = 0;
        IEnumerable<ChatMessage>? resumeMessages = null;
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return EmitUIActionCall("call-1", "GetAnswer", ct);
            }
            resumeMessages = msgs;
            return ResponseEmitters.EmitTextResponse("The answer is 42.");
        });

        var context = new AgentContext(agent);
        context.RegisterOnStatusChanged(s =>
        {
            if (s == ConversationStatus.AwaitingInput)
            {
                var turn = context.Turns[^1];
                turn.ResponseBlocks.OfType<UIActionBlock>().Single()
                    .InvokeAsync().GetAwaiter().GetResult();
            }
        });

        await context.SendMessageAsync("What is the answer?");

        Assert.NotNull(resumeMessages);
        var toolMessage = resumeMessages!.LastOrDefault(m => m.Role == ChatRole.Tool);
        Assert.NotNull(toolMessage);
        var resultContent = toolMessage!.Contents.OfType<FunctionResultContent>().Single();
        Assert.Equal("call-1", resultContent.CallId);
    }

    [Fact]
    public async Task UIAction_BlockEmittedWithCorrectProperties()
    {
        var action = AIFunctionFactory.Create(
            (string city) => $"sunny in {city}",
            "GetWeather", "Gets weather");
        var (agent, client) = CreateAgentWithUIAction(action);
        var callCount = 0;
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return EmitUIActionCall("call-1", "GetWeather", ct,
                    new Dictionary<string, object?> { ["city"] = "Seattle" });
            }
            return ResponseEmitters.EmitTextResponse("It's sunny.");
        });

        var context = new AgentContext(agent);
        UIActionBlock? capturedBlock = null;
        context.RegisterOnStatusChanged(s =>
        {
            if (s == ConversationStatus.AwaitingInput)
            {
                var turn = context.Turns[^1];
                capturedBlock = turn.ResponseBlocks.OfType<UIActionBlock>().First();
                capturedBlock.InvokeAsync().GetAwaiter().GetResult();
            }
        });

        await context.SendMessageAsync("Weather?");

        Assert.NotNull(capturedBlock);
        Assert.Equal("GetWeather", capturedBlock!.ToolName);
        Assert.Equal("call-1", capturedBlock.Id);
        Assert.True(capturedBlock.IsComplete);
        Assert.True(capturedBlock.HasResult);
    }

    [Fact]
    public async Task UIAction_ContinuationBlocksAddedToSameTurn()
    {
        var action = AIFunctionFactory.Create(() => "data", "Fetch", "Fetches data");
        var (agent, client) = CreateAgentWithUIAction(action);
        var callCount = 0;
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return EmitUIActionCall("call-1", "Fetch", ct);
            }
            return ResponseEmitters.EmitTextResponse("Here is the data.");
        });

        var context = new AgentContext(agent);
        context.RegisterOnStatusChanged(s =>
        {
            if (s == ConversationStatus.AwaitingInput)
            {
                var turn = context.Turns[^1];
                turn.ResponseBlocks.OfType<UIActionBlock>().Single()
                    .InvokeAsync().GetAwaiter().GetResult();
            }
        });

        await context.SendMessageAsync("Fetch data");

        Assert.Single(context.Turns);
        var turn = context.Turns[0];
        var uiBlock = turn.ResponseBlocks.OfType<UIActionBlock>().SingleOrDefault();
        var textBlock = turn.ResponseBlocks.OfType<RichContentBlock>().SingleOrDefault();
        Assert.NotNull(uiBlock);
        Assert.NotNull(textBlock);
        Assert.Equal("Here is the data.", textBlock!.RawText);
    }

    [Fact]
    public async Task UIAction_DeclarationsMergedIntoChatOptions()
    {
        var action = AIFunctionFactory.Create(() => "ok", "MyTool", "My tool");
        var (agent, client) = CreateAgentWithUIAction(action);
        ChatOptions? capturedOptions = null;
        client.SetHandler((msgs, opts, ct) =>
        {
            capturedOptions = opts;
            return ResponseEmitters.EmitTextResponse("Hello");
        });

        var context = new AgentContext(agent);
        await context.SendMessageAsync("Hi");

        Assert.NotNull(capturedOptions);
        Assert.NotNull(capturedOptions!.Tools);
        Assert.Single(capturedOptions.Tools!);
        var tool = capturedOptions.Tools![0];
        // Should be a declaration-only (AIFunctionDeclaration but not AIFunction)
        Assert.False(tool is AIFunction);
        Assert.IsAssignableFrom<AIFunctionDeclaration>(tool);
    }

    [Fact]
    public async Task UIAction_ExistingToolsPreserved()
    {
        var action = AIFunctionFactory.Create(() => "client", "ClientTool", "Client tool");
        var serverTool = AIFunctionFactory.Create(() => "server", "ServerTool", "Server tool");

        var chatClient = new DelegatingStreamingChatClient();
        var agent = new UIAgent(chatClient, options =>
        {
            options.ChatOptions = new ChatOptions { Tools = [serverTool] };
            options.RegisterUIAction(action);
        });

        ChatOptions? capturedOptions = null;
        chatClient.SetHandler((msgs, opts, ct) =>
        {
            capturedOptions = opts;
            return ResponseEmitters.EmitTextResponse("Hello");
        });

        var context = new AgentContext(agent);
        await context.SendMessageAsync("Hi");

        Assert.NotNull(capturedOptions);
        Assert.Equal(2, capturedOptions!.Tools!.Count);
    }

    [Fact]
    public async Task UIAction_IsAFunctionInvocationContentBlock()
    {
        var action = AIFunctionFactory.Create(() => "result", "DoThing", "Does a thing");
        var (agent, client) = CreateAgentWithUIAction(action);
        var callCount = 0;
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return EmitUIActionCall("call-1", "DoThing", ct);
            }
            return ResponseEmitters.EmitTextResponse("Done");
        });

        var context = new AgentContext(agent);
        ContentBlock? capturedBlock = null;
        context.RegisterOnStatusChanged(s =>
        {
            if (s == ConversationStatus.AwaitingInput)
            {
                var turn = context.Turns[^1];
                capturedBlock = turn.ResponseBlocks.First(b => b is UIActionBlock);
                ((UIActionBlock)capturedBlock).InvokeAsync().GetAwaiter().GetResult();
            }
        });

        await context.SendMessageAsync("Do it");

        // UIActionBlock IS-A InteractiveFunctionBlock with an InnerBlock
        Assert.IsAssignableFrom<InteractiveFunctionBlock>(capturedBlock);
        Assert.IsType<UIActionBlock>(capturedBlock);
        var uiBlock = (UIActionBlock)capturedBlock!;
        Assert.NotNull(uiBlock.InnerBlock);
        Assert.IsType<FunctionInvocationContentBlock>(uiBlock.InnerBlock);
    }

    [Fact]
    public async Task UIAction_InvokeAsync_WorksWithoutAgentContext()
    {
        var action = AIFunctionFactory.Create(() => "v", "F", "desc");
        var innerBlock = new FunctionInvocationContentBlock { Call = new FunctionCallContent("c1", "F") };
        var block = new UIActionBlock(action, innerBlock);

        await block.InvokeAsync();

        Assert.True(block.IsComplete);
        Assert.True(block.HasResult);
    }

    [Fact]
    public async Task MixedToolCall_BackendAndUIAction_BothInvoked()
    {
        // Backend tool: invoked automatically by AgentContext
        var backendTool = AIFunctionFactory.Create(
            (string city) => $"72°F and sunny in {city}",
            "GetWeather", "Gets weather");

        // UIAction: invoked by the user via AgentContext callback
        var uiAction = AIFunctionFactory.Create(
            () => "Seattle, WA",
            "GetUserLocation", "Gets location");

        var chatClient = new DelegatingStreamingChatClient();
        var agent = new UIAgent(chatClient, options =>
        {
            options.ChatOptions = new ChatOptions { Tools = [backendTool] };
            options.RegisterUIAction(uiAction);
        });

        var callCount = 0;
        IEnumerable<ChatMessage>? resumeMessages = null;
        chatClient.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            if (callCount == 1)
            {
                // Emit both tool calls in the same response (parallel tool calls)
                return ResponseEmitters.EmitMultipleToolCallResponse(ct,
                    new FunctionCallContent("call-weather", "GetWeather",
                        new Dictionary<string, object?> { ["city"] = "Seattle" }),
                    new FunctionCallContent("call-location", "GetUserLocation"));
            }
            resumeMessages = msgs;
            return ResponseEmitters.EmitTextResponse("It's 72°F and sunny in Seattle, WA.");
        });

        var context = new AgentContext(agent);
        var statuses = new List<ConversationStatus>();
        context.RegisterOnStatusChanged(s =>
        {
            statuses.Add(s);
            if (s == ConversationStatus.AwaitingInput)
            {
                var turn = context.Turns[^1];
                var actionBlock = turn.ResponseBlocks.OfType<UIActionBlock>().Single();
                actionBlock.InvokeAsync().GetAwaiter().GetResult();
            }
        });

        await context.SendMessageAsync("What's the weather at my location?");

        // Should have both block types in the response
        var turn = context.Turns[0];
        var uiBlock = turn.ResponseBlocks.OfType<UIActionBlock>().Single();
        var toolBlock = turn.ResponseBlocks.OfType<FunctionInvocationContentBlock>().Single();

        // Both should have results
        Assert.True(uiBlock.HasResult);
        Assert.True(toolBlock.HasResult);
        Assert.Equal("GetUserLocation", uiBlock.ToolName);
        Assert.Equal("GetWeather", toolBlock.ToolName);

        // Status should flow: Streaming → AwaitingInput → Streaming → Idle
        Assert.Equal(new[]
        {
            ConversationStatus.Streaming,
            ConversationStatus.AwaitingInput,
            ConversationStatus.Streaming,
            ConversationStatus.Idle,
        }, statuses);

        // The resume should send BOTH results to the LLM
        Assert.NotNull(resumeMessages);
        var toolMessage = resumeMessages!.Last(m => m.Role == ChatRole.Tool);
        var results = toolMessage.Contents.OfType<FunctionResultContent>().ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.CallId == "call-weather");
        Assert.Contains(results, r => r.CallId == "call-location");

        // Should have text response after resume
        var textBlock = turn.ResponseBlocks.OfType<RichContentBlock>().LastOrDefault();
        Assert.NotNull(textBlock);
    }

    [Fact]
    public async Task MixedToolCall_BackendToolResultsSetOnBlock()
    {
        var backendTool = AIFunctionFactory.Create(
            (string city) => $"Weather: sunny in {city}",
            "GetWeather", "Gets weather");
        var uiAction = AIFunctionFactory.Create(
            () => "Portland, OR",
            "GetLocation", "Gets location");

        var chatClient = new DelegatingStreamingChatClient();
        var agent = new UIAgent(chatClient, options =>
        {
            options.ChatOptions = new ChatOptions { Tools = [backendTool] };
            options.RegisterUIAction(uiAction);
        });

        var callCount = 0;
        chatClient.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return ResponseEmitters.EmitMultipleToolCallResponse(ct,
                    new FunctionCallContent("call-w", "GetWeather",
                        new Dictionary<string, object?> { ["city"] = "Portland" }),
                    new FunctionCallContent("call-l", "GetLocation"));
            }
            return ResponseEmitters.EmitTextResponse("Done");
        });

        var context = new AgentContext(agent);
        FunctionInvocationContentBlock? capturedToolBlock = null;
        context.RegisterOnStatusChanged(s =>
        {
            if (s == ConversationStatus.AwaitingInput)
            {
                var turn = context.Turns[^1];
                capturedToolBlock = turn.ResponseBlocks
                    .OfType<FunctionInvocationContentBlock>()
                    .First();

                // The backend tool should already have its result set
                // because AgentContext invokes it immediately
                turn.ResponseBlocks.OfType<UIActionBlock>().Single()
                    .InvokeAsync().GetAwaiter().GetResult();
            }
        });

        await context.SendMessageAsync("Go");

        Assert.NotNull(capturedToolBlock);
        Assert.True(capturedToolBlock!.HasResult);
        Assert.Contains("sunny in Portland", capturedToolBlock.Result!.Result?.ToString());
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> EmitUIActionCall(
        string callId,
        string name,
        [EnumeratorCancellation] CancellationToken ct = default,
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
