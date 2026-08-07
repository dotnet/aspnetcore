// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace BlazorWebAppPerPage.Data;

internal sealed class ConfigurableChatClient : IChatClient
{
    private readonly ClaimDemoTransport _transport = new();

    public string ConnectionDescription => "Local AG-UI demo transport";

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var input = options?.RawRepresentationFactory?.Invoke(this) as ClaimAgUiRunInput
            ?? new ClaimAgUiRunInput();
        input.Messages = messages.ToList();

        await foreach (var evt in _transport.SendAsync(input, cancellationToken))
        {
            switch (evt)
            {
                case ClaimAgUiRunStartedEvent:
                case ClaimAgUiRunFinishedEvent:
                    break;

                case ClaimAgUiRunErrorEvent error:
                    yield return new ChatResponseUpdate
                    {
                        Contents =
                        [
                            new ErrorContent(error.Message)
                            {
                                ErrorCode = error.Code,
                            },
                        ],
                        RawRepresentation = error,
                    };
                    break;

                case ClaimAgUiTextMessageContentEvent text:
                    yield return new ChatResponseUpdate
                    {
                        Role = ChatRole.Assistant,
                        MessageId = text.MessageId,
                        Contents = [new TextContent(text.Delta)],
                        RawRepresentation = text,
                    };
                    break;

                case ClaimAgUiToolCallEvent toolCall:
                    yield return new ChatResponseUpdate
                    {
                        Role = ChatRole.Assistant,
                        Contents =
                        [
                            new FunctionCallContent(
                                toolCall.ToolCallId,
                                toolCall.ToolName,
                                DeserializeArguments(toolCall.Arguments)),
                        ],
                        FinishReason = ChatFinishReason.ToolCalls,
                        RawRepresentation = toolCall,
                    };
                    break;

                case ClaimAgUiToolResultEvent toolResult:
                    yield return new ChatResponseUpdate
                    {
                        Contents =
                        [
                            new FunctionResultContent(
                                toolResult.ToolCallId,
                                toolResult.Result),
                        ],
                        RawRepresentation = toolResult,
                    };
                    break;

                case ClaimAgUiApprovalRequestEvent approval:
                    var approvalCall = new FunctionCallContent(
                        approval.ToolCallId,
                        approval.ToolName,
                        DeserializeArguments(approval.Arguments));
                    yield return new ChatResponseUpdate
                    {
                        Role = ChatRole.Assistant,
                        Contents =
                        [
                            new ToolApprovalRequestContent(
                                approval.RequestId,
                                approvalCall),
                        ],
                        RawRepresentation = approval,
                    };
                    break;

                case ClaimAgUiStateSnapshotEvent:
                case ClaimAgUiStateDeltaEvent:
                    yield return new ChatResponseUpdate
                    {
                        RawRepresentation = evt,
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
