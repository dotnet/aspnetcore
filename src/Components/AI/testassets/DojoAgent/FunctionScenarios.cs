// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using AGUI.Server;
using Microsoft.Extensions.AI;

namespace DojoAgent;

/// <summary>
/// Creates deterministic server-tool scenarios for both dojo backends.
/// </summary>
public static class FunctionScenarios
{
    /// <summary>The approval scenario's service key and endpoint.</summary>
    public const string Approval = "/function-approval";

    /// <summary>The informational invocation scenario's service key and endpoint.</summary>
    public const string Invocation = "/function-invocation";

    /// <summary>Creates a scenario using the real function-invocation pipeline.</summary>
    /// <param name="state">The host-owned invocation counters and result gates.</param>
    /// <param name="requiresApproval">Whether the server tool requires user approval.</param>
    /// <returns>The scenario's chat client.</returns>
    public static IChatClient Create(FunctionScenarioState state, bool requiresApproval)
        => new FunctionScenarioChatClient(state, requiresApproval);

    private sealed class FunctionScenarioChatClient(
        FunctionScenarioState state,
        bool requiresApproval)
        : DelegatingChatClient(new FunctionInvokingChatClient(new WeatherModelChatClient(requiresApproval)))
    {
        public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var threadId = GetThreadId(options);
            var requestOptions = options?.Clone() ?? new ChatOptions();
            AIFunction tool = requiresApproval
                ? AIFunctionFactory.Create(
                    (string location) => state.Invoke(threadId),
                    name: "get_weather",
                    description: "Get the weather for a given location.")
                : AIFunctionFactory.Create(
                    (string location, CancellationToken token) => state.InvokeAsync(threadId, token),
                    name: "get_weather",
                    description: "Get the weather for a given location.");
            requestOptions.Tools = [requiresApproval ? new ApprovalRequiredAIFunction(tool) : tool];

            await foreach (var update in base.GetStreamingResponseAsync(
                messages, requestOptions, cancellationToken).ConfigureAwait(false))
            {
                var originalCalls = new List<(int Index, FunctionCallContent Call)>();
                if (!requiresApproval)
                {
                    for (var i = 0; i < update.Contents.Count; i++)
                    {
                        if (update.Contents[i] is FunctionCallContent call)
                        {
                            originalCalls.Add((i, call));
                            update.Contents[i] = new FunctionCallContent(call.CallId, call.Name, call.Arguments)
                            {
                                InformationalOnly = true,
                                AdditionalProperties = call.AdditionalProperties,
                                Annotations = call.Annotations,
                                Exception = call.Exception,
                                RawRepresentation = call.RawRepresentation,
                            };
                        }
                    }
                }

                try
                {
                    yield return update;
                }
                finally
                {
                    foreach (var (index, call) in originalCalls)
                    {
                        update.Contents[index] = call;
                    }
                }
            }
        }

        public override Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => GetStreamingResponseAsync(messages, options, cancellationToken)
                .ToChatResponseAsync(cancellationToken);

        private static string GetThreadId(ChatOptions? options)
        {
            if (options?.AdditionalProperties?.TryGetValue(DojoRequestContext.PropertyName, out var value) == true)
            {
                return value switch
                {
                    DojoRequestContext context => context.ThreadId,
                    Func<DojoRequestContext> getContext => getContext().ThreadId,
                    _ => throw new InvalidOperationException("Invalid dojo request context."),
                };
            }

            if (options is not null && options.TryGetRunAgentInput(out var input))
            {
                return input.ThreadId;
            }

            throw new InvalidOperationException("The function scenario requires a conversation thread.");
        }
    }

    private sealed class WeatherModelChatClient(bool requiresApproval) : IChatClient
    {
        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var messageList = messages.ToList();
            if (messageList.LastOrDefault()?.Role == ChatRole.Tool)
            {
                var result = messageList[^1].Contents.OfType<FunctionResultContent>().Single().Result?.ToString();
                yield return new ChatResponseUpdate
                {
                    Role = ChatRole.Assistant,
                    MessageId = "dojo-weather-response",
                    Contents =
                    [
                        new TextContent(requiresApproval
                            ? result == "Tool call invocation rejected."
                                ? "The weather lookup was rejected and was not executed."
                                : "The approved weather lookup returned sunny conditions."
                            : "The weather in Seattle is sunny.")
                    ],
                    FinishReason = ChatFinishReason.Stop,
                };
                yield break;
            }

            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                MessageId = "dojo-weather-call",
                Contents =
                [
                    new FunctionCallContent(
                        "dojo-weather-call-1", "get_weather",
                        new Dictionary<string, object?> { ["location"] = "Seattle" })
                ],
                FinishReason = ChatFinishReason.ToolCalls,
            };

            await Task.CompletedTask;
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
}
