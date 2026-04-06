// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Pipeline;

public class StateMapperTests
{
    private class RecipeState
    {
        public string Title { get; set; } = "";
        public string Cuisine { get; set; } = "";
    }

    private class StateContent : AIContent
    {
        public object StateValue { get; set; } = new();
    }

    [Fact]
    public async Task StateMapper_ExtractsState_FiltersFromPipeline()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) => EmitStateAndText(ct));

        var agent = new UIAgent<RecipeState>(client,
            configure: options =>
            {
                options.StateMapper = ctx =>
                {
                    foreach (var content in ctx.UnhandledContents)
                    {
                        if (content is StateContent sc)
                        {
                            ctx.MarkHandled(sc);
                            ctx.SetState(sc.StateValue);
                            return true;
                        }
                    }
                    return false;
                };
            });

        var blocks = new List<ContentBlock>();
        await foreach (var block in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "Give me a recipe")))
        {
            blocks.Add(block);
        }

        Assert.Equal("Spaghetti Carbonara", agent.State.Value.Title);

        var assistantBlocks = blocks.Where(b => b.Role == ChatRole.Assistant).ToList();
        Assert.All(assistantBlocks, b => Assert.IsType<RichContentBlock>(b));

        static async IAsyncEnumerable<ChatResponseUpdate> EmitStateAndText(
            [EnumeratorCancellation] CancellationToken ct)
        {
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                Contents = [new StateContent
                {
                    StateValue = new RecipeState
                    {
                        Title = "Spaghetti Carbonara",
                        Cuisine = "Italian"
                    }
                }]
            };
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                MessageId = "msg-1",
                Contents = [new TextContent("Here's a classic Italian recipe!")]
            };
            await Task.CompletedTask;
        }
    }

    [Fact]
    public async Task StateMapper_MixedContentInOneUpdate_OnlyStateFiltered()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) => EmitMixed(ct));

        var agent = new UIAgent<RecipeState>(client,
            configure: options =>
            {
                options.StateMapper = ctx =>
                {
                    foreach (var content in ctx.UnhandledContents)
                    {
                        if (content is StateContent sc)
                        {
                            ctx.MarkHandled(sc);
                            ctx.SetState(sc.StateValue);
                            return true;
                        }
                    }
                    return false;
                };
            });

        var blocks = new List<ContentBlock>();
        await foreach (var block in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "Recipe?")))
        {
            blocks.Add(block);
        }

        var textBlocks = blocks.OfType<RichContentBlock>()
            .Where(b => b.Role == ChatRole.Assistant).ToList();
        Assert.NotEmpty(textBlocks);

        Assert.Equal("Pasta", agent.State.Value.Title);

        static async IAsyncEnumerable<ChatResponseUpdate> EmitMixed(
            [EnumeratorCancellation] CancellationToken ct)
        {
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                MessageId = "msg-1",
                Contents =
                [
                    new StateContent { StateValue = new RecipeState { Title = "Pasta" } },
                    new TextContent("Enjoy this recipe!")
                ]
            };
            await Task.CompletedTask;
        }
    }

    [Fact]
    public async Task NoStateMapper_AllContentFlowsToPipeline()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Just text"));

        var agent = new UIAgent(client);

        var blocks = new List<ContentBlock>();
        await foreach (var block in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "hi")))
        {
            blocks.Add(block);
        }

        var assistantBlocks = blocks.Where(b => b.Role == ChatRole.Assistant).ToList();
        Assert.NotEmpty(assistantBlocks);
    }

    [Fact]
    public void AgentState_OnChanged_FiresWhenStateMapperUpdatesValue()
    {
        var state = new AgentState<RecipeState>();
        var changed = false;
        state.OnChanged(() => changed = true);

        state.Value = new RecipeState { Title = "Test" };

        Assert.True(changed);
    }
}
