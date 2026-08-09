// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace AIApp.E2E.Tests.ServiceOverrides;

internal sealed class CapturingChatClient : IChatClient
{
    private readonly IChatClient _inner;
    private readonly List<CapturedChatCall> _calls = [];

    public CapturingChatClient(IChatClient inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    public IReadOnlyList<CapturedChatCall> Calls => _calls;

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var capturedMessages = Clone(messages.ToList());
        var capturedUpdates = new List<ChatResponseUpdate>();

        await foreach (var update in _inner.GetStreamingResponseAsync(
            capturedMessages, options, cancellationToken).ConfigureAwait(false))
        {
            capturedUpdates.Add(Clone(update));
            yield return update;
        }

        _calls.Add(new CapturedChatCall(capturedMessages, options, capturedUpdates));
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => GetStreamingResponseAsync(messages, options, cancellationToken)
            .ToChatResponseAsync(cancellationToken);

    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceType == typeof(IChatClient) ? this : _inner.GetService(serviceType, serviceKey);

    public void Dispose() => _inner.Dispose();

    public void SaveRecording(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var recordings = _calls.Select(call => new
        {
            call.Messages,
            call.Updates,
        });
        var json = JsonSerializer.Serialize(recordings, ReplayCheckpointScript.SerializerOptions);
        File.WriteAllText(path, json);
    }

    private static T Clone<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, ReplayCheckpointScript.SerializerOptions);
        return JsonSerializer.Deserialize<T>(json, ReplayCheckpointScript.SerializerOptions)
            ?? throw new InvalidOperationException($"Could not decode a captured {typeof(T).Name}.");
    }
}

internal sealed record CapturedChatCall(
    IReadOnlyList<ChatMessage> Messages,
    ChatOptions? Options,
    IReadOnlyList<ChatResponseUpdate> Updates);
