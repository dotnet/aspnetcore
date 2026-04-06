// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Pipeline;

public class MediaContentHandlerTests
{
    private static BlockMappingPipeline CreatePipeline()
    {
        var options = new UIAgentOptions();
        return new BlockMappingPipeline(options);
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

    [Fact]
    public async Task DataContent_EmitsMediaContentBlock()
    {
        var pipeline = CreatePipeline();
        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var update = new ChatResponseUpdate
        {
            Role = ChatRole.User,
            MessageId = "msg-1",
            Contents = [new DataContent(imageBytes, "image/png")]
        };

        var blocks = await ProcessAsync(pipeline, update);

        Assert.Single(blocks);
        var block = Assert.IsType<MediaContentBlock>(blocks[0]);
        Assert.Single(block.Items);
        Assert.Equal("image/png", block.Items[0].MediaType);
    }

    [Fact]
    public async Task MultipleDataContents_SingleBlockAccumulates()
    {
        var pipeline = CreatePipeline();
        var update1 = new ChatResponseUpdate
        {
            Role = ChatRole.User,
            MessageId = "msg-1",
            Contents = [new DataContent(new byte[] { 1, 2, 3 }, "image/png")]
        };
        var update2 = new ChatResponseUpdate
        {
            Role = ChatRole.User,
            MessageId = "msg-1",
            Contents = [new DataContent(new byte[] { 4, 5, 6 }, "image/jpeg")]
        };

        var blocks1 = await ProcessAsync(pipeline, update1);
        Assert.Single(blocks1);
        await ProcessAsync(pipeline, update2);

        var block = Assert.IsType<MediaContentBlock>(blocks1[0]);
        Assert.Equal(2, block.Items.Count);
    }

    [Fact]
    public async Task TextAndDataContent_ProducesSeparateBlocks()
    {
        var pipeline = CreatePipeline();
        var update = new ChatResponseUpdate
        {
            Role = ChatRole.User,
            MessageId = "msg-1",
            Contents =
            [
                new TextContent("Check this image"),
                new DataContent(new byte[] { 1, 2, 3 }, "image/png")
            ]
        };

        var blocks = await ProcessAsync(pipeline, update);

        Assert.Equal(2, blocks.Count);
        Assert.IsType<MediaContentBlock>(blocks[0]);
        Assert.IsType<RichContentBlock>(blocks[1]);
    }

    [Fact]
    public async Task DataContentFollowedByText_CompletesMediaEmitsText()
    {
        var pipeline = CreatePipeline();

        var mediaUpdate = new ChatResponseUpdate
        {
            Role = ChatRole.User,
            MessageId = "msg-1",
            Contents = [new DataContent(new byte[] { 1, 2, 3 }, "image/png")]
        };
        var mediaBlocks = await ProcessAsync(pipeline, mediaUpdate);

        var textUpdate = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = "msg-2",
            Contents = [new TextContent("I see an image.")]
        };
        var textBlocks = await ProcessAsync(pipeline, textUpdate);

        var mediaBlock = Assert.IsType<MediaContentBlock>(mediaBlocks[0]);
        Assert.Equal(BlockLifecycleState.Inactive, mediaBlock.LifecycleState);

        Assert.Single(textBlocks);
        Assert.IsType<RichContentBlock>(textBlocks[0]);
    }

    [Fact]
    public async Task AudioDataContent_EmitsMediaContentBlock()
    {
        var pipeline = CreatePipeline();
        var update = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = "msg-1",
            Contents = [new DataContent(new byte[] { 0xFF, 0xFB }, "audio/mpeg")]
        };

        var blocks = await ProcessAsync(pipeline, update);

        Assert.Single(blocks);
        var block = Assert.IsType<MediaContentBlock>(blocks[0]);
        Assert.Equal("audio/mpeg", block.Items[0].MediaType);
    }

    [Fact]
    public async Task MediaBlock_SetsId_FromMessageId()
    {
        var pipeline = CreatePipeline();
        var update = new ChatResponseUpdate
        {
            Role = ChatRole.User,
            MessageId = "test-msg-id",
            Contents = [new DataContent(new byte[] { 1 }, "image/png")]
        };

        var blocks = await ProcessAsync(pipeline, update);

        var block = Assert.IsType<MediaContentBlock>(blocks[0]);
        Assert.Equal("test-msg-id", block.Id);
    }
}
