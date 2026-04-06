// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Pipeline;

public class CustomBlockHandlerTests
{
    private sealed class CitationRaw
    {
        public string Source { get; set; } = "";
        public string Quote { get; set; } = "";
    }

    private static async Task<List<ContentBlock>> ProcessAsync(
        BlockMappingPipeline pipeline, ChatResponseUpdate update)
    {
        var blocks = new List<ContentBlock>();
        await foreach (var block in pipeline.Process(update))
        {
            blocks.Add(block);
        }
        return blocks;
    }

    private static DelegateBlockHandler<CitationBlock> CreateCitationHandler()
    {
        return new DelegateBlockHandler<CitationBlock>((context, state) =>
        {
            if (context.Update.RawRepresentation is not CitationRaw raw)
            {
                if (state.Source.Length > 0)
                {
                    return BlockMappingResult<CitationBlock>.Complete();
                }
                return BlockMappingResult<CitationBlock>.Pass();
            }

            context.MarkUpdateHandled();

            if (state.Source.Length == 0)
            {
                state.Source = raw.Source;
                state.Quote = raw.Quote;
                state.Id = context.Update.MessageId ?? Guid.NewGuid().ToString("N");
                return BlockMappingResult<CitationBlock>.Emit(state, state);
            }
            else
            {
                state.Quote += "; " + raw.Quote;
                return BlockMappingResult<CitationBlock>.Update(state);
            }
        });
    }

    private static ChatResponseUpdate CreateCitationUpdate(string source, string quote, string? messageId = null)
    {
        return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = messageId ?? Guid.NewGuid().ToString("N"),
            RawRepresentation = new CitationRaw { Source = source, Quote = quote }
        };
    }

    private static bool HasTextContent(BlockMappingContext context)
    {
        foreach (var content in context.UnhandledContents)
        {
            if (content is TextContent)
            {
                return true;
            }
        }
        return false;
    }

    [Fact]
    public async Task CustomHandler_RecognizesRawRepresentation_EmitsCustomBlock()
    {
        var options = new UIAgentOptions();
        options.AddBlockHandler(CreateCitationHandler());

        var pipeline = new BlockMappingPipeline(options);
        var update = CreateCitationUpdate("Wikipedia", "The sky is blue.");

        var blocks = await ProcessAsync(pipeline, update);

        Assert.Single(blocks);
        var block = Assert.IsType<CitationBlock>(blocks[0]);
        Assert.Equal("Wikipedia", block.Source);
        Assert.Equal("The sky is blue.", block.Quote);
    }

    [Fact]
    public async Task CustomHandler_PassesOnTextContent_BuiltInHandlerClaims()
    {
        var customHandlerSawText = false;
        var options = new UIAgentOptions();
        options.AddBlockHandler(new DelegateBlockHandler<CitationBlock>((context, state) =>
        {
            if (HasTextContent(context))
            {
                customHandlerSawText = true;
            }
            return BlockMappingResult<CitationBlock>.Pass();
        }));

        var pipeline = new BlockMappingPipeline(options);
        var update = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = "msg-1",
            Contents = [new TextContent("Hello")]
        };

        var blocks = await ProcessAsync(pipeline, update);

        // Custom handler runs first and sees TextContent (since it has priority),
        // but passes on it. Built-in text handler then claims it.
        Assert.True(customHandlerSawText);
        Assert.Single(blocks);
        Assert.IsType<RichContentBlock>(blocks[0]);
    }

    [Fact]
    public async Task RawRepresentation_BuiltInHandlersPass_CustomHandlerClaims()
    {
        var options = new UIAgentOptions();
        options.AddBlockHandler(CreateCitationHandler());

        var pipeline = new BlockMappingPipeline(options);
        var update = CreateCitationUpdate("Nature", "Water is wet.");

        var blocks = await ProcessAsync(pipeline, update);
        Assert.Single(blocks);
        Assert.IsType<CitationBlock>(blocks[0]);
    }

    [Fact]
    public async Task MixedContent_TextAndRawRepresentation_BothHandlersClaim()
    {
        var options = new UIAgentOptions();
        // This handler only claims content when RawRepresentation is CitationRaw
        // and does NOT mark TextContent as handled
        options.AddBlockHandler(new DelegateBlockHandler<CitationBlock>((context, state) =>
        {
            if (context.Update.RawRepresentation is not CitationRaw raw)
            {
                return BlockMappingResult<CitationBlock>.Pass();
            }

            state.Source = raw.Source;
            state.Id = Guid.NewGuid().ToString("N");
            return BlockMappingResult<CitationBlock>.Emit(state, state);
        }));

        var pipeline = new BlockMappingPipeline(options);
        var update = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = "msg-1",
            Contents = [new TextContent("Here's a fact: ")],
            RawRepresentation = new CitationRaw { Source = "Science", Quote = "E=mc²" }
        };

        var blocks = await ProcessAsync(pipeline, update);

        Assert.Equal(2, blocks.Count);
        Assert.Contains(blocks, b => b is RichContentBlock);
        Assert.Contains(blocks, b => b is CitationBlock);
    }

    [Fact]
    public async Task CustomHandler_MultiUpdateLifecycle_EmitUpdateComplete()
    {
        var options = new UIAgentOptions();
        options.AddBlockHandler(CreateCitationHandler());

        var pipeline = new BlockMappingPipeline(options);

        // First update — Emit
        var blocks1 = await ProcessAsync(pipeline, CreateCitationUpdate("Book", "Quote 1"));
        Assert.Single(blocks1);
        var block = Assert.IsType<CitationBlock>(blocks1[0]);
        Assert.Equal("Quote 1", block.Quote);

        // Second update — Update
        var changeCount = 0;
        block.OnChanged(() => changeCount++);
        await ProcessAsync(pipeline, CreateCitationUpdate("Book", "Quote 2"));
        Assert.Equal("Quote 1; Quote 2", block.Quote);
        Assert.Equal(1, changeCount);

        // Third update — no RawRepresentation → Complete
        await ProcessAsync(pipeline, new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = "msg-2",
            Contents = [new TextContent("Summary")]
        });
        Assert.Equal(BlockLifecycleState.Inactive, block.LifecycleState);
    }

    [Fact]
    public async Task ActiveHandler_GetsPriority_OverInactiveHandlers()
    {
        var options = new UIAgentOptions();
        options.AddBlockHandler(CreateCitationHandler());

        var pipeline = new BlockMappingPipeline(options);

        // Emit — handler becomes active
        await ProcessAsync(pipeline, CreateCitationUpdate("A", "a"));

        // Update — active handler gets priority
        await ProcessAsync(pipeline, CreateCitationUpdate("A", "b"));

        // Complete — handler returns to inactive
        await ProcessAsync(pipeline, new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = "msg-1",
            Contents = [new TextContent("done")]
        });

        // After complete, a new citation should create a new block
        var newBlocks = await ProcessAsync(pipeline, CreateCitationUpdate("C", "c"));

        Assert.Single(newBlocks);
        var newBlock = Assert.IsType<CitationBlock>(newBlocks[0]);
        Assert.Equal("C", newBlock.Source);
    }
}
