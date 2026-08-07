// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;

internal static class RecordingLoader
{
    internal static List<List<ChatResponseUpdate>> Load(string recordingFileName)
    {
        var basePath = Path.Combine(AppContext.BaseDirectory, "Baselines", recordingFileName);
        if (!File.Exists(basePath))
        {
            throw new FileNotFoundException(
                $"Recording baseline not found: {basePath}. " +
                "Ensure the file is copied to output (CopyToOutputDirectory = PreserveNewest).");
        }

        var json = File.ReadAllText(basePath);
        var turns = JsonSerializer.Deserialize<List<List<ChatResponseUpdate>>>(
            json, AIJsonUtilities.DefaultOptions);

        return turns ?? throw new InvalidOperationException(
            $"Recording file deserialized to null: {recordingFileName}");
    }

    internal static DelegatingStreamingChatClient CreateReplayClient(string recordingFileName)
    {
        var turns = Load(recordingFileName);
        var turnIndex = 0;

        var client = new DelegatingStreamingChatClient();
        client.SetHandler((messages, options, ct) =>
        {
            if (turnIndex >= turns.Count)
            {
                throw new InvalidOperationException(
                    $"Recording has {turns.Count} turns but GetStreamingResponseAsync " +
                    $"was called {turnIndex + 1} times.");
            }

            var currentTurn = turns[turnIndex];
            turnIndex++;
            return YieldUpdates(currentTurn, ct);
        });

        return client;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> YieldUpdates(
        List<ChatResponseUpdate> updates,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var update in updates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
        }

        await Task.CompletedTask;
    }
}
