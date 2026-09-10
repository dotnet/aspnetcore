// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json;
using DojoAgent.PredictiveStateUpdates;
using Microsoft.Extensions.AI;

namespace DojoAgent;

internal sealed class DirectDojoChatClient : DelegatingChatClient
{
    private readonly IList<AITool> _serverTools;
    private readonly string? _systemPrompt;
    private readonly bool _predictive;

    internal DirectDojoChatClient(IChatClient model, string endpoint)
        : base(model)
    {
        var jsonOptions = AIJsonUtilities.DefaultOptions;
        (string? SystemPrompt, IList<AITool> Tools) scenario = endpoint switch
        {
            DojoScenarioEndpoints.AgenticChatEndpoint => (null, []),
            DojoScenarioEndpoints.BackendToolRenderingEndpoint => (null,
                ChatClientAgentFactory.CreateBackendToolRenderingTools(jsonOptions)),
            DojoScenarioEndpoints.HumanInTheLoopEndpoint => (ChatClientAgentFactory.HumanInTheLoopSystemPrompt, []),
            DojoScenarioEndpoints.ToolBasedGenerativeUIEndpoint => (ChatClientAgentFactory.ToolBasedGenerativeUISystemPrompt, []),
            DojoScenarioEndpoints.AgenticGenerativeUIEndpoint => (ChatClientAgentFactory.AgenticGenerativeUISystemPrompt,
                ChatClientAgentFactory.CreateAgenticGenerativeUITools(jsonOptions)),
            DojoScenarioEndpoints.SharedStateEndpoint => (ChatClientAgentFactory.SharedStateSystemPrompt,
                ChatClientAgentFactory.CreateSharedStateTools(jsonOptions)),
            DojoScenarioEndpoints.PredictiveStateUpdatesEndpoint => (ChatClientAgentFactory.PredictiveStateUpdatesSystemPrompt,
                ChatClientAgentFactory.CreatePredictiveStateUpdatesTools(jsonOptions)),
            _ => throw new ArgumentException($"Unknown dojo scenario '{endpoint}'.", nameof(endpoint)),
        };
        _systemPrompt = scenario.SystemPrompt;
        _serverTools = scenario.Tools;
        _predictive = endpoint == DojoScenarioEndpoints.PredictiveStateUpdatesEndpoint;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestMessages = messages.ToList();
        if (_systemPrompt is not null)
        {
            requestMessages.Insert(0, new ChatMessage(ChatRole.System, _systemPrompt));
        }

        var requestOptions = options?.Clone() ?? new ChatOptions();
        requestOptions.RawRepresentationFactory = null;
        requestOptions.AdditionalProperties = options?.AdditionalProperties is { } properties
            ? new AdditionalPropertiesDictionary(properties)
            : [];
        if (requestOptions.AdditionalProperties.TryGetValue(DojoRequestContext.PropertyName, out var metadata) &&
            metadata is Func<DojoRequestContext> getContext)
        {
            requestOptions.AdditionalProperties[DojoRequestContext.PropertyName] = getContext();
        }

        var serverToolNames = _serverTools.Select(tool => tool.Name).ToHashSet(StringComparer.Ordinal);
        requestOptions.Tools =
        [
            .. options?.Tools?.Where(tool => !serverToolNames.Contains(tool.Name)) ?? [],
            .. _serverTools,
        ];

        var documentUpdates = new PredictiveDocumentUpdates();
        await foreach (var update in base.GetStreamingResponseAsync(
            requestMessages, requestOptions, cancellationToken).ConfigureAwait(false))
        {
            yield return update;

            if (!_predictive)
            {
                continue;
            }

            foreach (var call in update.Contents.OfType<FunctionCallContent>())
            {
                if (documentUpdates.Create(call) is not { } change)
                {
                    continue;
                }

                foreach (var state in change.Snapshots)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return new ChatResponseUpdate
                    {
                        Role = ChatRole.Assistant,
                        MessageId = update.MessageId,
                        Contents =
                        [
                            new DataContent(
                                JsonSerializer.SerializeToUtf8Bytes(state, AIJsonUtilities.DefaultOptions),
                                ChatClientAgentFactory.PredictiveStateMediaType),
                        ],
                    };
                }

                var writeDocument = (AIFunction)_serverTools.Single(tool => tool.Name == call.Name);
                var result = await writeDocument.InvokeAsync(
                    new AIFunctionArguments(call.Arguments), cancellationToken).ConfigureAwait(false);
                yield return new ChatResponseUpdate
                {
                    Role = ChatRole.Tool,
                    MessageId = Guid.NewGuid().ToString("N"),
                    Contents = [new FunctionResultContent(call.CallId, result)],
                };

                if (change.Confirmation is { } confirmation)
                {
                    yield return new ChatResponseUpdate
                    {
                        Role = ChatRole.Assistant,
                        MessageId = Guid.NewGuid().ToString("N"),
                        Contents = [confirmation],
                        FinishReason = ChatFinishReason.ToolCalls,
                    };
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

    protected override void Dispose(bool disposing)
    {
    }
}
