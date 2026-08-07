// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace AIApp.E2E.Tests.ServiceOverrides;

// IChatClient that replays pre-recorded turns from a baseline JSON file.
// Each call to GetStreamingResponseAsync yields the next recorded turn.
internal sealed class BaselineReplayClient : IChatClient
{
    private readonly List<List<ChatResponseUpdate>> _turns;
    private int _turnIndex;

    private BaselineReplayClient(List<List<ChatResponseUpdate>> turns)
    {
        _turns = turns;
    }

    internal static BaselineReplayClient FromBaseline(string recordingFileName)
    {
        var testAssemblyDir = Path.GetDirectoryName(typeof(BaselineReplayClient).Assembly.Location)!;
        var baselinePath = Path.Combine(testAssemblyDir, "Baselines", recordingFileName);

        if (!File.Exists(baselinePath))
        {
            throw new FileNotFoundException(
                $"E2E baseline not found: {baselinePath}. " +
                "Ensure the file is copied to output (CopyToOutputDirectory = PreserveNewest).");
        }

        var json = File.ReadAllText(baselinePath);
        var turns = JsonSerializer.Deserialize<List<List<ChatResponseUpdate>>>(
            json, AIJsonUtilities.DefaultOptions);

        return new BaselineReplayClient(
            turns ?? throw new InvalidOperationException(
                $"Baseline file deserialized to null: {recordingFileName}"));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_turnIndex >= _turns.Count)
        {
            throw new InvalidOperationException(
                $"Baseline has {_turns.Count} turns but GetStreamingResponseAsync " +
                $"was called {_turnIndex + 1} times.");
        }

        var currentTurn = _turns[_turnIndex];
        _turnIndex++;

        foreach (var update in currentTurn)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
        }

        await Task.CompletedTask;
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceType == typeof(IChatClient) ? this : null;

    public void Dispose() { }
}
