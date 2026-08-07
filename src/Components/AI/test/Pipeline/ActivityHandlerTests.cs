// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Pipeline;

public class ActivityHandlerTests
{
    private sealed class TestActivitySnapshot
    {
        public string Id { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public JsonElement Content { get; set; }
    }

    private sealed class TestActivityDelta
    {
        public string Id { get; set; } = string.Empty;
        public JsonElement Content { get; set; }
        public bool IsCompleted { get; set; }
    }

    private sealed class TestActivityHandler : ActivityHandler<ActivityContentBlock>
    {
        protected override bool TryCreateBlock(
            BlockMappingContext context, ActivityContentBlock state)
        {
            if (context.Update.RawRepresentation is not TestActivitySnapshot snapshot)
            {
                return false;
            }

            state.Id = snapshot.Id;
            state.ActivityType = snapshot.ActivityType;
            state.Content = snapshot.Content;
            context.MarkUpdateHandled();
            return true;
        }

        protected override bool TryUpdateBlock(
            BlockMappingContext context, ActivityContentBlock state, out bool isCompleted)
        {
            isCompleted = false;

            if (context.Update.RawRepresentation is TestActivitySnapshot snapshot
                && snapshot.Id == state.Id)
            {
                state.Content = snapshot.Content;
                context.MarkUpdateHandled();
                return true;
            }

            if (context.Update.RawRepresentation is TestActivityDelta delta
                && delta.Id == state.Id)
            {
                state.Content = delta.Content;
                isCompleted = delta.IsCompleted;
                context.MarkUpdateHandled();
                return true;
            }

            return false;
        }
    }

    private static BlockMappingPipeline CreatePipeline()
    {
        var options = new UIAgentOptions();
        options.AddBlockHandler(new TestActivityHandler());
        return new BlockMappingPipeline(options);
    }

    private static async Task<List<ContentBlock>> CollectBlocks(
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
    public async Task Snapshot_EmitsActivityContentBlock()
    {
        var pipeline = CreatePipeline();
        var content = JsonSerializer.SerializeToElement(new { goal = "test", steps = new[] { "step1" } });

        var update = new ChatResponseUpdate
        {
            RawRepresentation = new TestActivitySnapshot
            {
                Id = "act-1",
                ActivityType = "PLAN",
                Content = content
            }
        };

        var blocks = await CollectBlocks(pipeline, update);

        Assert.Single(blocks);
        var block = Assert.IsType<ActivityContentBlock>(blocks[0]);
        Assert.Equal("act-1", block.Id);
        Assert.Equal("PLAN", block.ActivityType);
        Assert.Equal(JsonValueKind.Object, block.Content.ValueKind);
        Assert.Equal(BlockLifecycleState.Active, block.LifecycleState);
    }

    [Fact]
    public async Task Delta_UpdatesContent()
    {
        var pipeline = CreatePipeline();

        var initialContent = JsonSerializer.SerializeToElement(new { progress = 0 });
        var snapshotUpdate = new ChatResponseUpdate
        {
            RawRepresentation = new TestActivitySnapshot
            {
                Id = "act-1",
                ActivityType = "SEARCH",
                Content = initialContent
            }
        };
        var blocks = await CollectBlocks(pipeline, snapshotUpdate);
        var block = Assert.IsType<ActivityContentBlock>(blocks[0]);

        var changed = false;
        block.OnChanged(() => changed = true);

        var updatedContent = JsonSerializer.SerializeToElement(new { progress = 50 });
        var deltaUpdate = new ChatResponseUpdate
        {
            RawRepresentation = new TestActivityDelta
            {
                Id = "act-1",
                Content = updatedContent
            }
        };
        await CollectBlocks(pipeline, deltaUpdate);

        Assert.Equal(50, block.Content.GetProperty("progress").GetInt32());
        Assert.True(changed);
    }

    [Fact]
    public async Task Delta_WithIsCompleted_CompletesBlock()
    {
        var pipeline = CreatePipeline();

        var content = JsonSerializer.SerializeToElement(new { progress = 0 });
        var snapshotUpdate = new ChatResponseUpdate
        {
            RawRepresentation = new TestActivitySnapshot
            {
                Id = "act-1",
                ActivityType = "PLAN",
                Content = content
            }
        };
        var blocks = await CollectBlocks(pipeline, snapshotUpdate);
        var block = Assert.IsType<ActivityContentBlock>(blocks[0]);

        var finalContent = JsonSerializer.SerializeToElement(new { progress = 100 });
        var deltaUpdate = new ChatResponseUpdate
        {
            RawRepresentation = new TestActivityDelta
            {
                Id = "act-1",
                Content = finalContent,
                IsCompleted = true
            }
        };
        await CollectBlocks(pipeline, deltaUpdate);

        Assert.Equal(BlockLifecycleState.Inactive, block.LifecycleState);
    }

    [Fact]
    public async Task NonMatchingUpdate_ReturnsPass()
    {
        var pipeline = CreatePipeline();

        var update = new ChatResponseUpdate
        {
            Contents = [new TextContent("hello")]
        };

        var blocks = await CollectBlocks(pipeline, update);

        // Text block emitted by TextBlockHandler, not activity
        var block = blocks.Single();
        Assert.IsNotType<ActivityContentBlock>(block);
    }

    [Fact]
    public async Task Delta_WithWrongId_NotClaimed()
    {
        var pipeline = CreatePipeline();

        var content = JsonSerializer.SerializeToElement(new { data = "a" });
        var snapshotUpdate = new ChatResponseUpdate
        {
            RawRepresentation = new TestActivitySnapshot
            {
                Id = "act-1",
                ActivityType = "PLAN",
                Content = content
            }
        };
        var blocks = await CollectBlocks(pipeline, snapshotUpdate);
        var block = Assert.IsType<ActivityContentBlock>(blocks[0]);

        var changed = false;
        block.OnChanged(() => changed = true);

        var wrongDelta = new ChatResponseUpdate
        {
            RawRepresentation = new TestActivityDelta
            {
                Id = "act-999",
                Content = JsonSerializer.SerializeToElement(new { data = "b" })
            }
        };
        await CollectBlocks(pipeline, wrongDelta);

        // Block should not have been updated
        Assert.Equal("a", block.Content.GetProperty("data").GetString());
        Assert.False(changed);
    }

    [Fact]
    public async Task ActivityBlockId_MatchesSnapshotId()
    {
        var pipeline = CreatePipeline();
        var content = JsonSerializer.SerializeToElement("test");

        var update = new ChatResponseUpdate
        {
            RawRepresentation = new TestActivitySnapshot
            {
                Id = "my-activity-42",
                ActivityType = "ANALYSIS",
                Content = content
            }
        };

        var blocks = await CollectBlocks(pipeline, update);
        var block = Assert.IsType<ActivityContentBlock>(blocks[0]);
        Assert.Equal("my-activity-42", block.Id);
    }

    [Fact]
    public async Task SnapshotUpdate_UpdatesExistingBlock()
    {
        var pipeline = CreatePipeline();

        var initial = JsonSerializer.SerializeToElement(new { step = 1 });
        var snapshotUpdate1 = new ChatResponseUpdate
        {
            RawRepresentation = new TestActivitySnapshot
            {
                Id = "act-1",
                ActivityType = "PLAN",
                Content = initial
            }
        };
        await CollectBlocks(pipeline, snapshotUpdate1);

        var updated = JsonSerializer.SerializeToElement(new { step = 2 });
        var snapshotUpdate2 = new ChatResponseUpdate
        {
            RawRepresentation = new TestActivitySnapshot
            {
                Id = "act-1",
                ActivityType = "PLAN",
                Content = updated
            }
        };
        var blocks2 = await CollectBlocks(pipeline, snapshotUpdate2);

        // No new blocks emitted
        Assert.Empty(blocks2);
    }

    [Fact]
    public async Task Finalize_ActiveActivityBlock_TransitionsToInactive()
    {
        var pipeline = CreatePipeline();
        var content = JsonSerializer.SerializeToElement("pending");

        var update = new ChatResponseUpdate
        {
            RawRepresentation = new TestActivitySnapshot
            {
                Id = "act-1",
                ActivityType = "SEARCH",
                Content = content
            }
        };
        var blocks = await CollectBlocks(pipeline, update);
        var block = Assert.IsType<ActivityContentBlock>(blocks[0]);
        Assert.Equal(BlockLifecycleState.Active, block.LifecycleState);

        pipeline.Finalize();

        Assert.Equal(BlockLifecycleState.Inactive, block.LifecycleState);
    }
}

public class ActivityHandlerOnContentUpdatedTests
{
    private sealed class TestSnapshot
    {
        public string Id { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public JsonElement Content { get; set; }
    }

    private sealed class TestDelta
    {
        public string Id { get; set; } = string.Empty;
        public JsonElement Content { get; set; }
    }

    private class PlanningActivityBlock : ActivityContentBlock
    {
        public string Goal { get; set; } = string.Empty;
        public List<string> Steps { get; set; } = new();
    }

    private sealed class PlanningActivityHandler : ActivityHandler<PlanningActivityBlock>
    {
        public int OnContentUpdatedCallCount { get; private set; }

        protected override bool TryCreateBlock(
            BlockMappingContext context, PlanningActivityBlock state)
        {
            if (context.Update.RawRepresentation is not TestSnapshot snapshot)
            {
                return false;
            }

            state.Id = snapshot.Id;
            state.ActivityType = snapshot.ActivityType;
            state.Content = snapshot.Content;
            context.MarkUpdateHandled();
            return true;
        }

        protected override bool TryUpdateBlock(
            BlockMappingContext context, PlanningActivityBlock state, out bool isCompleted)
        {
            isCompleted = false;

            if (context.Update.RawRepresentation is TestDelta delta
                && delta.Id == state.Id)
            {
                state.Content = delta.Content;
                context.MarkUpdateHandled();
                return true;
            }

            return false;
        }

        protected override void OnContentUpdated(PlanningActivityBlock state)
        {
            OnContentUpdatedCallCount++;
            if (state.Content.ValueKind != JsonValueKind.Undefined)
            {
                if (state.Content.TryGetProperty("goal", out var goal))
                {
                    state.Goal = goal.GetString() ?? string.Empty;
                }

                if (state.Content.TryGetProperty("steps", out var steps))
                {
                    state.Steps = steps.Deserialize<List<string>>() ?? new();
                }
            }
        }
    }

    private static async Task<List<ContentBlock>> CollectBlocks(
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
    public async Task OnContentUpdated_CalledAfterCreate()
    {
        var handler = new PlanningActivityHandler();
        var options = new UIAgentOptions();
        options.AddBlockHandler(handler);
        var pipeline = new BlockMappingPipeline(options);

        var content = JsonSerializer.SerializeToElement(new
        {
            goal = "Build feature",
            steps = new[] { "Design", "Implement", "Test" }
        });

        var update = new ChatResponseUpdate
        {
            RawRepresentation = new TestSnapshot
            {
                Id = "act-1",
                ActivityType = "PLAN",
                Content = content
            }
        };

        var blocks = await CollectBlocks(pipeline, update);
        var block = Assert.IsType<PlanningActivityBlock>(blocks[0]);

        Assert.Equal(1, handler.OnContentUpdatedCallCount);
        Assert.Equal("Build feature", block.Goal);
        Assert.Equal(3, block.Steps.Count);
        Assert.Equal("Design", block.Steps[0]);
    }

    [Fact]
    public async Task OnContentUpdated_CalledAfterUpdate()
    {
        var handler = new PlanningActivityHandler();
        var options = new UIAgentOptions();
        options.AddBlockHandler(handler);
        var pipeline = new BlockMappingPipeline(options);

        var initialContent = JsonSerializer.SerializeToElement(new
        {
            goal = "Build feature",
            steps = new[] { "Design" }
        });

        var createUpdate = new ChatResponseUpdate
        {
            RawRepresentation = new TestSnapshot
            {
                Id = "act-1",
                ActivityType = "PLAN",
                Content = initialContent
            }
        };
        var blocks = await CollectBlocks(pipeline, createUpdate);
        var block = Assert.IsType<PlanningActivityBlock>(blocks[0]);

        var updatedContent = JsonSerializer.SerializeToElement(new
        {
            goal = "Build feature",
            steps = new[] { "Design", "Implement", "Test", "Deploy" }
        });

        var deltaUpdate = new ChatResponseUpdate
        {
            RawRepresentation = new TestDelta
            {
                Id = "act-1",
                Content = updatedContent
            }
        };
        await CollectBlocks(pipeline, deltaUpdate);

        Assert.Equal(2, handler.OnContentUpdatedCallCount);
        Assert.Equal(4, block.Steps.Count);
        Assert.Equal("Deploy", block.Steps[3]);
    }
}
