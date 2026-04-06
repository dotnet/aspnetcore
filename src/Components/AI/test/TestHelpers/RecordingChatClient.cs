// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;

internal sealed class RecordingChatClient : IChatClient
{
    private readonly IChatClient _inner;
    private readonly List<List<ChatResponseUpdate>> _recordedTurns = new();

    internal RecordingChatClient(IChatClient inner)
    {
        _inner = inner;
    }

    internal IReadOnlyList<List<ChatResponseUpdate>> RecordedTurns => _recordedTurns;

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var turnUpdates = new List<ChatResponseUpdate>();

        await foreach (var update in _inner.GetStreamingResponseAsync(
            messages, options, cancellationToken).ConfigureAwait(false))
        {
            // Snapshot via round-trip serialization so downstream mutations
            // (e.g. FunctionInvokingChatClient setting InformationalOnly = true)
            // don't pollute the recording.
            var json = JsonSerializer.Serialize(update, AIJsonUtilities.DefaultOptions);
            var snapshot = JsonSerializer.Deserialize<ChatResponseUpdate>(json, AIJsonUtilities.DefaultOptions)!;
            turnUpdates.Add(snapshot);
            yield return update;
        }

        _recordedTurns.Add(turnUpdates);
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Only streaming is supported for recording.");

    public object? GetService(Type serviceType, object? serviceKey = null)
        => _inner.GetService(serviceType, serviceKey);

    public void Dispose() => _inner.Dispose();

    internal void SaveRecording(string path)
    {
        var json = JsonSerializer.Serialize(_recordedTurns, AIJsonUtilities.DefaultOptions);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json);
    }
}
