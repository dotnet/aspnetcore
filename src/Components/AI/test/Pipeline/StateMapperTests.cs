// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Pipeline;

public class StateMapperTests
{
    [Fact]
    public async Task StateMapper_ExtractsStateAndFiltersItFromBlocks()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((_, _, cancellationToken) => EmitStateAndText(cancellationToken));
        var agent = CreateAgent(client);

        var blocks = await CollectBlocksAsync(agent);

        Assert.Equal("Spaghetti Carbonara", agent.State.Value.Title);
        var assistantBlocks = blocks
            .Where(block => block.Role == ChatRole.Assistant)
            .ToList();
        Assert.Single(assistantBlocks);
        Assert.IsType<RichContentBlock>(assistantBlocks[0]);
    }

    [Fact]
    public async Task StateMapper_MixedUpdatePreservesUnhandledText()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((_, _, cancellationToken) => EmitMixed(cancellationToken));
        var agent = CreateAgent(client);

        var blocks = await CollectBlocksAsync(agent);

        Assert.Equal("Pasta", agent.State.Value.Title);
        var textBlock = Assert.Single(blocks
            .OfType<RichContentBlock>()
            .Where(block => block.Role == ChatRole.Assistant));
        Assert.Equal("Enjoy this recipe!", textBlock.RawText);
    }

    [Fact]
    public async Task StateMapper_RawUpdateChangesStateAndNotifies()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((_, _, cancellationToken) => EmitRawState(cancellationToken));
        var agent = new UIAgent<RecipeState>(client, options =>
        {
            options.StateMapper = context =>
            {
                if (context.Update.RawRepresentation is not RecipeState state)
                {
                    return false;
                }

                context.SetState(state);
                return true;
            };
        });
        var callbackCount = 0;
        agent.State.OnChanged(() => callbackCount++);

        await CollectBlocksAsync(agent);

        Assert.Equal("Observable state", agent.State.Value.Title);
        Assert.Equal(1, callbackCount);
    }

    private static UIAgent<RecipeState> CreateAgent(IChatClient client)
    {
        return new UIAgent<RecipeState>(client, options =>
        {
            options.StateMapper = context =>
            {
                foreach (var content in context.UnhandledContents)
                {
                    if (content is StateContent stateContent)
                    {
                        context.MarkHandled(stateContent);
                        context.SetState(stateContent.StateValue);
                        return true;
                    }
                }

                return false;
            };
        });
    }

    private static async Task<List<ContentBlock>> CollectBlocksAsync(
        UIAgent agent)
    {
        var blocks = new List<ContentBlock>();
        await foreach (var block in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "Give me a recipe")))
        {
            blocks.Add(block);
        }

        return blocks;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> EmitStateAndText(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents =
            [
                new StateContent
                {
                    StateValue = new RecipeState
                    {
                        Title = "Spaghetti Carbonara",
                    },
                },
            ],
        };
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = "message-1",
            Contents = [new TextContent("Here's a classic Italian recipe!")],
        };

        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> EmitMixed(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = "message-1",
            Contents =
            [
                new StateContent
                {
                    StateValue = new RecipeState { Title = "Pasta" },
                },
                new TextContent("Enjoy this recipe!"),
            ],
        };

        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> EmitRawState(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            RawRepresentation = new RecipeState { Title = "Observable state" },
        };

        await Task.CompletedTask;
    }

    private sealed class RecipeState
    {
        public string Title { get; set; } = "";
    }

    private sealed class StateContent : AIContent
    {
        public object StateValue { get; set; } = new();
    }
}
