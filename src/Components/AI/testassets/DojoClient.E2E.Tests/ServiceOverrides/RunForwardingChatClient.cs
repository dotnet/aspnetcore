// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json;
using AGUI.Abstractions;
using DojoAgent;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.AI;

namespace DojoClient.E2E.Tests.ServiceOverrides;

internal sealed class RunForwardingChatClient(
    IChatClient innerClient,
    NavigationManager navigation,
    DojoBackendKind backend) : DelegatingChatClient(innerClient)
{
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var query = QueryHelpers.ParseQuery(new Uri(navigation.Uri).Query);
        if (!query.TryGetValue(DojoRunStore.RunKey, out var runId) ||
            !Guid.TryParseExact(runId, "N", out _))
        {
            throw new InvalidOperationException("Navigate with DojoTestSession.GetScenarioUrl to select a test session.");
        }

        var requestOptions = options?.Clone() ?? new ChatOptions();
        if (backend == DojoBackendKind.Direct)
        {
            requestOptions.AdditionalProperties = options?.AdditionalProperties is { } properties
                ? new(properties)
                : [];
            requestOptions.AdditionalProperties[DojoRunStore.RunKey] = runId.ToString();
        }
        else
        {
            // AGUIChatClient pins its generated thread ID in the supplied options. Preserve
            // that shared metadata when wrapping the request factory.
            if (options is not null)
            {
                options.AdditionalProperties ??= [];
                requestOptions.AdditionalProperties = options.AdditionalProperties;
            }

            var createInput = options?.RawRepresentationFactory;
            requestOptions.RawRepresentationFactory = client =>
            {
                var input = createInput?.Invoke(client) switch
                {
                    null => new RunAgentInput(),
                    RunAgentInput provided => provided,
                    _ => throw new InvalidOperationException("Expected an AG-UI request representation."),
                };
                var forwarded = input.ForwardedProperties.ValueKind switch
                {
                    JsonValueKind.Undefined or JsonValueKind.Null => new Dictionary<string, JsonElement>(),
                    JsonValueKind.Object => input.ForwardedProperties.EnumerateObject()
                        .ToDictionary(property => property.Name, property => property.Value),
                    _ => throw new InvalidOperationException("Expected object-valued AG-UI forwarded properties."),
                };
                forwarded[DojoRunStore.RunKey] = JsonSerializer.SerializeToElement(runId.ToString());
                input.ForwardedProperties = JsonSerializer.SerializeToElement(forwarded);

                return input;
            };
        }

        await foreach (var update in base.GetStreamingResponseAsync(
            messages, requestOptions, cancellationToken).ConfigureAwait(false))
        {
            yield return update;
        }
    }

    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => GetStreamingResponseAsync(messages, options, cancellationToken)
            .ToChatResponseAsync(cancellationToken);
}
