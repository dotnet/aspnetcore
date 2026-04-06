// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.AI.Tests.Pipeline;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Engine;

public class UIAgentCustomHandlerTests
{
    private sealed class CitationRaw
    {
        public string Source { get; set; } = "";
        public string Quote { get; set; } = "";
    }

    [Fact]
    public async Task UIAgent_WithCustomHandler_ProducesCustomBlocks()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) => EmitCitations(ct));

        var agent = new UIAgent(client, configure: options =>
        {
            options.AddBlockHandler(new DelegateBlockHandler<CitationBlock>((context, state) =>
            {
                if (context.Update.RawRepresentation is not CitationRaw raw)
                {
                    return BlockMappingResult<CitationBlock>.Pass();
                }
                context.MarkUpdateHandled();
                state.Source = raw.Source;
                state.Quote = raw.Quote;
                state.Id = Guid.NewGuid().ToString("N");
                return BlockMappingResult<CitationBlock>.Emit(state, state);
            }));
        });

        var blocks = new List<ContentBlock>();
        await foreach (var block in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "Find sources")))
        {
            blocks.Add(block);
        }

        var assistantBlocks = blocks.Where(b => b.Role == ChatRole.Assistant).ToList();
        Assert.Contains(assistantBlocks, b => b is CitationBlock);
        Assert.Contains(assistantBlocks, b => b is RichContentBlock);

        static async IAsyncEnumerable<ChatResponseUpdate> EmitCitations(
            [EnumeratorCancellation] CancellationToken ct)
        {
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                RawRepresentation = new CitationRaw { Source = "Journal", Quote = "Data shows..." }
            };
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                MessageId = "msg-1",
                Contents = [new TextContent("Based on the research...")]
            };
            await Task.CompletedTask;
        }
    }
}
