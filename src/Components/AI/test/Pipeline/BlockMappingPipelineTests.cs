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
    public async Task Process_RichTextSnapshots_ReplaceTreeOnSameBlock()
    {
        var pipeline = CreatePipeline();
        var firstNodes = new RichTextNode[]
        {
            CreateNode<HeadingNode>(new TextNode("Partial")),
        };
        var finalNodes = new RichTextNode[]
        {
            CreateNode<HeadingNode>(new TextNode("Complete")),
            CreateNode<ParagraphNode>(new TextNode("Formatted response")),
        };

        var blocks = await ProcessAsync(
            pipeline,
            CreateRichTextUpdate("msg-1", "Partial", firstNodes));
        var moreBlocks = await ProcessAsync(
            pipeline,
            CreateRichTextUpdate("msg-1", "Complete\n\nFormatted response", finalNodes));

        Assert.Empty(moreBlocks);
        var block = Assert.IsType<RichContentBlock>(Assert.Single(blocks));
        Assert.Equal("Complete\n\nFormatted response", block.RawText);
        Assert.Equal(finalNodes, block.Content);
        Assert.Equal("Partial", Assert.IsType<TextNode>(Assert.Single(firstNodes[0].Children)).Text);
    }

    [Fact]
    public async Task Process_RichTextSnapshot_NotifiesSubscribers()
    {
        var pipeline = CreatePipeline();
        var blocks = await ProcessAsync(
            pipeline,
            CreateRichTextUpdate(
                "msg-1",
                "Partial",
                [CreateNode<ParagraphNode>(new TextNode("Partial"))]));
        var changeCount = 0;
        using var subscription = blocks[0].OnChanged(() => changeCount++);

        await ProcessAsync(
            pipeline,
            CreateRichTextUpdate(
                "msg-1",
                "Complete",
                [CreateNode<ParagraphNode>(new TextNode("Complete"))]));

        Assert.Equal(1, changeCount);
    }

    [Fact]
    public async Task Process_RichTextSnapshotsWithDifferentMessageIds_EmitSeparateBlocks()
    {
        var pipeline = CreatePipeline();

        var firstBlocks = await ProcessAsync(
            pipeline,
            CreateRichTextUpdate(
                "msg-1",
                "First",
                [CreateNode<ParagraphNode>(new TextNode("First"))]));
        var secondBlocks = await ProcessAsync(
            pipeline,
            CreateRichTextUpdate(
                "msg-2",
                "Second",
                [CreateNode<ParagraphNode>(new TextNode("Second"))]));

        var firstBlock = Assert.IsType<RichContentBlock>(Assert.Single(firstBlocks));
        var secondBlock = Assert.IsType<RichContentBlock>(Assert.Single(secondBlocks));
        Assert.Equal("msg-1", firstBlock.Id);
        Assert.Equal("First", firstBlock.RawText);
        Assert.Equal(BlockLifecycleState.Inactive, firstBlock.LifecycleState);
        Assert.Equal("msg-2", secondBlock.Id);
        Assert.Equal("Second", secondBlock.RawText);
    }

    [Fact]
    public async Task Process_RichTextSnapshotWithTextFallback_EmitsSingleBlock()
    {
        var pipeline = CreatePipeline();
        var update = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = "msg-1",
            Contents =
            [
                new RichTextContent(
                    "Formatted",
                    [CreateNode<ParagraphNode>(new TextNode("Formatted"))]),
                new TextContent("Formatted"),
            ],
        };

        var blocks = await ProcessAsync(pipeline, update);

        var block = Assert.IsType<RichContentBlock>(Assert.Single(blocks));
        Assert.Equal("Formatted", block.RawText);
        Assert.IsType<ParagraphNode>(Assert.Single(block.Content));
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

    private static ChatResponseUpdate CreateRichTextUpdate(
        string messageId,
        string text,
        IReadOnlyList<RichTextNode> nodes) => new()
        {
            Role = ChatRole.Assistant,
            MessageId = messageId,
            Contents = [new RichTextContent(text, nodes)],
        };

    private static TNode CreateNode<TNode>(params RichTextNode[] children)
        where TNode : RichTextNode, new()
    {
        var node = new TNode();
        foreach (var child in children)
        {
            node.AddChild(child);
        }

        return node;
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
