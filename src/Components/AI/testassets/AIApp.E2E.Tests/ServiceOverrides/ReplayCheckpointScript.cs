// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.AI;

namespace AIApp.E2E.Tests.ServiceOverrides;

internal sealed class ReplayCheckpointScript
{
    internal static JsonSerializerOptions SerializerOptions { get; } = CreateSerializerOptions();

    public required List<ReplayCall> Calls { get; init; }

    public static ReplayCheckpointScript Load(string recordingFileName)
    {
        ArgumentException.ThrowIfNullOrEmpty(recordingFileName);
        var assemblyDirectory = Path.GetDirectoryName(typeof(ReplayCheckpointScript).Assembly.Location)
            ?? throw new InvalidOperationException("Could not locate the E2E test assembly.");
        var path = Path.Combine(assemblyDirectory, "Baselines", recordingFileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Replay checkpoint script not found: {path}", path);
        }

        var script = JsonSerializer.Deserialize<ReplayCheckpointScript>(
            File.ReadAllText(path),
            SerializerOptions);

        return script ?? throw new InvalidOperationException(
            $"Replay checkpoint script decoded to null: {recordingFileName}");
    }

    public string GetLockName(int callIndex, int checkpointIndex)
    {
        var frame = Calls[callIndex].Frames[checkpointIndex];
        return $"replay:call-{callIndex + 1}:checkpoint-{checkpointIndex + 1}:{frame.Name}";
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions(AIJsonUtilities.DefaultOptions)
        {
            WriteIndented = true,
        };
        return options;
    }
}

internal sealed class ReplayCall
{
    public ReplayRequestExpectation? Request { get; init; }

    public required List<ReplayFrame> Frames { get; init; }
}

internal sealed class ReplayRequestExpectation
{
    public string? LastUserMessage { get; init; }

    public int? MessageCount { get; init; }

    public List<string>? ToolNames { get; init; }

    public ReplayFunctionResultExpectation? FunctionResult { get; init; }
}

internal sealed class ReplayFunctionResultExpectation
{
    public required string CallId { get; init; }

    public required string Result { get; init; }
}

internal sealed class ReplayFrame
{
    public required string Name { get; init; }

    public required List<ChatResponseUpdate> Updates { get; init; }
}
