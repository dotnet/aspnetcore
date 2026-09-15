// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json;
using AGUI.Abstractions;
using Microsoft.Extensions.AI;

namespace ComponentsAIClaimApp.Data;

internal sealed class ClaimAgentChatClient : IChatClient
{
    private readonly ClaimAgentTransport _transport;

    public ClaimAgentChatClient(
        IClaimAssistantBackend backend,
        ILogger<ClaimAgentTransport> logger)
    {
        _transport = new ClaimAgentTransport(backend, logger);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var input = options?.RawRepresentationFactory?.Invoke(this) as RunAgentInput;
        await foreach (var evt in _transport.SendAsync(
            messages.ToList(),
            input?.State,
            cancellationToken))
        {
            switch (evt)
            {
                case ClaimAgentErrorEvent error:
                    yield return new ChatResponseUpdate
                    {
                        RawRepresentation = new RunErrorEvent
                        {
                            Code = error.Code,
                            Message = error.Message,
                        },
                    };
                    break;

                case ClaimAgentTextEvent text:
                    yield return new ChatResponseUpdate
                    {
                        Role = ChatRole.Assistant,
                        MessageId = text.MessageId,
                        Contents = [new TextContent(text.Delta)],
                    };
                    break;

                case ClaimAgentToolCallEvent toolCall:
                    yield return new ChatResponseUpdate
                    {
                        Role = ChatRole.Assistant,
                        MessageId = toolCall.MessageId,
                        Contents =
                        [
                            new FunctionCallContent(
                                toolCall.ToolCallId,
                                toolCall.ToolName,
                                DeserializeArguments(toolCall.Arguments)),
                        ],
                        FinishReason = ChatFinishReason.ToolCalls,
                    };
                    break;

                case ClaimAgentToolResultEvent toolResult:
                    yield return new ChatResponseUpdate
                    {
                        Contents =
                        [
                            new FunctionResultContent(
                                toolResult.ToolCallId,
                                toolResult.Result),
                        ],
                    };
                    break;

                case ClaimAgentApprovalRequestEvent approval:
                    var approvalCall = new FunctionCallContent(
                        approval.ToolCallId,
                        approval.ToolName,
                        DeserializeArguments(approval.Arguments));
                    yield return new ChatResponseUpdate
                    {
                        Role = ChatRole.Assistant,
                        MessageId = approval.MessageId,
                        Contents =
                        [
                            new ToolApprovalRequestContent(
                                approval.RequestId,
                                approvalCall),
                        ],
                    };
                    break;

                case ClaimAgentStateSnapshotEvent snapshot:
                    yield return new ChatResponseUpdate
                    {
                        RawRepresentation = new StateSnapshotEvent
                        {
                            Snapshot = snapshot.Snapshot,
                        },
                    };
                    break;

                case ClaimAgentStateDeltaEvent delta:
                    yield return new ChatResponseUpdate
                    {
                        RawRepresentation = new StateDeltaEvent
                        {
                            Delta = delta.Delta,
                        },
                    };
                    break;
            }
        }
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("This sample client uses streaming responses.");

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }

    private static IDictionary<string, object?>? DeserializeArguments(JsonElement arguments)
        => arguments.Deserialize<Dictionary<string, object?>>(ClaimStateJson.Options);
}
