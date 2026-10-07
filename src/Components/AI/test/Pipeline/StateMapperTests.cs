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
                    return;
                }

                context.SetState(state);
            };
        });
        var callbackCount = 0;
        agent.State.OnChanged(() => callbackCount++);

        await CollectBlocksAsync(agent);

        Assert.Equal("Observable state", agent.State.Value.Title);
        Assert.Equal(1, callbackCount);
    }

    [Fact]
    public void TypedOptionsExposeAgentState()
    {
        var initialState = new RecipeState { Title = "Initial state" };
        UIAgentOptions<RecipeState>? configuredOptions = null;

        var agent = new UIAgent<RecipeState>(
            new DelegatingStreamingChatClient(),
            options => configuredOptions = options,
            initialState);

        Assert.NotNull(configuredOptions);
        Assert.Same(agent.State, configuredOptions.State);
        Assert.Same(initialState, configuredOptions.State.Value);

        var updatedState = new RecipeState { Title = "Updated state" };
        agent.State.Value = updatedState;

        Assert.Same(updatedState, configuredOptions.State.Value);
    }

    [Fact]
    public void TypedAgentAcceptsBaseOptionsConfiguration()
    {
        UIAgentOptions? configuredOptions = null;
        Action<UIAgentOptions> configure = options => configuredOptions = options;

        var agent = new UIAgent<RecipeState>(
            new DelegatingStreamingChatClient(),
            configure);

        Assert.IsType<UIAgentOptions<RecipeState>>(configuredOptions);
        Assert.Same(agent.State, ((UIAgentOptions<RecipeState>)configuredOptions).State);
    }

    [Fact]
    public void StateMapper_FilteredUpdatePreservesMetadata()
    {
        var agent = CreateAgent(new DelegatingStreamingChatClient());
        var rawRepresentation = new object();
        var additionalProperties = new AdditionalPropertiesDictionary
        {
            ["property"] = "value",
        };
        var continuationToken = ResponseContinuationToken.FromBytes(new byte[] { 1, 2, 3 });
        var createdAt = new DateTimeOffset(2026, 8, 26, 8, 0, 0, TimeSpan.Zero);
        var stateContent = new StateContent
        {
            StateValue = new RecipeState { Title = "Pasta" },
        };
        var update = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            AuthorName = "Test assistant",
            MessageId = "message-1",
            ResponseId = "response-1",
            ConversationId = "conversation-1",
            CreatedAt = createdAt,
            FinishReason = ChatFinishReason.Stop,
            ModelId = "test-model",
            ContinuationToken = continuationToken,
            RawRepresentation = rawRepresentation,
            AdditionalProperties = additionalProperties,
            Contents = [stateContent, new TextContent("Enjoy this recipe!")],
        };

        var mappedUpdate = agent.ApplyStateMapper(update);

        Assert.Equal(update.Role, mappedUpdate.Role);
        Assert.Equal(update.AuthorName, mappedUpdate.AuthorName);
        Assert.Equal(update.MessageId, mappedUpdate.MessageId);
        Assert.Equal(update.ResponseId, mappedUpdate.ResponseId);
        Assert.Equal(update.ConversationId, mappedUpdate.ConversationId);
        Assert.Equal(createdAt, mappedUpdate.CreatedAt);
        Assert.Equal(update.FinishReason, mappedUpdate.FinishReason);
        Assert.Equal(update.ModelId, mappedUpdate.ModelId);
        Assert.Same(continuationToken, mappedUpdate.ContinuationToken);
        Assert.Same(rawRepresentation, mappedUpdate.RawRepresentation);
        Assert.Same(additionalProperties, mappedUpdate.AdditionalProperties);
        Assert.DoesNotContain(stateContent, mappedUpdate.Contents);
    }

    [Fact]
    public void StateMapper_IncompatibleStateTypeThrows()
    {
        var agent = new UIAgent<RecipeState>(
            new DelegatingStreamingChatClient(),
            options => options.StateMapper = context => context.SetState(new object()));

        var exception = Assert.Throws<InvalidOperationException>(
            () => agent.ApplyStateMapper(new ChatResponseUpdate()));

        Assert.Contains(typeof(RecipeState).ToString(), exception.Message);
    }

    [Fact]
    public async Task StateMapper_HandledContentIsFilteredFromHistory()
    {
        var client = new DelegatingStreamingChatClient();
        List<ChatMessage>? secondRequest = null;
        var callCount = 0;
        client.SetHandler((messages, _, cancellationToken) =>
        {
            callCount++;
            if (callCount == 2)
            {
                secondRequest = messages.ToList();
            }

            return callCount == 1
                ? EmitMixed(cancellationToken)
                : EmitRawState(cancellationToken);
        });
        var agent = CreateAgent(client);

        await CollectBlocksAsync(agent);
        await CollectBlocksAsync(agent);

        Assert.NotNull(secondRequest);
        var assistantMessage = Assert.Single(
            secondRequest.Where(message => message.Role == ChatRole.Assistant));
        Assert.Collection(
            assistantMessage.Contents,
            content => Assert.IsType<TextContent>(content));
    }

    [Fact]
    public async Task StateMapper_FullyHandledUpdatePreservesMessageIdentityForLaterText()
    {
        var client = new DelegatingStreamingChatClient();
        List<ChatMessage>? secondRequest = null;
        var callCount = 0;
        client.SetHandler((messages, _, cancellationToken) =>
        {
            callCount++;
            if (callCount == 2)
            {
                secondRequest = messages.ToList();
            }

            return callCount == 1
                ? EmitHandledStateThenUnidentifiedText(cancellationToken)
                : EmitRawState(cancellationToken);
        });
        var agent = CreateAgent(client);

        await CollectBlocksAsync(agent);
        await CollectBlocksAsync(agent);

        Assert.NotNull(secondRequest);
        var assistantMessage = Assert.Single(
            secondRequest.Where(message => message.Role == ChatRole.Assistant));
        var textContent = Assert.IsType<TextContent>(Assert.Single(assistantMessage.Contents));
        Assert.Equal("Enjoy this recipe!", textContent.Text);
        Assert.Equal("message-1", assistantMessage.MessageId);
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
                        return;
                    }
                }
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

    private static async IAsyncEnumerable<ChatResponseUpdate> EmitHandledStateThenUnidentifiedText(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = "message-1",
            ResponseId = "response-1",
            Contents =
            [
                new StateContent
                {
                    StateValue = new RecipeState { Title = "Pasta" },
                },
            ],
        };
        yield return new ChatResponseUpdate
        {
            Contents = [new TextContent("Enjoy this recipe!")],
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
