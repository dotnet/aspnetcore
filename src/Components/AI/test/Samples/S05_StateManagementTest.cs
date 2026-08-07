// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Samples;

public class S05_StateManagementTest
{
    private sealed class RecipeState
    {
        public string Title { get; set; } = string.Empty;
        public string Cuisine { get; set; } = string.Empty;
        public List<string> Ingredients { get; set; } = [];
    }

    [Fact]
    public async Task StateMapper_ExtractsStructuredState()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) => EmitStateResponse(ct));

        var agent = new UIAgent<RecipeState>(client, options =>
        {
            options.StateMapper = context =>
            {
                foreach (var content in context.UnhandledContents)
                {
                    if (content is TextContent tc && tc.Text?.StartsWith("{", StringComparison.Ordinal) == true)
                    {
                        var state = JsonSerializer.Deserialize<RecipeState>(tc.Text);
                        if (state is not null)
                        {
                            context.MarkHandled(content);
                            context.SetState(state);
                            return true;
                        }
                    }
                }
                return false;
            };
        });

        var context = new AgentContext(agent);

        await context.SendMessageAsync("Make me a pasta recipe");

        Assert.Equal("Spaghetti Carbonara", agent.State.Value.Title);
        Assert.Equal("Italian", agent.State.Value.Cuisine);
        Assert.Contains("Spaghetti", agent.State.Value.Ingredients);
    }

    [Fact]
    public async Task StateMapper_FiresOnChanged()
    {
        var changeCount = 0;
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) => EmitStateResponse(ct));

        var agent = new UIAgent<RecipeState>(client, options =>
        {
            options.StateMapper = context =>
            {
                foreach (var content in context.UnhandledContents)
                {
                    if (content is TextContent tc && tc.Text?.StartsWith("{", StringComparison.Ordinal) == true)
                    {
                        var state = JsonSerializer.Deserialize<RecipeState>(tc.Text);
                        if (state is not null)
                        {
                            context.MarkHandled(content);
                            context.SetState(state);
                            return true;
                        }
                    }
                }
                return false;
            };
        });
        agent.State.OnChanged(() => changeCount++);

        var context = new AgentContext(agent);
        await context.SendMessageAsync("Make me a recipe");

        Assert.True(changeCount > 0);
    }

    [Fact]
    public async Task StateMapper_ConsumedContent_NotInBlocks()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) => EmitMixedResponse(ct));

        var agent = new UIAgent<RecipeState>(client, options =>
        {
            options.StateMapper = context =>
            {
                foreach (var content in context.UnhandledContents)
                {
                    if (content is TextContent tc && tc.Text?.StartsWith("{", StringComparison.Ordinal) == true)
                    {
                        var state = JsonSerializer.Deserialize<RecipeState>(tc.Text);
                        if (state is not null)
                        {
                            context.MarkHandled(content);
                            context.SetState(state);
                            return true;
                        }
                    }
                }
                return false;
            };
        });

        var context = new AgentContext(agent);
        await context.SendMessageAsync("Recipe please");

        // State mapper consumed the JSON, visible block has only the text
        var textBlock = context.Turns[0].ResponseBlocks.OfType<RichContentBlock>().Single();
        Assert.Equal("Here's your recipe!", textBlock.RawText);
        Assert.Equal("Spaghetti Carbonara", agent.State.Value.Title);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> EmitStateResponse(
        [EnumeratorCancellation] CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(new RecipeState
        {
            Title = "Spaghetti Carbonara",
            Cuisine = "Italian",
            Ingredients = ["Spaghetti", "Eggs", "Pancetta", "Parmesan"]
        });

        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = Guid.NewGuid().ToString("N"),
            Contents = [new TextContent(json)]
        };
        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> EmitMixedResponse(
        [EnumeratorCancellation] CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(new RecipeState
        {
            Title = "Spaghetti Carbonara",
            Cuisine = "Italian",
            Ingredients = ["Spaghetti", "Eggs"]
        });

        var messageId = Guid.NewGuid().ToString("N");
        // First update: state JSON (consumed by mapper)
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = messageId,
            Contents = [new TextContent(json)]
        };
        // Second update: visible text (not consumed)
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = Guid.NewGuid().ToString("N"),
            Contents = [new TextContent("Here's your recipe!")]
        };
        await Task.CompletedTask;
    }
}
