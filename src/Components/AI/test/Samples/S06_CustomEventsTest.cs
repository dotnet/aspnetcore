// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.AI.Tests.Pipeline;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Samples;

public class S06_CustomEventsTest
{
    // AG-UI "custom events" map to custom ContentBlockHandlers in components-ai.
    // A handler inspects RawRepresentation on ChatResponseUpdate and emits a domain block.

    private sealed class NotificationPayload
    {
        public string Level { get; set; } = "";
        public string Message { get; set; } = "";
    }

    private sealed class NotificationBlock : ContentBlock
    {
        public string Level { get; set; } = "";
        public string Message { get; set; } = "";
    }

    [Fact]
    public async Task CustomHandler_EmitsDomainBlock()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) => EmitNotificationThenText(ct));

        var agent = new UIAgent(client, options =>
        {
            options.AddBlockHandler(new DelegateBlockHandler<NotificationBlock>((context, state) =>
            {
                if (context.Update.RawRepresentation is not NotificationPayload payload)
                {
                    return BlockMappingResult<NotificationBlock>.Pass();
                }
                context.MarkUpdateHandled();
                state.Id = Guid.NewGuid().ToString("N");
                state.Level = payload.Level;
                state.Message = payload.Message;
                return BlockMappingResult<NotificationBlock>.Emit(state, state);
            }));
        });

        var context = new AgentContext(agent);
        await context.SendMessageAsync("Run task");

        var turn = context.Turns[0];
        var notification = turn.ResponseBlocks.OfType<NotificationBlock>().Single();
        Assert.Equal("info", notification.Level);
        Assert.Equal("Task started", notification.Message);

        var textBlock = turn.ResponseBlocks.OfType<RichContentBlock>().Single();
        Assert.Equal("Task complete.", textBlock.RawText);
    }

    [Fact]
    public async Task CustomHandler_UnmatchedUpdates_FallThroughToBuiltIn()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) => EmitOnlyText(ct));

        var agent = new UIAgent(client, options =>
        {
            options.AddBlockHandler(new DelegateBlockHandler<NotificationBlock>((context, state) =>
            {
                if (context.Update.RawRepresentation is not NotificationPayload)
                {
                    return BlockMappingResult<NotificationBlock>.Pass();
                }
                context.MarkUpdateHandled();
                state.Id = Guid.NewGuid().ToString("N");
                return BlockMappingResult<NotificationBlock>.Emit(state, state);
            }));
        });

        var context = new AgentContext(agent);
        await context.SendMessageAsync("Say hello");

        var turn = context.Turns[0];
        Assert.Empty(turn.ResponseBlocks.OfType<NotificationBlock>());
        var textBlock = turn.ResponseBlocks.OfType<RichContentBlock>().Single();
        Assert.Equal("Hello!", textBlock.RawText);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> EmitNotificationThenText(
        [EnumeratorCancellation] CancellationToken ct)
    {
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            RawRepresentation = new NotificationPayload { Level = "info", Message = "Task started" }
        };
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = Guid.NewGuid().ToString("N"),
            Contents = [new TextContent("Task complete.")]
        };
        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> EmitOnlyText(
        [EnumeratorCancellation] CancellationToken ct)
    {
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = Guid.NewGuid().ToString("N"),
            Contents = [new TextContent("Hello!")]
        };
        await Task.CompletedTask;
    }
}
