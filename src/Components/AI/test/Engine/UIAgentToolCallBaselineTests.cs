// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Engine;

public class UIAgentToolCallBaselineTests
{
    [Fact]
    public async Task Step02_BackendToolCall_ProducesCorrectBlocks()
    {
        var client = RecordingLoader.CreateReplayClient(
            "Step02_BackendToolsTest.PostRun_WithBackendToolCall_InvokesToolAndStreamsResult.recording.json");
        var agent = new UIAgent(client);

        var blocks = new List<ContentBlock>();
        await foreach (var block in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "Find Italian restaurants in Seattle")))
        {
            blocks.Add(block);
        }

        var assistantBlocks = blocks.Where(b => b.Role == ChatRole.Assistant).ToList();

        // Should have a FunctionInvocationContentBlock (SearchRestaurants call)
        var toolBlock = assistantBlocks.OfType<FunctionInvocationContentBlock>().FirstOrDefault();
        Assert.NotNull(toolBlock);
        Assert.Equal("SearchRestaurants", toolBlock.ToolName);
        Assert.Equal("call_abc123", toolBlock.Id);
        Assert.True(toolBlock.HasResult);

        // Should also have a RichContentBlock (text response describing results)
        var textBlock = assistantBlocks.OfType<RichContentBlock>().FirstOrDefault();
        Assert.NotNull(textBlock);
        Assert.False(string.IsNullOrEmpty(textBlock.RawText));
    }
}
