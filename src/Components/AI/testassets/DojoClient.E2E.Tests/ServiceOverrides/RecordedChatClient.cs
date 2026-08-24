// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Extensions.AI;

namespace DojoClient.E2E.Tests.ServiceOverrides;

// Replaces the model client inside AGUIDojoApi. The rest of the stack — the AG-UI request
// serialization, the SSE response, AGUIChatClient in DojoClient, and Components.AI — is the
// real one, which is what these tests exist to cover.
//
// Between checkpoints the client waits on a test-controlled gate, so a test can assert the
// partially streamed UI before letting the response finish.
internal sealed class RecordedChatClient : IChatClient
{
    private readonly RecordedScript _script;
    private readonly TestLockProvider _locks;

    public RecordedChatClient(RecordedScript script, TestLockProvider locks)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(locks);
        _script = script;
        _locks = locks;
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var messageList = messages.ToList();
        var lastUserMessage =
            messageList.LastOrDefault(message => message.Role == ChatRole.User)?.Text ?? "";
        var call = _script.GetCall(lastUserMessage, messageList.Count);
        AssertRequest(call, messageList, options);
        var messageId = Guid.NewGuid().ToString("N");

        for (var frameIndex = 0; frameIndex < call.Frames.Count; frameIndex++)
        {
            var frame = call.Frames[frameIndex];
            if (frame.FunctionCall is not null)
            {
                yield return new ChatResponseUpdate
                {
                    Role = ChatRole.Assistant,
                    MessageId = messageId,
                    Contents =
                    [
                        new FunctionCallContent(
                            frame.FunctionCall.CallId,
                            frame.FunctionCall.Name,
                            frame.FunctionCall.Arguments)
                    ],
                    FinishReason = ChatFinishReason.ToolCalls,
                };
            }

            foreach (var chunk in frame.Chunks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new ChatResponseUpdate
                {
                    Role = ChatRole.Assistant,
                    MessageId = messageId,
                    Contents = [new TextContent(chunk)],
                };
            }

            if (frameIndex < call.Frames.Count - 1)
            {
                await _locks.WaitOn(GetLockKey(lastUserMessage, frame.Name)).WaitAsync(cancellationToken);
            }
        }
    }

    internal static string GetLockKey(string lastUserMessage, string frameName)
        => $"replay:{lastUserMessage}:{frameName}";

    private static void AssertRequest(
        RecordedCall call,
        IReadOnlyList<ChatMessage> messages,
        ChatOptions? options)
    {
        if (call.ToolNames is not null)
        {
            var actualToolNames = options?.Tools?
                .OfType<AIFunctionDeclaration>()
                .Select(tool => tool.Name)
                .ToList() ?? [];
            if (!actualToolNames.SequenceEqual(call.ToolNames))
            {
                throw new InvalidOperationException(
                    $"Expected tools [{string.Join(", ", call.ToolNames)}], " +
                    $"received [{string.Join(", ", actualToolNames)}].");
            }
        }

        if (call.ToolResultCallIds is not null)
        {
            var actualCallIds = messages
                .Where(message => message.Role == ChatRole.Tool)
                .SelectMany(message => message.Contents)
                .OfType<FunctionResultContent>()
                .Select(result => result.CallId)
                .ToList();
            if (!actualCallIds.SequenceEqual(call.ToolResultCallIds))
            {
                throw new InvalidOperationException(
                    $"Expected tool results [{string.Join(", ", call.ToolResultCallIds)}], " +
                    $"received [{string.Join(", ", actualCallIds)}].");
            }
        }
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => GetStreamingResponseAsync(messages, options, cancellationToken)
            .ToChatResponseAsync(cancellationToken);

    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceType == typeof(IChatClient) ? this : null;

    public void Dispose()
    {
    }
}
