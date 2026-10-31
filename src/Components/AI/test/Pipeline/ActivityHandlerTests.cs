// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.AI.Tests.Pipeline;

public class ActivityHandlerTests
{
    [Fact]
    public async Task Snapshot_EmitsActiveActivityBlock()
    {
        var pipeline = CreatePipeline();

        var blocks = await CollectBlocksAsync(
            pipeline,
            new ActivitySnapshot
            {
                Id = "activity-1",
                ActivityType = "PLAN",
                Content = JsonSerializer.SerializeToElement(new { progress = 0 }),
            });

        var block = Assert.IsType<ActivityContentBlock>(Assert.Single(blocks));
        Assert.Equal("activity-1", block.Id);
        Assert.Equal("PLAN", block.ActivityType);
        Assert.Equal(0, block.Content.GetProperty("progress").GetInt32());
        Assert.Equal(BlockLifecycleState.Active, block.LifecycleState);
    }

    [Fact]
    public async Task Delta_UpdatesExistingBlockAndNotifies()
    {
        var pipeline = CreatePipeline();
        var block = Assert.IsType<ActivityContentBlock>(Assert.Single(
            await CollectBlocksAsync(
                pipeline,
                new ActivitySnapshot
                {
                    Id = "activity-1",
                    ActivityType = "PLAN",
                    Content = JsonSerializer.SerializeToElement(new { progress = 0 }),
                })));
        var callbackCount = 0;
        block.OnChanged(() => callbackCount++);

        var emitted = await CollectBlocksAsync(
            pipeline,
            new ActivityDelta
            {
                Id = "activity-1",
                Content = JsonSerializer.SerializeToElement(new { progress = 50 }),
            });

        Assert.Empty(emitted);
        Assert.Equal(50, block.Content.GetProperty("progress").GetInt32());
        Assert.Equal(1, callbackCount);
        Assert.Equal(BlockLifecycleState.Active, block.LifecycleState);
    }

    [Fact]
    public async Task CompletingDelta_MarksExistingBlockInactive()
    {
        var pipeline = CreatePipeline();
        var block = Assert.IsType<ActivityContentBlock>(Assert.Single(
            await CollectBlocksAsync(
                pipeline,
                new ActivitySnapshot
                {
                    Id = "activity-1",
                    ActivityType = "PLAN",
                    Content = JsonSerializer.SerializeToElement(new { progress = 0 }),
                })));

        await CollectBlocksAsync(
            pipeline,
            new ActivityDelta
            {
                Id = "activity-1",
                Content = JsonSerializer.SerializeToElement(new { progress = 100 }),
                IsCompleted = true,
            });

        Assert.Equal(100, block.Content.GetProperty("progress").GetInt32());
        Assert.Equal(BlockLifecycleState.Inactive, block.LifecycleState);
    }

    [Fact]
    public async Task Delta_ForDifferentActivityDoesNotUpdateBlock()
    {
        var pipeline = CreatePipeline();
        var block = Assert.IsType<ActivityContentBlock>(Assert.Single(
            await CollectBlocksAsync(
                pipeline,
                new ActivitySnapshot
                {
                    Id = "activity-1",
                    ActivityType = "PLAN",
                    Content = JsonSerializer.SerializeToElement(new { progress = 0 }),
                })));

        await CollectBlocksAsync(
            pipeline,
            new ActivityDelta
            {
                Id = "activity-2",
                Content = JsonSerializer.SerializeToElement(new { progress = 50 }),
            });

        Assert.Equal(0, block.Content.GetProperty("progress").GetInt32());
    }

    [Fact]
    public async Task Snapshot_HandlerDoesNotAssignId_AssignsFallbackId()
    {
        var pipeline = CreatePipeline(assignId: false);

        var block = Assert.IsType<ActivityContentBlock>(Assert.Single(
            await CollectBlocksAsync(
                pipeline,
                new ActivitySnapshot
                {
                    Id = "activity-1",
                    ActivityType = "PLAN",
                    Content = JsonSerializer.SerializeToElement(new { progress = 0 }),
                })));

        Assert.NotEmpty(block.Id);
    }

    private static BlockMappingPipeline CreatePipeline(bool assignId = true)
    {
        var options = new UIAgentOptions();
        options.AddBlockHandler(new TestActivityHandler(assignId));
        return new BlockMappingPipeline(options);
    }

    private static async Task<List<ContentBlock>> CollectBlocksAsync(
        BlockMappingPipeline pipeline,
        object rawRepresentation)
    {
        var blocks = new List<ContentBlock>();
        await foreach (var block in pipeline.Process(
            new Microsoft.Extensions.AI.ChatResponseUpdate
            {
                RawRepresentation = rawRepresentation,
            }))
        {
            blocks.Add(block);
        }

        return blocks;
    }

    private sealed class TestActivityHandler : ActivityHandler<ActivityContentBlock>
    {
        private readonly bool _assignId;

        public TestActivityHandler(bool assignId)
        {
            _assignId = assignId;
        }

        protected override bool TryCreateBlock(
            BlockMappingContext context,
            ActivityContentBlock state)
        {
            if (context.Update.RawRepresentation is not ActivitySnapshot snapshot)
            {
                return false;
            }

            if (_assignId)
            {
                state.Id = snapshot.Id;
            }

            state.ActivityType = snapshot.ActivityType;
            state.Content = snapshot.Content;
            context.MarkUpdateHandled();
            return true;
        }

        protected override bool TryUpdateBlock(
            BlockMappingContext context,
            ActivityContentBlock state,
            out bool isCompleted)
        {
            isCompleted = false;
            if (context.Update.RawRepresentation is not ActivityDelta delta ||
                delta.Id != state.Id)
            {
                return false;
            }

            state.Content = delta.Content;
            isCompleted = delta.IsCompleted;
            context.MarkUpdateHandled();
            return true;
        }
    }

    private sealed class ActivitySnapshot
    {
        public required string Id { get; init; }

        public required string ActivityType { get; init; }

        public JsonElement Content { get; init; }
    }

    private sealed class ActivityDelta
    {
        public required string Id { get; init; }

        public JsonElement Content { get; init; }

        public bool IsCompleted { get; init; }
    }
}
