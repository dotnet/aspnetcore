// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Samples;

public class S02_BackendToolsTest
{
    [Fact]
    public async Task BackendTool_AutoExecuted_ProducesInvocationBlock()
    {
        var searchCalled = false;
        var searchFunction = AIFunctionFactory.Create(
            (string location, string cuisine) =>
            {
                searchCalled = true;
                return new { Name = "Pizza Place", Rating = 4.5 };
            },
            "SearchRestaurants",
            "Search for restaurants");

        var callCount = 0;
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return ResponseEmitters.EmitToolCallResponse(
                    "call-1", "SearchRestaurants",
                    new Dictionary<string, object?> { ["location"] = "NYC", ["cuisine"] = "Italian" });
            }
            return ResponseEmitters.EmitTextResponse("I found Pizza Place rated 4.5!");
        });

        var chatOptions = new ChatOptions { Tools = [searchFunction] };
        var agent = new UIAgent(client, chatOptions);
        var context = new AgentContext(agent);

        await context.SendMessageAsync("Find Italian restaurants in NYC");

        Assert.True(searchCalled);

        var turn = context.Turns[0];
        var invocationBlock = turn.ResponseBlocks.OfType<FunctionInvocationContentBlock>().Single();
        Assert.Equal("SearchRestaurants", invocationBlock.ToolName);
        Assert.True(invocationBlock.HasResult);

        var textBlock = turn.ResponseBlocks.OfType<RichContentBlock>().Single();
        Assert.Contains("Pizza Place", textBlock.RawText);
    }

    [Fact]
    public async Task BackendTool_MultipleToolCalls_AllExecuted()
    {
        var callLog = new List<string>();

        var getWeather = AIFunctionFactory.Create(
            (string city) =>
            {
                callLog.Add($"weather:{city}");
                return $"{city}: 72°F";
            },
            "GetWeather",
            "Get weather for a city");

        var getTime = AIFunctionFactory.Create(
            (string city) =>
            {
                callLog.Add($"time:{city}");
                return $"{city}: 2:30 PM";
            },
            "GetTime",
            "Get time for a city");

        var callCount = 0;
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return ResponseEmitters.EmitMultipleToolCallResponse(ct,
                    new FunctionCallContent("c1", "GetWeather",
                        new Dictionary<string, object?> { ["city"] = "London" }),
                    new FunctionCallContent("c2", "GetTime",
                        new Dictionary<string, object?> { ["city"] = "London" }));
            }
            return ResponseEmitters.EmitTextResponse("London: 72°F at 2:30 PM");
        });

        var chatOptions = new ChatOptions { Tools = [getWeather, getTime] };
        var agent = new UIAgent(client, chatOptions);
        var context = new AgentContext(agent);

        await context.SendMessageAsync("Weather and time in London?");

        Assert.Equal(2, callLog.Count);
        Assert.Contains("weather:London", callLog);
        Assert.Contains("time:London", callLog);

        var turn = context.Turns[0];
        var invocations = turn.ResponseBlocks.OfType<FunctionInvocationContentBlock>().ToList();
        Assert.Equal(2, invocations.Count);
        Assert.All(invocations, b => Assert.True(b.HasResult));
    }

    [Fact]
    public async Task BackendTool_StatusRemainsStreaming_DuringAutoExecution()
    {
        var statuses = new List<ConversationStatus>();

        var tool = AIFunctionFactory.Create(() => "result", "DoWork", "Does work");
        var callCount = 0;
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return ResponseEmitters.EmitToolCallResponse("c1", "DoWork");
            }
            return ResponseEmitters.EmitTextResponse("Done");
        });

        var chatOptions = new ChatOptions { Tools = [tool] };
        var agent = new UIAgent(client, chatOptions);
        var context = new AgentContext(agent);
        context.RegisterOnStatusChanged(s => statuses.Add(s));

        await context.SendMessageAsync("Do it");

        // Backend tools don't cause AwaitingInput — stays Streaming throughout
        Assert.DoesNotContain(ConversationStatus.AwaitingInput, statuses);
        Assert.Equal(ConversationStatus.Idle, context.Status);
    }
}
