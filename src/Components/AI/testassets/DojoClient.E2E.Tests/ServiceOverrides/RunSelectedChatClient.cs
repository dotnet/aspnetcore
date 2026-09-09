// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json;
using AGUI.Server;
using Microsoft.Extensions.AI;

namespace DojoClient.E2E.Tests.ServiceOverrides;

internal sealed class RunSelectedChatClient(DojoRunStore runs, bool predictive = false) : IChatClient
{
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? runId = null;
        if (options?.AdditionalProperties?.TryGetValue(DojoRunStore.RunKey, out var value) == true)
        {
            runId = value as string;
        }
        else if (options is not null && options.TryGetRunAgentInput(out var input) &&
            input.ForwardedProperties.ValueKind == JsonValueKind.Object &&
            input.ForwardedProperties.TryGetProperty(DojoRunStore.RunKey, out var id))
        {
            runId = id.GetString();
        }

        if (string.IsNullOrEmpty(runId))
        {
            throw new InvalidOperationException("The model request is missing its dojo test-session ID.");
        }

        await foreach (var update in runs.Get(runId).GetUpdatesAsync(
            messages, options, predictive, cancellationToken).ConfigureAwait(false))
        {
            yield return update;
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
