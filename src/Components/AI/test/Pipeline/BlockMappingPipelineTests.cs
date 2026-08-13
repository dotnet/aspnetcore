// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Pipeline;

public class BlockMappingPipelineTests
{
    [Fact]
    public async Task Process_SingleTextUpdate_EmitsOneRichContentBlock()
    {
        var pipeline = CreatePipeline();
        var update = CreateTextUpdate("msg-1", "Hello");

        var blocks = await ProcessAsync(pipeline, update);

        var block = Assert.IsType<RichContentBlock>(Assert.Single(blocks));
        Assert.Equal("Hello", block.RawText);
        Assert.Equal("msg-1", block.Id);
        Assert.Equal(ChatRole.Assistant, block.Role);
        Assert.Equal(BlockLifecycleState.Active, block.LifecycleState);
    }

    [Fact]
    public async Task Process_MultipleTextUpdates_SingleBlockAccumulates()
    {
        var pipeline = CreatePipeline();

        var blocks = await ProcessAsync(pipeline, CreateTextUpdate("msg-1", "Hello"));
        var moreBlocks = await ProcessAsync(pipeline, CreateTextUpdate("msg-1", " world"));

        Assert.Empty(moreBlocks);
        var block = Assert.IsType<RichContentBlock>(Assert.Single(blocks));
        Assert.Equal("Hello world", block.RawText);
    }

    [Fact]
    public async Task Process_SubsequentUpdate_NotifiesSubscribers()
    {
        var pipeline = CreatePipeline();
        var blocks = await ProcessAsync(pipeline, CreateTextUpdate("msg-1", "Hello"));
        var changeCount = 0;
        using var subscription = blocks[0].OnChanged(() => changeCount++);

        await ProcessAsync(pipeline, CreateTextUpdate("msg-1", " world"));

        Assert.Equal(1, changeCount);
    }

    [Fact]
    public async Task Process_UpdateWithoutText_CompletesActiveBlock()
    {
        var pipeline = CreatePipeline();
        var blocks = await ProcessAsync(pipeline, CreateTextUpdate("msg-1", "Hello"));

        await ProcessAsync(pipeline, new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = "msg-1",
            Contents = [],
        });

        Assert.Equal(BlockLifecycleState.Inactive, blocks[0].LifecycleState);
    }

    [Fact]
    public async Task Finalize_DeactivatesAndNotifiesActiveBlocks()
    {
        var pipeline = CreatePipeline();
        var blocks = await ProcessAsync(pipeline, CreateTextUpdate("msg-1", "Hello"));
        var changeCount = 0;
        using var subscription = blocks[0].OnChanged(() => changeCount++);

        pipeline.Finalize();

        Assert.Equal(BlockLifecycleState.Inactive, blocks[0].LifecycleState);
        Assert.Equal(1, changeCount);
    }

    [Fact]
    public async Task Process_RegisteredHandler_ClaimsContentBeforeTextHandler()
    {
        var options = new UIAgentOptions();
        options.AddBlockHandler(new UppercaseTextHandler());
        var pipeline = new BlockMappingPipeline(options);

        var blocks = await ProcessAsync(pipeline, CreateTextUpdate("msg-1", "Hello"));

        var block = Assert.IsType<UppercaseTextBlock>(Assert.Single(blocks));
        Assert.Equal("HELLO", block.Text);
    }

    [Fact]
    public void AddBlockHandler_NullHandlerThrows()
    {
        var options = new UIAgentOptions();

        Assert.Throws<ArgumentNullException>(() => options.AddBlockHandler<RichContentBlock>(null!));
    }

    private static BlockMappingPipeline CreatePipeline() => new(new UIAgentOptions());

    private static ChatResponseUpdate CreateTextUpdate(string messageId, string text) => new()
    {
        Role = ChatRole.Assistant,
        MessageId = messageId,
        Contents = [new TextContent(text)],
    };

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

    private sealed class UppercaseTextBlock : ContentBlock
    {
        public string Text { get; set; } = "";
    }

    private sealed class UppercaseTextHandler : ContentBlockHandler<UppercaseTextBlock>
    {
        public override BlockMappingResult<UppercaseTextBlock> Handle(
            BlockMappingContext context, UppercaseTextBlock state)
        {
            foreach (var content in context.UnhandledContents)
            {
                if (content is TextContent text)
                {
                    context.MarkHandled(text);
                    state.Text = (text.Text ?? "").ToUpperInvariant();
                    state.Id = context.Update.MessageId ?? Guid.NewGuid().ToString("N");
                    return BlockMappingResult<UppercaseTextBlock>.Emit(state, state);
                }
            }

            return state.Text.Length > 0
                ? BlockMappingResult<UppercaseTextBlock>.Complete()
                : BlockMappingResult<UppercaseTextBlock>.Pass();
        }
    }
}
