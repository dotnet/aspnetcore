// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.AI.Tests.Engine;

public class UIAgentRecordedTests
{
    private const string Endpoint = "https://your-resource.cognitiveservices.azure.com/";
    private const string DeploymentName = "your-deployment-name";

    private readonly ITestOutputHelper _output;
    private readonly ILoggerFactory _loggerFactory;

    public UIAgentRecordedTests(ITestOutputHelper output)
    {
        _output = output;
        _loggerFactory = new XunitLoggerFactory(output);
    }

    private static string GetBaselinePath(string fileName)
    {
        return Path.Combine(
            AppContext.BaseDirectory,
            "Baselines",
            fileName);
    }

    private static string GetSourceBaselinePath(string fileName)
    {
        // Navigate from bin output back to source Baselines folder
        var projectDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", ".."));
        return Path.Combine(projectDir, "Baselines", fileName);
    }

    private static IChatClient CreateAzureOpenAIClient()
    {
        var azureClient = new AzureOpenAIClient(
            new Uri(Endpoint),
            new DefaultAzureCredential());
        return azureClient.GetChatClient(DeploymentName).AsIChatClient();
    }

    private static IChatClient CreateReplayOrRecordClient(string baselineFileName)
    {
        var baselinePath = GetBaselinePath(baselineFileName);
        if (File.Exists(baselinePath))
        {
            return RecordingLoader.CreateReplayClient(baselineFileName);
        }

        // Record mode: wrap real LLM
        var inner = CreateAzureOpenAIClient();
        return new RecordingChatClient(inner);
    }

    private static void SaveIfRecording(IChatClient client, string baselineFileName)
    {
        if (client is RecordingChatClient recorder)
        {
            var sourcePath = GetSourceBaselinePath(baselineFileName);
            recorder.SaveRecording(sourcePath);

            // Also save to output dir so subsequent test runs in the same session can find it
            var outputPath = GetBaselinePath(baselineFileName);
            recorder.SaveRecording(outputPath);
        }
    }

    [Fact]
    public async Task TextStreaming_SingleTurn_ProducesRichContentBlock()
    {
        const string baseline = "TextStreaming_SingleTurn.recording.json";
        var client = CreateReplayOrRecordClient(baseline);
        var agent = new UIAgent(client, configure: null, _loggerFactory);

        var blocks = new List<ContentBlock>();
        await foreach (var block in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "Say hello in one sentence.")))
        {
            blocks.Add(block);
        }

        SaveIfRecording(client, baseline);

        var assistantBlocks = blocks.Where(b => b.Role == ChatRole.Assistant).ToList();
        Assert.NotEmpty(assistantBlocks);
        var textBlock = Assert.IsType<RichContentBlock>(assistantBlocks[0]);
        Assert.False(string.IsNullOrEmpty(textBlock.RawText));
        Assert.Equal(BlockLifecycleState.Inactive, textBlock.LifecycleState);
    }

    [Fact]
    public async Task TextStreaming_MultiTurn_ProducesBlocksPerTurn()
    {
        const string baseline = "TextStreaming_MultiTurn.recording.json";
        var client = CreateReplayOrRecordClient(baseline);
        var agent = new UIAgent(client, configure: null, _loggerFactory);

        // Turn 1
        var turn1Blocks = new List<ContentBlock>();
        await foreach (var block in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "Say hello in one sentence.")))
        {
            turn1Blocks.Add(block);
        }

        var text1 = turn1Blocks.OfType<RichContentBlock>()
            .First(b => b.Role == ChatRole.Assistant);
        Assert.False(string.IsNullOrEmpty(text1.RawText));
        Assert.Equal(BlockLifecycleState.Inactive, text1.LifecycleState);

        // Turn 2
        var turn2Blocks = new List<ContentBlock>();
        await foreach (var block in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "Now say goodbye in one sentence.")))
        {
            turn2Blocks.Add(block);
        }

        var text2 = turn2Blocks.OfType<RichContentBlock>()
            .First(b => b.Role == ChatRole.Assistant);
        Assert.False(string.IsNullOrEmpty(text2.RawText));
        Assert.Equal(BlockLifecycleState.Inactive, text2.LifecycleState);

        SaveIfRecording(client, baseline);
    }

    [Fact]
    public async Task ToolCall_ProducesFunctionInvocationBlock()
    {
        const string baseline = "ToolCall_BackendTool.recording.json";
        var client = CreateReplayOrRecordClient(baseline);

        var tool = AIFunctionFactory.Create(GetWeather);
        var chatClient = client.AsBuilder()
            .UseFunctionInvocation(_loggerFactory)
            .Build();
        var agent = new UIAgent(chatClient, new ChatOptions { Tools = [tool] }, _loggerFactory);

        var blocks = new List<ContentBlock>();
        await foreach (var block in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "What's the weather in Seattle?")))
        {
            blocks.Add(block);
        }

        SaveIfRecording(client, baseline);

        var assistantBlocks = blocks.Where(b => b.Role == ChatRole.Assistant).ToList();

        // FunctionInvokingChatClient handles the tool call loop internally.
        // It yields the tool call updates, invokes the tool, then yields the text response.
        Assert.NotEmpty(assistantBlocks);

        // Should have a FunctionInvocationContentBlock for the tool call
        var toolBlock = assistantBlocks.OfType<FunctionInvocationContentBlock>().FirstOrDefault();
        Assert.NotNull(toolBlock);
        Assert.Equal("GetWeather", toolBlock!.ToolName);

        // Should have text content after the tool call completes
        var hasText = assistantBlocks.OfType<RichContentBlock>().Any(b => !string.IsNullOrEmpty(b.RawText));
        Assert.True(hasText, $"Expected text content. Block types: {string.Join(", ", assistantBlocks.Select(b => b.GetType().Name))}");
    }

    [Fact]
    public async Task ToolApproval_ProducesApprovalBlockAndResumes()
    {
        const string baseline = "ToolApproval_HumanInLoop.recording.json";
        var client = CreateReplayOrRecordClient(baseline);

        var tool = new ApprovalRequiredAIFunction(AIFunctionFactory.Create(DeleteFile));
        var chatClient = client.AsBuilder()
            .UseFunctionInvocation(_loggerFactory)
            .Build();
        var agent = new UIAgent(chatClient, options => options.ChatOptions = new ChatOptions { Tools = [tool] }, _loggerFactory);

        var context = new AgentContext(agent);
        FunctionApprovalBlock? approvalBlock = null;
        context.RegisterOnStatusChanged(s =>
        {
            if (s == ConversationStatus.AwaitingInput)
            {
                var turn = context.Turns[^1];
                approvalBlock = turn.ResponseBlocks.OfType<FunctionApprovalBlock>().FirstOrDefault();
                approvalBlock?.Approve();
            }
        });

        await context.SendMessageAsync("Delete the file named test.txt");

        SaveIfRecording(client, baseline);

        Assert.NotNull(approvalBlock);
        Assert.Equal(ApprovalStatus.Approved, approvalBlock!.Status);
        Assert.Equal("DeleteFile", approvalBlock.InnerBlock.ToolName);

        // After approval, should have continued to produce text
        var lastTurn = context.Turns[^1];
        var textBlock = lastTurn.ResponseBlocks.OfType<RichContentBlock>().LastOrDefault();
        Assert.NotNull(textBlock);
        Assert.False(string.IsNullOrEmpty(textBlock!.RawText));
    }

    [Fact]
    public async Task UIAction_ProducesUIActionBlockAndResumes()
    {
        const string baseline = "UIAction_ClientTool.recording.json";
        var client = CreateReplayOrRecordClient(baseline);

        var action = AIFunctionFactory.Create(GetUserLocation);
        var agent = new UIAgent(client, options =>
        {
            options.RegisterUIAction(action);
        }, _loggerFactory);

        var context = new AgentContext(agent);
        UIActionBlock? actionBlock = null;
        context.RegisterOnStatusChanged(s =>
        {
            if (s == ConversationStatus.AwaitingInput)
            {
                var turn = context.Turns[^1];
                actionBlock = turn.ResponseBlocks.OfType<UIActionBlock>().FirstOrDefault();
                if (actionBlock is not null)
                {
                    actionBlock.InvokeAsync().GetAwaiter().GetResult();
                }
            }
        });

        await context.SendMessageAsync(
            "Use the GetUserLocation tool to find my location, " +
            "then suggest fun things to do there.");

        SaveIfRecording(client, baseline);

        Assert.NotNull(actionBlock);
        Assert.Equal("GetUserLocation", actionBlock!.ToolName);
        Assert.True(actionBlock.IsComplete);
        Assert.True(actionBlock.HasResult);

        // After UIAction resume, should have text response
        var lastTurn = context.Turns[^1];
        var textBlock = lastTurn.ResponseBlocks.OfType<RichContentBlock>().LastOrDefault();
        Assert.NotNull(textBlock);
        Assert.False(string.IsNullOrEmpty(textBlock!.RawText));
    }

    [Description("Get the current weather for a city.")]
    private static string GetWeather([Description("The city name")] string city)
    {
        return $"The weather in {city} is 72°F and sunny.";
    }

    [Description("Delete a file by name.")]
    private static string DeleteFile([Description("The file name to delete")] string fileName)
    {
        return $"File '{fileName}' has been deleted.";
    }

    [Description("Get the user's current location from GPS.")]
    private static string GetUserLocation()
    {
        return "Seattle, WA (47.61°N, 122.33°W)";
    }

    [Fact]
    public async Task MixedToolCall_BackendAndUIAction_BothInvoked()
    {
        const string baseline = "MixedToolCall_BackendAndUIAction.recording.json";
        var client = CreateReplayOrRecordClient(baseline);

        var backendTool = AIFunctionFactory.Create(GetWeather);
        var uiAction = AIFunctionFactory.Create(GetUserLocation);

        var chatClient = client.AsBuilder()
            .UseFunctionInvocation(_loggerFactory)
            .Build();
        var agent = new UIAgent(chatClient, options =>
        {
            options.ChatOptions = new ChatOptions { Tools = [backendTool] };
            options.RegisterUIAction(uiAction);
        }, _loggerFactory);

        var context = new AgentContext(agent);
        UIActionBlock? actionBlock = null;
        FunctionInvocationContentBlock? weatherBlock = null;
        context.RegisterOnStatusChanged(s =>
        {
            if (s == ConversationStatus.AwaitingInput)
            {
                var turn = context.Turns[^1];
                actionBlock = turn.ResponseBlocks.OfType<UIActionBlock>().FirstOrDefault();
                weatherBlock = turn.ResponseBlocks
                    .OfType<FunctionInvocationContentBlock>()
                    .FirstOrDefault();
                if (actionBlock is not null)
                {
                    actionBlock.InvokeAsync().GetAwaiter().GetResult();
                }
            }
        });

        await context.SendMessageAsync(
            "Use the GetUserLocation tool to get my location, " +
            "then use GetWeather to check the weather there. " +
            "Call both tools at the same time.");

        SaveIfRecording(client, baseline);

        // UIAction should have been invoked
        Assert.NotNull(actionBlock);
        Assert.Equal("GetUserLocation", actionBlock!.ToolName);
        Assert.True(actionBlock.IsComplete);
        Assert.True(actionBlock.HasResult);

        // Backend tool should have been invoked automatically
        Assert.NotNull(weatherBlock);
        Assert.Equal("GetWeather", weatherBlock!.ToolName);
        Assert.True(weatherBlock.HasResult);

        // Should have text response after both tools complete
        var lastTurn = context.Turns[^1];
        var textBlock = lastTurn.ResponseBlocks.OfType<RichContentBlock>().LastOrDefault();
        Assert.NotNull(textBlock);
        Assert.False(string.IsNullOrEmpty(textBlock!.RawText));
    }
}
